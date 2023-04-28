namespace Tgs.Payment.MetaPay.Client;
public record MetaPayDepositResponse(

    decimal Amount,
    string? Remark,
    Guid FormId,
    Uri RedirectUrl);
