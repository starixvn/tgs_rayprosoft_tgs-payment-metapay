using System.Text.Json.Serialization;

namespace Tgs.Payment.MetaPay.ViewModels;

public class CallbackWithdrawViewModel
{
    [JsonPropertyName("appId")]
    public string AppId { get; init; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; init; } = default!;

    [JsonPropertyName("referenceNo")]
    public string ReferenceNo { get; init; } = default!;

    [JsonPropertyName("transId")]
    public string TransId { get; init; } = default!;

    [JsonPropertyName("transactionDate")]
    public string TransactionDate { get; init; } = default!;

    [JsonPropertyName("amount")]
    public string Amount { get; init; } = default!;

    [JsonPropertyName("msg")]
    public string? Msg { get; init; }

    [JsonPropertyName("sign")]
    public string Sign { get; init; } = default!;
}