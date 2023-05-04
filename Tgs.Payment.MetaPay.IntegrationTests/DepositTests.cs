using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.MetaPay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.MetaPay.IntegrationTests;

public class DepositTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DepositTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 代收回調_訂單狀態成功()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var signer = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        await using var web = MetaPayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();        

        var formId = Guid.Parse("65c7fdc6bd4f4f90b47bc46e04d88a9c");
        var privateKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==";
        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";

        var request = """
            {
                "amount":"100.00",
                "referenceNo":"65c7fdc6bd4f4f90b47bc46e04d88a9c",
                "transId":"276501765834",
                "appId":"2081536946682450",
                "sign":"w/Q5rPSvmiFRshDgB5yG5hXJNmVNsdps6UjO7GlQqqcOZuzKFzBx4jBm4QuVWbzkGn3hVrMLH4VDvP48r4BJiVa6glDv34HxWetYvlXngYa1Wqj2uhOBaFgnR2aDmdkGZC1egR+m5/JUXIw7Xb4opmJWKvwbLV5S7yTbPVprNMk=",
                "transactionDate":"2023-04-27 18:16:35",
                "status":"0"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var applyInfo = new MetaPayDepositApplyInfo(
            new MetaPayDepositSettings(
                "2081536946682450",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey)),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey)),
                MetaPayDepositType.Cashier_Desk,
                ReturnUrl: new Uri("https://dev-payment.rpgp.cc/metapay/deposit/callback"),
                formId,
                Phone: "0922189379",
                "Satya,Hernandez,Kevin"),
             new MetaPayDepositResponse(
                100M,                
                formId,
                "276501765834",
                new Uri("https://merchant-test.globalcbtis.com/front/alipayplus/cashier/e1b18109b5c8f8b240755103cee8291255a07d72582ad3a089b74518b778fa9f")));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = Guid.NewGuid(),
                Status = PaymentFlowState.WaitPayment,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, MetaPayUtility.SerializerOptions)
            });

        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("SUCCESS", actual);

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .ApproveDepositAsync(
                Arg.Is(formId),
                Arg.Any<Guid>(),
                Arg.Is(applyInfo.Response.Amount),
                Arg.Any<CancellationToken>());
    }
}
