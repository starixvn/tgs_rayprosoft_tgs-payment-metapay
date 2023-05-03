using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Payment.Clients;
using Tgs.Payment.MetaPay.Client;
using Tgs.Payment.Protos;

namespace Tgs.Payment.MetaPay.MessageQueue;

internal class DepositProcessor : IMessageProcessor
{
    private readonly MetaPayClient _metaPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly MetaPayOptions _options;

    public DepositProcessor(
        MetaPayClient metaPayClient,
        IPaymentClient paymentClient,
        IOptions<MetaPayOptions> optionsAccessor)
    {
        _metaPayClient = metaPayClient ?? throw new ArgumentNullException(nameof(metaPayClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _options = optionsAccessor.Value;
    }

    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = DepositRequest.Parser.ParseFrom(data.Span);

        var settings = JsonSerializer.Deserialize<MetaPayDepositSettings>(
            request.ApplyInfo,
            MetaPayUtility.SerializerOptions)!;

        var order = new DepositOrder(
            settings.PaymentType,
            settings.FormId,
            decimal.Parse(request.Amount),
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.ReturnUrl,
            settings.Phone,
            UserName: settings.RealName);

        var depositResponse = await _metaPayClient.DepositAsync(
            settings.AppId,
            Encoding.UTF8.GetString(Convert.FromBase64String(settings.PublicKey)),
            Encoding.UTF8.GetString(Convert.FromBase64String(settings.PrivateKey)),
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new MetaPayDepositApplyInfo(
            settings,
            new MetaPayDepositResponse(
                depositResponse.Amount,                
                depositResponse.FormId,
                depositResponse.ThirdPartyOrderNo,
                depositResponse.RedirectUrl));

        await _paymentClient.ScheduleAsync(
            "tgs.payment.metapay.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(11D),
            cancellationToken).ConfigureAwait(false);

        var msg = new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo,MetaPayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}