using Tgs.Payment.Clients;
using Tgs.Payment.MetaPay.Client;

namespace Tgs.Payment.MetaPay;
public record WithdrawForm(
    Guid Id,
    string? UserId,
    UserType UserType,
    Guid BrandId,
    Guid? BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    Guid BankInfoId,
    MetaPayWithdrawApplyInfo? ApplyInfo,
    string? PaymentReturn = null);