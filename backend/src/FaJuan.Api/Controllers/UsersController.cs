using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/users")]
public class UsersController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserListItemDto>>>> GetList([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.AppUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => (x.Mobile != null && x.Mobile.Contains(keyword)) || (x.Nickname != null && x.Nickname.Contains(keyword)) || x.MiniOpenId.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserListItemDto
            {
                Id = x.Id,
                MiniOpenId = x.MiniOpenId,
                Mobile = x.Mobile,
                Nickname = x.Nickname,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        return Ok(Success(new PagedResult<UserListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }
}
