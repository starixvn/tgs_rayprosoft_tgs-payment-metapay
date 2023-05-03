namespace Tgs.Payment.MetaPay.Client;
public record MetaPayDepositResponse(
    decimal Amount,
    Guid FormId,
    string ThirdPartyOrderNo,
    Uri RedirectUrl);
