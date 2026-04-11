namespace FaJuan.Api.Contracts;

public class CreatePaymentRequest
{
    public long OrderId { get; init; }
}

public class CreatePaymentResultDto
{
    public long PaymentTransactionId { get; init; }
    public string PaymentNo { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public bool IsMock { get; init; }
    public string? MockPayToken { get; init; }
    public string? PrepayId { get; init; }
    public string? TimeStamp { get; init; }
    public string? NonceStr { get; init; }
    public string? PackageValue { get; init; }
    public string? SignType { get; init; }
    public string? PaySign { get; init; }
}

public class PaymentCallbackRequest
{
    public string PaymentNo { get; init; } = string.Empty;
    public string? ChannelTradeNo { get; init; }
    public bool Success { get; init; }
    public string? RawCallback { get; init; }
}

public class WeChatPayCallbackHeaders
{
    public string? Timestamp { get; init; }
    public string? Nonce { get; init; }
    public string? Signature { get; init; }
    public string? Serial { get; init; }
}

public class RefundOrderRequest
{
    public long OrderId { get; init; }
}
