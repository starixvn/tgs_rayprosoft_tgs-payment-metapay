namespace Tgs.Payment.MetaPay;
public record MetaPayWithdrawSettings(
    string AppId,
    string PrivateKey,
    string PublicKey,
    MetaPayWithdrawType PaymentType,
    Guid FormId,
    string AccountNo,
    string UserName,
    string MobileNumber,
    string? BankName);
