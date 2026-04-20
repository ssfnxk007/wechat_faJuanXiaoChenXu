using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using FaJuan.Api.Application.Common;
using FaJuan.Api.Contracts;

namespace FaJuan.Api.Infrastructure.WeChatPay;

public class WeChatPayService(HttpClient httpClient, WeChatPaySettingsProvider settingsProvider)
{
    private readonly Dictionary<string, RSA> _platformCertificates = new();

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var opts = await settingsProvider.GetAsync(cancellationToken);
        return opts.IsConfigured;
    }

    public async Task<WeChatPayConfigStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var opts = await settingsProvider.GetAsync(cancellationToken);
        return new WeChatPayConfigStatusDto
        {
            IsConfigured = opts.IsConfigured,
            EnableMockFallback = opts.EnableMockFallback,
            AppIdPreview = Preview(opts.AppId),
            MerchantIdPreview = Preview(opts.MerchantId),
            NotifyUrl = opts.NotifyUrl,
            Message = opts.IsConfigured ? "微信支付配置已就绪" : "请补齐 WeChatPay 配置",
        };
    }

    public async Task<(bool Success, string Message, CreatePaymentResultDto? Result)> CreateJsapiOrderAsync(string outTradeNo, string description, decimal amount, string openId, CancellationToken cancellationToken = default)
    {
        var opts = await settingsProvider.GetAsync(cancellationToken);
        if (!opts.IsConfigured)
        {
            return (false, "微信支付配置未完成", null);
        }

        var requestModel = new WeChatJsapiPrepayRequest
        {
            AppId = opts.AppId,
            MerchantId = opts.MerchantId,
            Description = description,
            OutTradeNo = outTradeNo,
            NotifyUrl = opts.NotifyUrl,
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
        var authorization = BuildAuthorization("POST", "/v3/pay/transactions/jsapi", timestamp, nonce, body, opts);

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
        var paySign = SignMiniProgramPay(opts.AppId, opts.PrivateKeyPem, payTimeStamp, payNonce, packageValue);

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

    public async Task<WeChatTransactionResource?> TryDecryptCallbackAsync(string requestBody, CancellationToken cancellationToken = default)
    {
        var opts = await settingsProvider.GetAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(opts.ApiV3Key))
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
        var keyBytes = Encoding.UTF8.GetBytes(opts.ApiV3Key);

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

        var opts = await settingsProvider.GetAsync(cancellationToken);

        if (!_platformCertificates.TryGetValue(serial, out var rsa))
        {
            rsa = await LoadPlatformCertificateAsync(serial, opts, cancellationToken);
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
        var opts = await settingsProvider.GetAsync(cancellationToken);
        if (!opts.IsConfigured)
        {
            if (!opts.EnableMockFallback)
            {
                return (false, "微信支付未配置完成，且已关闭模拟支付回退", null);
            }

            return (true, "模拟退款成功", new WeChatRefundResponse
            {
                RefundId = $"MOCK-REFUND-{Guid.NewGuid():N}",
                OutRefundNo = OrderNoGenerator.Create("REF"),
                Status = "SUCCESS",
            });
        }

        var outRefundNo = OrderNoGenerator.Create("REF");
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
        var authorization = BuildAuthorization("POST", "/v3/refund/domestic/refunds", timestamp, nonce, body, opts);

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

    private async Task<RSA?> LoadPlatformCertificateAsync(string serial, WeChatPaySettingsSnapshot opts, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(opts.ApiV3Key))
        {
            return null;
        }

        var nonce = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var authorization = BuildAuthorization("GET", "/v3/certificates", timestamp, nonce, string.Empty, opts);

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

            var plainCert = DecryptCertificate(item.EncryptCertificate, opts);
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

    private string? DecryptCertificate(WeChatPayEncryptCertificate encryptCertificate, WeChatPaySettingsSnapshot opts)
    {
        if (string.IsNullOrWhiteSpace(encryptCertificate.CipherText)
            || string.IsNullOrWhiteSpace(encryptCertificate.Nonce)
            || string.IsNullOrWhiteSpace(opts.ApiV3Key))
        {
            return null;
        }

        var cipherBytes = Convert.FromBase64String(encryptCertificate.CipherText);
        var nonceBytes = Encoding.UTF8.GetBytes(encryptCertificate.Nonce);
        var aadBytes = Encoding.UTF8.GetBytes(encryptCertificate.AssociatedData ?? string.Empty);
        var keyBytes = Encoding.UTF8.GetBytes(opts.ApiV3Key);

        var cipherText = cipherBytes[..^16];
        var tag = cipherBytes[^16..];
        var plainBytes = new byte[cipherText.Length];

        using var aes = new AesGcm(keyBytes, 16);
        aes.Decrypt(nonceBytes, cipherText, tag, plainBytes, aadBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private string BuildAuthorization(string method, string canonicalUrl, string timestamp, string nonce, string body, WeChatPaySettingsSnapshot opts)
    {
        var message = $"{method}\n{canonicalUrl}\n{timestamp}\n{nonce}\n{body}\n";
        var signature = SignContent(message, opts.PrivateKeyPem);
        return $"WECHATPAY2-SHA256-RSA2048 mchid=\"{opts.MerchantId}\",nonce_str=\"{nonce}\",signature=\"{signature}\",timestamp=\"{timestamp}\",serial_no=\"{opts.MerchantSerialNo}\"";
    }

    private static string SignMiniProgramPay(string appId, string privateKeyPem, string timestamp, string nonce, string packageValue)
    {
        var message = $"{appId}\n{timestamp}\n{nonce}\n{packageValue}\n";
        return SignContent(message, privateKeyPem);
    }

    private static string SignContent(string content, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
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
