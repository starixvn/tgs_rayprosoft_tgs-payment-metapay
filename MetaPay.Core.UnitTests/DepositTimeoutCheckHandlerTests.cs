using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.MetaPay;
using Tgs.Payment.MetaPay.Client;
using Tgs.Payment.MetaPay.MessageQueue;
using Tgs.Payment.Protos;

namespace MetaPay.Core.UnitTests;

public class DepositTimeoutCheckHandlerTests
{
    [Fact]
    public async Task 檢查已經申請十分鐘的單子是否已經完成()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var fakePhpGoClient = new MetaPayClient(
            httpClient,
            new MetaPaySigner(NullLogger<MetaPaySigner>.Instance),
            NullLogger<MetaPayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();

        var sut = new DepositTimeoutCheckHandler(
            fakePaymentServiceClient,
            fakePhpGoClient,
            fakePaymentClient,
            NullLogger<DepositTimeoutCheckHandler>.Instance);

        var request = new ScheduleRequest
        {
            Subject = "test.channel",
            FormId = Guid.NewGuid(),
            ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var applyInfo = new MetaPayDepositApplyInfo(
            new MetaPayDepositSettings(
                "PGTest",
                "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==",
                "publicKey",
                MetaPayDepositType.GCashOnline,
                new Uri("http://localhost"),
                request.FormId.ToGuid(),
                "",
                ""),
            new MetaPayDepositResponse(
                1000M,
                request.FormId.ToGuid(),
                "thirdPartyOrderNo",
                new Uri("http://abc.cc")));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new DepositFormInfo
            {
                Id = request.FormId.ToGuid(),
                Amount = 1000M,
                BrandId = Guid.NewGuid(),
                ThirdPartyOrderNo = "thirdPartyOrderNo",
                ApplyInfo = JsonSerializer.Serialize(applyInfo, MetaPayUtility.SerializerOptions)
            });

        _ = mockHttp.When("http://localhost/openapi/payment/collect/queryCollectOrder")
            .Respond(new StringContent($$"""
				{
				    "appId": "appId",
				    "status": 2,
					"referenceNo": "{{request.FormId.ToGuid()}}",
				    "transId": "thirdPartyOrderNo",
					"amount": "1000",
					"platRespCode": 0,
					"sign": "sign"
				}
				"""));

        // Act
        await sut.HandleAsync(request.ToByteArray());

        // Assert
        _ = fakePaymentClient.Received(1)
            .RejectDepositAsync(
                Arg.Is(request.FormId.ToGuid()),
                Arg.Any<Guid>());
    }
}
