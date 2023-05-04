using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.MetaPay.ApplicationServices;
using Tgs.Payment.MetaPay.Client;
using Tgs.Payment.MetaPay.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Payment.MetaPay.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositController : ControllerBase
{
    private readonly ILogger<DepositController> _logger;

    public DepositController(ILogger<DepositController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("callback", Name = "NotifyDeposit")]
    public async Task<string> DepositCallbackAsync(
        [FromServices] MetaPaySigner signer,
        [FromServices] IGetService<GetDepositFormQuery, DepositForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromBody] CallbackDepositViewModel source)
    {
        _logger.LogInformation($"Deposit Callback: {JsonSerializer.Serialize(source)}");

        var formId = Guid.Parse(source.ReferenceNo);
        var thirdPartyNo = source.TransId;

        var depositForm = await getService
            .GetAsync(new GetDepositFormQuery(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            _logger.LogWarning("Can't found deposit form({id}).", formId);
            return "FAIL";
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(source))!;

        var publicKey = Encoding.UTF8.GetString(Convert.FromBase64String(depositForm.ApplyInfo!.Settings.PublicKey));

        if (!signer.VerifySign(dict, publicKey))
        {
            _logger.LogWarning("Deposit Callback Sign value invalid. input: {sign}", source.Sign);
            return "FAIL";
        }

        var paymentReturn = new MetaPaymentReturn
        {
            FormId = formId,
            Amount = source.Amount,
            AppId = source.AppId,
            Sign = source.Sign,
            Status = source.Status,
            ThirdPartyOrderNo = thirdPartyNo,
            TransactionDate = source.TransactionDate,
        };

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (paymentReturn.Status)
        {
            case 0:
                if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            MetaPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.ApproveDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        decimal.Parse(paymentReturn.Amount),
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            case 2:
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            MetaPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;
        }

        return "SUCCESS";
    }
}
