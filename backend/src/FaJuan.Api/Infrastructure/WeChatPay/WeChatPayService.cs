using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using FaJuan.Api.Contracts;
using Microsoft.Extensions.Options;

namespace FaJuan.Api.Infrastructure.WeChatPay;

public class WeChatPayService(HttpClient httpClient, IOptions<WeChatPayOptions> options)
{
    private readonly WeChatPayOptions _options = options.Value;
    private readonly Dictionary<string, RSA> _platformCertificates = new();

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

    public async Task<bool> VerifyCallbackSignatureAsync(string serial, string timestamp, string nonce, string signature, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serial) || string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(nonce) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        if (!_platformCertificates.TryGetValue(serial, out var rsa))
        {
            rsa = await LoadPlatformCertificateAsync(serial, cancellationToken);
            if (rsa is null)
            {
                return false;
            }
        }

        var message = $"{timestamp}\n{nonce}\n{body}\n";
        var signBytes = Convert.FromBase64String(signature);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        return rsa.VerifyData(messageBytes, signBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public async Task<(bool Success, string Message, WeChatRefundResponse? Result)> RefundAsync(string outTradeNo, decimal amount, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            if (!_options.EnableMockFallback)
            {
                return (false, "微信支付未配置完成，且已关闭模拟支付回退", null);
            }

            return (true, "模拟退款成功", new WeChatRefundResponse
            {
                RefundId = $"MOCK-REFUND-{Guid.NewGuid():N}",
                OutRefundNo = $"REF{DateTime.Now:yyyyMMddHHmmssfff}",
                Status = "SUCCESS",
            });
        }

        var outRefundNo = $"REF{DateTime.Now:yyyyMMddHHmmssfff}";
        var requestModel = new WeChatRefundRequest
        {
            OutRefundNo = outRefundNo,
            OutTradeNo = outTradeNo,
            Reason = reason ?? "运营退款",
            Amount = new WeChatRefundAmount
            {
                Refund = Convert.ToInt32(decimal.Round(amount * 100, 0, MidpointRounding.AwayFromZero)),
                Total = Convert.ToInt32(decimal.Round(amount * 100, 0, MidpointRounding.AwayFromZero)),
            },
        };

        var body = JsonSerializer.Serialize(requestModel);
        var nonce = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var authorization = BuildAuthorization("POST", "/v3/refund/domestic/refunds", timestamp, nonce, body);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mch.weixin.qq.com/v3/refund/domestic/refunds");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (false, $"微信退款申请失败: {payload}", null);
        }

        var refundResponse = JsonSerializer.Deserialize<WeChatRefundResponse>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (refundResponse is null)
        {
            return (false, "微信退款返回解析失败", null);
        }

        return (true, "退款申请成功", refundResponse);
    }

    private async Task<RSA?> LoadPlatformCertificateAsync(string serial, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiV3Key))
        {
            return null;
        }

        var nonce = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var authorization = BuildAuthorization("GET", "/v3/certificates", timestamp, nonce, string.Empty);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.mch.weixin.qq.com/v3/certificates");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var certs = JsonSerializer.Deserialize<WeChatPayCertificatesResponse>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (certs?.Data is null)
        {
            return null;
        }

        foreach (var item in certs.Data)
        {
            if (string.IsNullOrWhiteSpace(item.SerialNo) || item.EncryptCertificate is null)
            {
                continue;
            }

            var plainCert = DecryptCertificate(item.EncryptCertificate);
            if (string.IsNullOrWhiteSpace(plainCert))
            {
                continue;
            }

            var cert = new X509Certificate2(Encoding.UTF8.GetBytes(plainCert));
            var rsa = cert.GetRSAPublicKey();
            if (rsa is not null)
            {
                _platformCertificates[item.SerialNo] = rsa;
            }
        }

        _platformCertificates.TryGetValue(serial, out var result);
        return result;
    }

    private string? DecryptCertificate(WeChatPayEncryptCertificate encryptCertificate)
    {
        if (string.IsNullOrWhiteSpace(encryptCertificate.CipherText)
            || string.IsNullOrWhiteSpace(encryptCertificate.Nonce)
            || string.IsNullOrWhiteSpace(_options.ApiV3Key))
        {
            return null;
        }

        var cipherBytes = Convert.FromBase64String(encryptCertificate.CipherText);
        var nonceBytes = Encoding.UTF8.GetBytes(encryptCertificate.Nonce);
        var aadBytes = Encoding.UTF8.GetBytes(encryptCertificate.AssociatedData ?? string.Empty);
        var keyBytes = Encoding.UTF8.GetBytes(_options.ApiV3Key);

        var cipherText = cipherBytes[..^16];
        var tag = cipherBytes[^16..];
        var plainBytes = new byte[cipherText.Length];

        using var aes = new AesGcm(keyBytes, 16);
        aes.Decrypt(nonceBytes, cipherText, tag, plainBytes, aadBytes);

        return Encoding.UTF8.GetString(plainBytes);
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
