using FaJuan.Api.Application.Common;
using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Application.UserCoupons;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/user-coupons")]
public class UserCouponsController(AppDbContext dbContext, UserCouponGrantService grantService) : ApiControllerBase
{
    [AdminPermissionAuthorize("user-coupon.grant")]
    [HttpPost("import-grant")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ImportGrantUserCouponsResultDto>>> ImportGrant([FromForm] ImportGrantUserCouponsRequest request)
    {
        var file = request.File;
        var couponTemplateId = request.CouponTemplateId;
        var quantityPerUser = request.QuantityPerUser;
        var csvContent = request.CsvContent;

        if (couponTemplateId <= 0)
        {
            return BadRequest(Failure<ImportGrantUserCouponsResultDto>("券模板不能为空"));
        }

        if ((file is null || file.Length <= 0) && string.IsNullOrWhiteSpace(csvContent))
        {
            return BadRequest(Failure<ImportGrantUserCouponsResultDto>("请上传 CSV 文件或提供 CSV 内容"));
        }

        if (file is not null && file.Length > 0 && !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(Failure<ImportGrantUserCouponsResultDto>("目前仅支持 CSV 导入"));
        }

        var defaultQuantityPerUser = quantityPerUser.GetValueOrDefault(1);
        if (defaultQuantityPerUser <= 0)
        {
            return BadRequest(Failure<ImportGrantUserCouponsResultDto>("默认每人发放张数必须大于 0"));
        }

        string content;
        if (!string.IsNullOrWhiteSpace(csvContent))
        {
            content = csvContent;
        }
        else
        {
            await using var stream = file!.OpenReadStream();
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            content = await reader.ReadToEndAsync();
        }

        var lines = content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (lines.Count == 0)
        {
            return BadRequest(Failure<ImportGrantUserCouponsResultDto>("CSV 文件内容为空"));
        }

        var invalidRows = new List<string>();
        var header = DetectImportHeader(lines[0]);
        var dataLines = header is null ? lines : lines.Skip(1).ToList();
        if (dataLines.Count == 0)
        {
            return BadRequest(Failure<ImportGrantUserCouponsResultDto>("CSV 未解析到有效数据"));
        }

        var rows = new List<ParsedImportGrantRow>();
        foreach (var rawLine in dataLines)
        {
            var cells = rawLine.Split(',', StringSplitOptions.TrimEntries);
            if (cells.Length == 0)
            {
                continue;
            }

            if (header is null)
            {
                if (cells.Length < 3)
                {
                    invalidRows.Add($"列数不足：{rawLine}");
                    continue;
                }

                if (!long.TryParse(GetCsvCell(cells, 0), out var appUserId) || appUserId <= 0)
                {
                    invalidRows.Add($"用户ID无效：{rawLine}");
                    continue;
                }

                if (!long.TryParse(GetCsvCell(cells, 1), out var legacyCouponTemplateId) || legacyCouponTemplateId != couponTemplateId)
                {
                    invalidRows.Add($"券模板ID无效或与表单不一致：{rawLine}");
                    continue;
                }

                if (!int.TryParse(GetCsvCell(cells, 2), out var legacyQuantityPerUser) || legacyQuantityPerUser <= 0)
                {
                    invalidRows.Add($"发放数量无效：{rawLine}");
                    continue;
                }

                rows.Add(new ParsedImportGrantRow
                {
                    RawLine = rawLine,
                    AppUserId = appUserId,
                    CouponTemplateId = legacyCouponTemplateId,
                    QuantityPerUser = legacyQuantityPerUser,
                });

                continue;
            }

            var identifierValue = GetCsvCell(cells, header.IdentifierIndex);
            if (string.IsNullOrWhiteSpace(identifierValue))
            {
                invalidRows.Add($"用户标识不能为空：{rawLine}");
                continue;
            }

            var rowCouponTemplateId = couponTemplateId;
            if (header.TemplateIndex.HasValue)
            {
                if (!long.TryParse(GetCsvCell(cells, header.TemplateIndex.Value), out rowCouponTemplateId) || rowCouponTemplateId != couponTemplateId)
                {
                    invalidRows.Add($"券模板ID无效或与表单不一致：{rawLine}");
                    continue;
                }
            }

            var rowQuantityPerUser = defaultQuantityPerUser;
            if (header.QuantityIndex.HasValue)
            {
                if (!int.TryParse(GetCsvCell(cells, header.QuantityIndex.Value), out rowQuantityPerUser) || rowQuantityPerUser <= 0)
                {
                    invalidRows.Add($"发放数量无效：{rawLine}");
                    continue;
                }
            }

            rows.Add(new ParsedImportGrantRow
            {
                RawLine = rawLine,
                IdentifierType = header.IdentifierType,
                IdentifierValue = identifierValue,
                CouponTemplateId = rowCouponTemplateId,
                QuantityPerUser = rowQuantityPerUser,
            });
        }

        var parsedRows = await ResolveImportRowsAsync(rows, invalidRows);
        if (parsedRows.Length == 0)
        {
            return BadRequest(Failure<ImportGrantUserCouponsResultDto>("CSV 未解析到有效数据，请使用 appUserId/mobile/miniOpenId/officialOpenId 等标识列"));
        }

        var template = await dbContext.CouponTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == couponTemplateId);
        if (template is null)
        {
            return NotFound(Failure<ImportGrantUserCouponsResultDto>("券模板不存在", 404));
        }

        if (!template.IsEnabled)
        {
            return BadRequest(Failure<ImportGrantUserCouponsResultDto>("券模板已停用，不能发券"));
        }

        var inputByUser = parsedRows
            .GroupBy(x => x.AppUserId)
            .Select(group => new ManualGrantUserCouponInput
            {
                AppUserId = group.Key,
                QuantityPerUser = group.Sum(x => x.QuantityPerUser),
            })
            .ToArray();

        var parsedUserCount = inputByUser.Length;
        var grantResult = await grantService.GrantAsync(couponTemplateId, inputByUser);

        return Ok(Success(new ImportGrantUserCouponsResultDto
        {
            CouponTemplateId = couponTemplateId,
            QuantityPerUser = defaultQuantityPerUser,
            TotalRows = lines.Count,
            ParsedUserCount = parsedUserCount,
            SuccessCount = grantResult.SuccessCount,
            FailureCount = grantResult.FailureCount,
            InvalidRows = invalidRows,
            Items = grantResult.Items,
        }, $"导入完成，解析到 {parsedUserCount} 个用户"));
    }

    [AdminPermissionAuthorize("user-coupon.grant")]
    [HttpPost("manual-grant")]
    public async Task<ActionResult<ApiResponse<ManualGrantUserCouponsResultDto>>> ManualGrant([FromBody] ManualGrantUserCouponsRequest request)
    {
        if (request.CouponTemplateId <= 0)
        {
            return BadRequest(Failure<ManualGrantUserCouponsResultDto>("券模板不能为空"));
        }

        var appUserIds = request.AppUserIds.Where(x => x > 0).Distinct().ToArray();
        if (appUserIds.Length == 0)
        {
            return BadRequest(Failure<ManualGrantUserCouponsResultDto>("至少选择一个用户"));
        }

        if (request.QuantityPerUser <= 0)
        {
            return BadRequest(Failure<ManualGrantUserCouponsResultDto>("每人发放数量必须大于 0"));
        }

        var template = await dbContext.CouponTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CouponTemplateId);
        if (template is null)
        {
            return NotFound(Failure<ManualGrantUserCouponsResultDto>("券模板不存在", 404));
        }

        if (!template.IsEnabled)
        {
            return BadRequest(Failure<ManualGrantUserCouponsResultDto>("券模板已停用，不能发券"));
        }

        var result = await grantService.GrantAsync(request.CouponTemplateId, appUserIds.Select(x => new ManualGrantUserCouponInput
        {
            AppUserId = x,
            QuantityPerUser = request.QuantityPerUser,
        }).ToArray());

        return Ok(Success(result, $"已处理 {result.Items.Count} 个用户"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserCouponListItemDto>>>> GetList([FromQuery] long? userId, [FromQuery] string? couponCode, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.UserCoupons.AsNoTracking();

        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(x => x.AppUserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            query = query.Where(x => x.CouponCode.Contains(couponCode));
        }

        var totalCount = await query.CountAsync();
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new UserCouponListItemDto
            {
                Id = x.Id,
                AppUserId = x.AppUserId,
                CouponTemplateId = x.CouponTemplateId,
                CouponCode = x.CouponCode,
                Status = (int)x.Status,
                EffectiveAt = x.EffectiveAt,
                ExpireAt = x.ExpireAt,
                ReceivedAt = x.ReceivedAt,
            })
            .ToListAsync();

        return Ok(Success(new PagedResult<UserCouponListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<UserCouponDetailDto>>> GetDetail(long id)
    {
        var detail = await dbContext.UserCoupons.AsNoTracking()
            .Where(x => x.Id == id)
            .Join(
                dbContext.CouponTemplates.AsNoTracking(),
                userCoupon => userCoupon.CouponTemplateId,
                template => template.Id,
                (userCoupon, template) => new UserCouponDetailDto
                {
                    Id = userCoupon.Id,
                    AppUserId = userCoupon.AppUserId,
                    CouponTemplateId = userCoupon.CouponTemplateId,
                    CouponTemplateName = template.Name,
                    TemplateType = (int)template.TemplateType,
                    ValidPeriodType = (int)template.ValidPeriodType,
                    DiscountAmount = template.DiscountAmount,
                    ThresholdAmount = template.ThresholdAmount,
                    ValidDays = template.ValidDays,
                    ValidFrom = template.ValidFrom,
                    ValidTo = template.ValidTo,
                    IsNewUserOnly = template.IsNewUserOnly,
                    IsAllStores = template.IsAllStores,
                    PerUserLimit = template.PerUserLimit,
                    TemplateEnabled = template.IsEnabled,
                    TemplateRemark = template.Remark,
                    CouponCode = userCoupon.CouponCode,
                    Status = (int)userCoupon.Status,
                    EffectiveAt = userCoupon.EffectiveAt,
                    ExpireAt = userCoupon.ExpireAt,
                    ReceivedAt = userCoupon.ReceivedAt,
                })
            .FirstOrDefaultAsync();

        if (detail is null)
        {
            return NotFound(Failure<UserCouponDetailDto>("用户券不存在"));
        }

        return Ok(Success(detail));
    }

    [HttpGet("{id:long}/writeoff-records")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CouponWriteOffRecordDto>>>> GetWriteOffRecords(long id)
    {
        var exists = await dbContext.UserCoupons.AsNoTracking().AnyAsync(x => x.Id == id);
        if (!exists)
        {
            return NotFound(Failure<IReadOnlyCollection<CouponWriteOffRecordDto>>("用户券不存在"));
        }

        var records = await dbContext.Set<Domain.Entities.CouponWriteOffRecord>().AsNoTracking()
            .Where(x => x.UserCouponId == id)
            .GroupJoin(
                dbContext.Stores.AsNoTracking(),
                record => record.StoreId,
                store => store.Id,
                (record, stores) => new { record, stores })
            .SelectMany(
                x => x.stores.DefaultIfEmpty(),
                (x, store) => new CouponWriteOffRecordDto
                {
                    Id = x.record.Id,
                    UserCouponId = x.record.UserCouponId,
                    CouponCode = x.record.CouponCode,
                    StoreId = x.record.StoreId,
                    StoreName = store != null ? store.Name : string.Empty,
                    OperatorName = x.record.OperatorName,
                    DeviceCode = x.record.DeviceCode,
                    WriteOffAt = x.record.WriteOffAt,
                    CreatedAt = x.record.CreatedAt,
                })
            .OrderByDescending(x => x.WriteOffAt)
            .ToListAsync();

        return Ok(Success<IReadOnlyCollection<CouponWriteOffRecordDto>>(records));
    }

    private async Task<ParsedImportGrantRow[]> ResolveImportRowsAsync(List<ParsedImportGrantRow> rows, List<string> invalidRows)
    {
        var resolvedRows = rows.Where(x => x.AppUserId > 0).ToList();

        var groupedByType = rows
            .Where(x => x.AppUserId <= 0 && x.IdentifierType.HasValue && !string.IsNullOrWhiteSpace(x.IdentifierValue))
            .GroupBy(x => x.IdentifierType!.Value)
            .ToArray();

        foreach (var group in groupedByType)
        {
            var normalizedValues = group.Select(x => NormalizeIdentifier(group.Key, x.IdentifierValue!)).Distinct().ToArray();
            var resolvedMap = await ResolveIdentifierMapAsync(group.Key, normalizedValues);

            foreach (var row in group)
            {
                var normalizedValue = NormalizeIdentifier(group.Key, row.IdentifierValue!);
                if (!resolvedMap.TryGetValue(normalizedValue, out var appUserId) || appUserId <= 0)
                {
                    invalidRows.Add($"未匹配到用户：{row.RawLine}");
                    continue;
                }

                row.AppUserId = appUserId;
                resolvedRows.Add(row);
            }
        }

        return resolvedRows.ToArray();
    }

    private async Task<Dictionary<string, long>> ResolveIdentifierMapAsync(ImportIdentifierType identifierType, IReadOnlyCollection<string> normalizedValues)
    {
        if (normalizedValues.Count == 0)
        {
            return new Dictionary<string, long>();
        }

        var normalizedValueSet = normalizedValues.ToHashSet();

        return identifierType switch
        {
            ImportIdentifierType.Mobile => (await dbContext.AppUsers.AsNoTracking()
                .Where(x => x.Mobile != null)
                .GroupBy(x => x.Mobile!)
                .Where(x => x.Select(y => y.Id).Distinct().Count() == 1)
                .Select(x => new { Key = x.Key, AppUserId = x.Select(y => y.Id).First() })
                .ToListAsync())
                .Where(x => normalizedValueSet.Contains(x.Key))
                .ToDictionary(x => NormalizeIdentifier(identifierType, x.Key), x => x.AppUserId),
            ImportIdentifierType.MiniOpenId => (await dbContext.AppUsers.AsNoTracking()
                .GroupBy(x => x.MiniOpenId)
                .Where(x => x.Select(y => y.Id).Distinct().Count() == 1)
                .Select(x => new { Key = x.Key, AppUserId = x.Select(y => y.Id).First() })
                .ToListAsync())
                .Where(x => normalizedValueSet.Contains(x.Key))
                .ToDictionary(x => NormalizeIdentifier(identifierType, x.Key), x => x.AppUserId),
            ImportIdentifierType.OfficialOpenId => (await dbContext.AppUsers.AsNoTracking()
                .Where(x => x.OfficialOpenId != null)
                .GroupBy(x => x.OfficialOpenId!)
                .Where(x => x.Select(y => y.Id).Distinct().Count() == 1)
                .Select(x => new { Key = x.Key, AppUserId = x.Select(y => y.Id).First() })
                .ToListAsync())
                .Where(x => normalizedValueSet.Contains(x.Key))
                .ToDictionary(x => NormalizeIdentifier(identifierType, x.Key), x => x.AppUserId),
            _ => new Dictionary<string, long>(),
        };
    }

    private static ImportCsvHeader? DetectImportHeader(string firstLine)
    {
        var cells = firstLine.Split(',', StringSplitOptions.TrimEntries)
            .Select(x => x.Trim().Trim('"'))
            .ToArray();

        if (cells.Length == 0)
        {
            return null;
        }

        var identifierIndex = -1;
        ImportIdentifierType? identifierType = null;
        int? templateIndex = null;
        int? quantityIndex = null;

        for (var index = 0; index < cells.Length; index++)
        {
            var name = cells[index].Trim();
            if (TryMatchIdentifierColumn(name, out var matchedType))
            {
                identifierIndex = index;
                identifierType = matchedType;
                continue;
            }

            if (IsTemplateColumn(name))
            {
                templateIndex = index;
                continue;
            }

            if (IsQuantityColumn(name))
            {
                quantityIndex = index;
            }
        }

        if (identifierIndex < 0 || !identifierType.HasValue)
        {
            return null;
        }

        return new ImportCsvHeader(identifierIndex, identifierType.Value, templateIndex, quantityIndex);
    }

    private static bool TryMatchIdentifierColumn(string name, out ImportIdentifierType identifierType)
    {
        var normalized = name.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "appuserid":
            case "userid":
            case "用户id":
            case "用户编号":
                identifierType = ImportIdentifierType.AppUserId;
                return true;
            case "mobile":
            case "phone":
            case "手机号":
            case "手机号码":
                identifierType = ImportIdentifierType.Mobile;
                return true;
            case "miniopenid":
            case "小程序openid":
                identifierType = ImportIdentifierType.MiniOpenId;
                return true;
            case "officialopenid":
            case "公众号openid":
            case "openid":
                identifierType = ImportIdentifierType.OfficialOpenId;
                return true;
            default:
                identifierType = default;
                return false;
        }
    }

    private static bool IsTemplateColumn(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return normalized is "coupontemplateid" or "templateid" or "券模板id";
    }

    private static bool IsQuantityColumn(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return normalized is "quantityperuser" or "quantity" or "count" or "发放数量" or "每人发放张数";
    }

    private static string GetCsvCell(string[] cells, int index)
    {
        if (index < 0 || index >= cells.Length)
        {
            return string.Empty;
        }

        return cells[index].Trim().Trim('"');
    }

    private static string NormalizeIdentifier(ImportIdentifierType identifierType, string value)
    {
        var trimmed = value.Trim().Trim('"');
        return identifierType == ImportIdentifierType.Mobile
            ? trimmed.Replace(" ", string.Empty)
            : trimmed;
    }

    private sealed record ImportCsvHeader(int IdentifierIndex, ImportIdentifierType IdentifierType, int? TemplateIndex, int? QuantityIndex);

    private sealed class ParsedImportGrantRow
    {
        public string RawLine { get; init; } = string.Empty;
        public ImportIdentifierType? IdentifierType { get; init; }
        public string? IdentifierValue { get; init; }
        public long AppUserId { get; set; }
        public long CouponTemplateId { get; init; }
        public int QuantityPerUser { get; init; }
    }

    private enum ImportIdentifierType
    {
        AppUserId = 1,
        Mobile = 2,
        MiniOpenId = 3,
        OfficialOpenId = 4,
    }
}
