namespace Tgs.Payment.MetaPay;

public class MetaPayOptions
{
    public Uri PaymentApiUrl { get; set; } = default!;
    public Uri CallbackBaseUrl { get; set; } = default!;
    public Uri PaymentServiceBaseUrl { get; set; } = default!;
}