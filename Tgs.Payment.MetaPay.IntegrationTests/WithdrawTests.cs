using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Xunit.Abstractions;

namespace Tgs.Payment.MetaPay.IntegrationTests;

public class WithdrawTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public WithdrawTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 代付回調_訂單狀態成功()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = MetaPayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("c44fe5a1f8c54304b3290d911166ed12");
        var privateKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==";
        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";

        var settings = new MetaPayWithdrawSettings(
            "2081536946682450",
            privateKey,
            publicKey,
            MetaPayWithdrawType.GCASH,
            formId,
            "09112233888",
            "SANTOS,CRUZ,JUAN",
            "09112233888",
            null);

        var withdrawResponse = new MetaPayWithdrawResponse(
            formId);

        var applyInfo = new MetaPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = Guid.NewGuid(),
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, MetaPayUtility.SerializerOptions)
            });

        var request = """
            {
                "amount": "100.00",
                "referenceNo": "c44fe5a1f8c54304b3290d911166ed12",
                "transId": "TTT42304280941038821",
                "appId": "2081536946682450",
                "sign": "r50F9dzV0RQIPtgOG+rFWZt4nJNVtMLb/3QXOUfb6S4pSIPCBeqwRsMJQVuml0QIjVcyb9fitQsz7AQcd5q4wDLucTNH3hDqQg88vd45FXdVtF1m5VeekuiGDepaSIFHalJCxP5ReKDu1n8eENbjA/fSbrp26Cu86svXNwdRGHE=",
                "transactionDate": "2023-04-28 09:59:12",
                "status": "0"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("SUCCESS", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .PaidWithdrawAsync(
                Arg.Is(formId),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代付回調_訂單狀態失敗()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = MetaPayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Text.Plain));

        var formId = Guid.Parse("c44fe5a1f8c54304b3290d911166ed12");
        var privateKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==";
        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";

        var settings = new MetaPayWithdrawSettings(
            "2081536946682450",
            privateKey,
            publicKey,
            MetaPayWithdrawType.GCASH,
            formId,
            "09112233888",
            "SANTOS,CRUZ,JUAN",
            "09112233888",
            null);

        var withdrawResponse = new MetaPayWithdrawResponse(
            formId);

        var applyInfo = new MetaPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = Guid.NewGuid(),
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, MetaPayUtility.SerializerOptions)
            });

        var request = """
            {
                "amount": "100.00",
                "referenceNo": "c44fe5a1f8c54304b3290d911166ed12",
                "transId": "TTT42304280941038821",
                "appId": "2081536946682450",
                "sign": "MDPPBGbdW5Hrgn/eYBfkKrgY+Hf0M7Cd5ODINQfCoQFS5R4/XGqP3+rD0YlmfgpPtci68NUZN4fluo5IFj9rA+fXBYsg7rsNluFLzY1qq8Ww2asfHVvZ/f/R5h6i5WjxIYQ+XHio6o8xCCLONA8/FkiRuMXBE8sb3HcQrg4sMgE=",
                "transactionDate": "2023-04-28 09:45:58",
                "status": "2"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("SUCCESS", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectWithdrawAsync(
                Arg.Is(formId),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }
}
