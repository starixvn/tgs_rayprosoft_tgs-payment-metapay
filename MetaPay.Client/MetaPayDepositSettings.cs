namespace Tgs.Payment.MetaPay.Client;
public record MetaPayDepositSettings(
    string AppId,
    string PrivateKey,
    string PublicKey,
    MetaPayDepositType PaymentType,
    Uri ReturnUrl,
    Guid FormId,
    string Phone,
    string RealName);