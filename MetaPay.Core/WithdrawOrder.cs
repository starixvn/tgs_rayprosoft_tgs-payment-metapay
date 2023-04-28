namespace Tgs.Payment.MetaPay;
public record WithdrawOrder(
    decimal Amount,
    MetaPayWithdrawType PayType,
    Guid FormId,
    string AccountNo,
    string UserName,    
    string MobileNumber,
    string? BankName,
    Uri CallbackUrl);
