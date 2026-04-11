namespace FaJuan.Api.Domain.Enums;

public enum CouponIssueImportDetailStatus
{
    PendingMatch = 1,
    MatchedAndGranted = 2,
    MatchFailed = 3,
    Expired = 4,
}
