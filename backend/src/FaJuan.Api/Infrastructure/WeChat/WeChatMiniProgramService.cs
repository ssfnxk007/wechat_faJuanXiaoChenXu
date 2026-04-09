using System.Text.Json;
using FaJuan.Api.Contracts;
using Microsoft.Extensions.Options;

namespace FaJuan.Api.Infrastructure.WeChat;

public class WeChatMiniProgramService(HttpClient httpClient, IOptions<WeChatMiniProgramOptions> options)
{
    private readonly WeChatMiniProgramOptions _options = options.Value;

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

        var result = JsonSerializer.Deserialize<WeChatCode2SessionResponse>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        return new Code2SessionResult
        {
            OpenId = result?.OpenId,
            SessionKey = result?.SessionKey,
            UnionId = result?.UnionId,
            ErrorCode = result?.ErrorCode,
            ErrorMessage = result?.ErrorMessage,
        };
    }
}
