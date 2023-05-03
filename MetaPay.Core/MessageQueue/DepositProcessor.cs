using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Payment.MetaPay.Client;
using Tgs.Payment.Protos;

namespace Tgs.Payment.MetaPay.MessageQueue;

internal class DepositProcessor : IMessageProcessor
{
    private readonly MetaPayClient _metaPayClient;
    private readonly MetaPayOptions _options;

    public DepositProcessor(MetaPayClient metaPayClient, IOptions<MetaPayOptions> optionsAccessor)
    {
        _metaPayClient = metaPayClient ?? throw new ArgumentNullException(nameof(metaPayClient));
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

        var msg = new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo)
        };

        return msg.ToByteArray();
    }
}