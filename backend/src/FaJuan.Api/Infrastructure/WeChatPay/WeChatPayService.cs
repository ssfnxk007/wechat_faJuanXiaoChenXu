using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FaJuan.Api.Contracts;
using Microsoft.Extensions.Options;

namespace FaJuan.Api.Infrastructure.WeChatPay;

public class WeChatPayService(HttpClient httpClient, IOptions<WeChatPayOptions> options)
{
    private readonly WeChatPayOptions _options = options.Value;

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_options.AppId)
            && !string.IsNullOrWhiteSpace(_options.MerchantId)
            && !string.IsNullOrWhiteSpace(_options.MerchantSerialNo)
            && !string.IsNullOrWhiteSpace(_options.PrivateKeyPemPath)
            && !string.IsNullOrWhiteSpace(_options.NotifyUrl);
    }

    public WeChatPayConfigStatusDto GetStatus()
    {
        return new WeChatPayConfigStatusDto
        {
            IsConfigured = IsConfigured(),
            EnableMockFallback = _options.EnableMockFallback,
            AppIdPreview = Preview(_options.AppId),
            MerchantIdPreview = Preview(_options.MerchantId),
            NotifyUrl = _options.NotifyUrl,
            Message = IsConfigured() ? "微信支付配置已就绪" : "请补齐 WeChatPay 配置",
        };
    }

    public async Task<(bool Success, string Message, CreatePaymentResultDto? Result)> CreateJsapiOrderAsync(string outTradeNo, string description, decimal amount, string openId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return (false, "微信支付配置未完成", null);
        }

        var requestModel = new WeChatJsapiPrepayRequest
        {
            AppId = _options.AppId,
            MerchantId = _options.MerchantId,
            Description = description,
            OutTradeNo = outTradeNo,
            NotifyUrl = _options.NotifyUrl,
            Amount = new WeChatAmount
            {
                Total = Convert.ToInt32(decimal.Round(amount * 100, 0, MidpointRounding.AwayFromZero)),
            },
            Payer = new WeChatPayer
            {
                OpenId = openId,
            },
        };

        var body = JsonSerializer.Serialize(requestModel);
        var nonce = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var authorization = BuildAuthorization("POST", "/v3/pay/transactions/jsapi", timestamp, nonce, body);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mch.weixin.qq.com/v3/pay/transactions/jsapi");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (false, $"微信支付下单失败: {payload}", null);
        }

        var prepayResponse = JsonSerializer.Deserialize<WeChatJsapiPrepayResponse>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (string.IsNullOrWhiteSpace(prepayResponse?.PrepayId))
        {
            return (false, "微信支付未返回 prepay_id", null);
        }

        var payTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var payNonce = Guid.NewGuid().ToString("N");
        var packageValue = $"prepay_id={prepayResponse.PrepayId}";
        var paySign = SignMiniProgramPay(_options.AppId, payTimeStamp, payNonce, packageValue);

        return (true, "下单成功", new CreatePaymentResultDto
        {
            PaymentNo = outTradeNo,
            Amount = amount,
            IsMock = false,
            PrepayId = prepayResponse.PrepayId,
            TimeStamp = payTimeStamp,
            NonceStr = payNonce,
            PackageValue = packageValue,
            SignType = "RSA",
            PaySign = paySign,
        });
    }

    public WeChatTransactionResource? TryDecryptCallback(string requestBody)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiV3Key))
        {
            return null;
        }

        var envelope = JsonSerializer.Deserialize<WeChatPayCallbackEnvelope>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (envelope?.Resource is null || string.IsNullOrWhiteSpace(envelope.Resource.CipherText) || string.IsNullOrWhiteSpace(envelope.Resource.Nonce))
        {
            return null;
        }

        var cipherBytes = Convert.FromBase64String(envelope.Resource.CipherText);
        var nonceBytes = Encoding.UTF8.GetBytes(envelope.Resource.Nonce);
        var aadBytes = Encoding.UTF8.GetBytes(envelope.Resource.AssociatedData ?? string.Empty);
        var keyBytes = Encoding.UTF8.GetBytes(_options.ApiV3Key);

        var cipherText = cipherBytes[..^16];
        var tag = cipherBytes[^16..];
        var plainBytes = new byte[cipherText.Length];

        using var aes = new AesGcm(keyBytes, 16);
        aes.Decrypt(nonceBytes, cipherText, tag, plainBytes, aadBytes);

        var plainText = Encoding.UTF8.GetString(plainBytes);
        return JsonSerializer.Deserialize<WeChatTransactionResource>(plainText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
    }

    private string BuildAuthorization(string method, string canonicalUrl, string timestamp, string nonce, string body)
    {
        var message = $"{method}\n{canonicalUrl}\n{timestamp}\n{nonce}\n{body}\n";
        var signature = SignContent(message);
        return $"WECHATPAY2-SHA256-RSA2048 mchid=\"{_options.MerchantId}\",nonce_str=\"{nonce}\",signature=\"{signature}\",timestamp=\"{timestamp}\",serial_no=\"{_options.MerchantSerialNo}\"";
    }

    private string SignMiniProgramPay(string appId, string timestamp, string nonce, string packageValue)
    {
        var message = $"{appId}\n{timestamp}\n{nonce}\n{packageValue}\n";
        return SignContent(message);
    }

    private string SignContent(string content)
    {
        var pem = File.ReadAllText(_options.PrivateKeyPemPath);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        var signed = rsa.SignData(Encoding.UTF8.GetBytes(content), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signed);
    }

    private static string Preview(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (value.Length <= 6) return value;
        return $"{value[..3]}***{value[^3..]}";
    }
}
