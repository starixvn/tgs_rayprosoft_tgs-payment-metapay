namespace Tgs.Payment.MetaPay;
public record DepositOrder(
    MetaPayDepositType PayType,
    Guid FormId,
    decimal Amount,
    Uri CallbackUrl,
    Uri ReturnUrl,
    string Mobile,
    string UserName);