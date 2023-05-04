using System.Security.Cryptography.X509Certificates;
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
public class WithdrawController : ControllerBase
{
    private readonly ILogger<WithdrawController> _logger;

    public WithdrawController(ILogger<WithdrawController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("callback", Name = "NotifyWithdraw")]
    public async Task<string> WithdrawCallbackAsync(
        [FromServices] MetaPaySigner signer,
        [FromServices] IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromBody] CallbackWithdrawViewModel source)
    {        
        _logger.LogInformation($"Withdraw Callback: {JsonSerializer.Serialize(source)}");

        var formId = Guid.Parse(source.ReferenceNo);
        var thirdPartyNo = source.TransId;

        var withdrawForm = await getService
            .GetAsync(new GetWithdrawFormQuery(formId))
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            _logger.LogWarning("Can't found withdraw form({id}).", formId);
            return "FAIL";
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(source))!;

        var publicKey = Encoding.UTF8.GetString(Convert.FromBase64String(withdrawForm.ApplyInfo!.Settings.PublicKey));

        if (!signer.VerifySign(dict, publicKey))
        {
            _logger.LogWarning("Withdraw Callback Sign value invalid. input: {sign}", source.Sign);
            return "FAIL";
        }

        var paymentReturn = new MetaPaymentReturn
        {
            FormId = formId,
            Amount = source.Amount,
            AppId = source.AppId,
            Sign = source.Sign,
            Status = int.Parse(source.Status),
            ThirdPartyOrderNo = thirdPartyNo,
            TransactionDate = source.TransactionDate,
        };

        var fsm = PaymentStateMachineFactory.CreateWithdrawStateMachine(withdrawForm.Status);

        switch (paymentReturn.Status)
        {
            case 0:
                if (fsm.CanFire(PaymentTrigger.PaymentWithdrawCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentWithdrawCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            MetaPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.PaidWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId).ConfigureAwait(false);
                }

                break;

            case 2:
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            MetaPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;
        }

        return "SUCCESS";
    }
}
