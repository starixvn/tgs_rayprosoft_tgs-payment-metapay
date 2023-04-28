using Tgs.Payment.Clients;

namespace Tgs.Payment.MetaPay;
public record DepositForm(
    Guid Id,
    string PlayerCode,
    Guid BrandId,
    Guid BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    MetaPayDepositApplyInfo? ApplyInfo,
    string? PaymentReturn = null);