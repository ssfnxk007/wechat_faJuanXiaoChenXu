using System.Net;
using System.Net.Mime;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using FaJuan.Api.Contracts;
using Microsoft.Extensions.Options;

namespace FaJuan.Api.Infrastructure.WeChat;

public class WeChatMiniProgramService(HttpClient httpClient, IOptions<WeChatMiniProgramOptions> options, ILogger<WeChatMiniProgramService> logger)
{
    private readonly WeChatMiniProgramOptions _options = options.Value;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;

    public async Task<Code2SessionResult> Code2SessionAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            return new Code2SessionResult
            {
                ErrorCode = -1,
                ErrorMessage = "微信小程序 AppId/AppSecret 未配置",
            };
        }

        var requestUrl = $"https://api.weixin.qq.com/sns/jscode2session?appid={Uri.EscapeDataString(_options.AppId)}&secret={Uri.EscapeDataString(_options.AppSecret)}&js_code={Uri.EscapeDataString(code)}&grant_type=authorization_code";
        using var response = await httpClient.GetAsync(requestUrl, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new Code2SessionResult
            {
                ErrorCode = (int)response.StatusCode,
                ErrorMessage = $"微信接口请求失败: {payload}",
            };
        }

        var result = JsonSerializer.Deserialize<WeChatCode2SessionResponse>(payload, _jsonSerializerOptions);

        return new Code2SessionResult
        {
            OpenId = result?.OpenId,
            SessionKey = result?.SessionKey,
            UnionId = result?.UnionId,
            ErrorCode = result?.ErrorCode,
            ErrorMessage = result?.ErrorMessage,
        };
    }

    public async Task<WeChatPhoneNumberResult> GetPhoneNumberAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new WeChatPhoneNumberResult
            {
                ErrorCode = -1,
                ErrorMessage = "手机号凭证不能为空",
            };
        }

        var accessToken = await GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new WeChatPhoneNumberResult
            {
                ErrorCode = -1,
                ErrorMessage = "微信 access_token 获取失败",
            };
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.weixin.qq.com/wxa/business/getuserphonenumber?access_token={Uri.EscapeDataString(accessToken)}");
        request.Version = HttpVersion.Version11;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        request.Headers.ExpectContinue = false;
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { code = code.Trim() }),
            System.Text.Encoding.UTF8,
            MediaTypeNames.Application.Json);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json)
        {
            CharSet = "utf-8",
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "WeChat getuserphonenumber failed. Status={StatusCode} Reason={ReasonPhrase} Body={Body}",
                (int)response.StatusCode,
                response.ReasonPhrase,
                string.IsNullOrWhiteSpace(payload) ? "<empty>" : payload);
            return new WeChatPhoneNumberResult
            {
                ErrorCode = (int)response.StatusCode,
                ErrorMessage = $"微信手机号接口请求失败: HTTP {(int)response.StatusCode} {response.ReasonPhrase}; Body: {(string.IsNullOrWhiteSpace(payload) ? "<empty>" : payload)}",
            };
        }

        var result = JsonSerializer.Deserialize<WeChatGetPhoneNumberResponse>(payload, _jsonSerializerOptions);
        if (result?.ErrorCode is > 0)
        {
            logger.LogWarning(
                "WeChat getuserphonenumber business error. ErrCode={ErrorCode} ErrMsg={ErrorMessage} Body={Body}",
                result.ErrorCode,
                result.ErrorMessage,
                string.IsNullOrWhiteSpace(payload) ? "<empty>" : payload);
        }
        return new WeChatPhoneNumberResult
        {
            PhoneNumber = result?.PhoneInfo?.PhoneNumber,
            PurePhoneNumber = result?.PhoneInfo?.PurePhoneNumber,
            CountryCode = result?.PhoneInfo?.CountryCode,
            ErrorCode = result?.ErrorCode,
            ErrorMessage = result?.ErrorMessage,
        };
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _accessToken;
        }

        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            return null;
        }

        var requestUrl = $"https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={Uri.EscapeDataString(_options.AppId)}&secret={Uri.EscapeDataString(_options.AppSecret)}";
        using var response = await httpClient.GetAsync(requestUrl, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = JsonSerializer.Deserialize<WeChatAccessTokenResponse>(payload, _jsonSerializerOptions);
        if (result is null || string.IsNullOrWhiteSpace(result.AccessToken) || result.ErrorCode is > 0)
        {
            return null;
        }

        _accessToken = result.AccessToken;
        _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, result.ExpiresIn - 120));
        return _accessToken;
    }
}

public class WeChatPhoneNumberResult
{
    public string? PhoneNumber { get; init; }
    public string? PurePhoneNumber { get; init; }
    public string? CountryCode { get; init; }
    public int? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

internal class WeChatAccessTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("errcode")]
    public int? ErrorCode { get; init; }

    [JsonPropertyName("errmsg")]
    public string? ErrorMessage { get; init; }
}

internal class WeChatGetPhoneNumberResponse
{
    [JsonPropertyName("errcode")]
    public int? ErrorCode { get; init; }

    [JsonPropertyName("errmsg")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("phone_info")]
    public WeChatPhoneInfo? PhoneInfo { get; init; }
}

internal class WeChatPhoneInfo
{
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; init; }

    [JsonPropertyName("purePhoneNumber")]
    public string? PurePhoneNumber { get; init; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }
}
