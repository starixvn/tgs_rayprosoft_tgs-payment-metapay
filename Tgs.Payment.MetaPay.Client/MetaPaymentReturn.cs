namespace Tgs.Payment.MetaPay;
public record MetaPaymentReturn
{
    public string AppId { get; init; } = default!;
    public int Status { get; init; }
    public Guid FormId { get; init; } = default!;
    public string ThirdPartyOrderNo { get; init; } = default!;
    public string Amount { get; init; } = default!;
    public string TransactionDate { get; init; } = default!;
    public string Sign { get; init; } = default!;
}