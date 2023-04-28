using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Payment.Protos;

namespace Tgs.Payment.MetaPay.MessageQueue;

internal class WithdrawProcessor : IMessageProcessor
{
    private readonly MetaPayClient _metaPayClient;
    private readonly MetaPayOptions _options;

    public WithdrawProcessor(MetaPayClient metaPayClient, IOptions<MetaPayOptions> optionsAccessor)
    {
        _metaPayClient = metaPayClient ?? throw new ArgumentNullException(nameof(metaPayClient));
        _options = optionsAccessor.Value;
    }

    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = WithdrawRequest.Parser.ParseFrom(data.Span);
        var settings = JsonSerializer.Deserialize<MetaPayWithdrawSettings>(
            request.ApplyInfo, 
            MetaPayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            decimal.Parse(request.Amount),
            settings.PaymentType,
            settings.FormId,
            settings.AccountNo,
            settings.UserName,
            settings.MobileNumber,
            settings.BankName,
            new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"));

        var withdrawResponse = await _metaPayClient.WithdrawAsync(
            settings.AppId,
            Encoding.UTF8.GetString(Convert.FromBase64String(settings.PrivateKey)),
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new MetaPayWithdrawApplyInfo(
            settings,
            new MetaPayWithdrawResponse(
                withdrawResponse.FormId));

        var msg = new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo)
        };

        return msg.ToByteArray();
    }
}
