namespace Tgs.Payment.MetaPay;
public record MetaPayDepositSettings(
    string AppId,
    string PrivateKey,
    string PublicKey,
    MetaPayDepositType PaymentType,
    Uri ReturnUrl,
    Guid FormId,
    string Phone,
    string RealName);