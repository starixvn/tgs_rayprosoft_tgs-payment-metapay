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
                "TUlJQ2RnSUJBREFOQmdrcWhraUc5dzBCQVFFRkFBU0NBbUF3Z2dKY0FnRUFBb0dCQU1GWlRIMzlLZHBQNC9YQVozOXFDR0t2MUxkb1JNclRpd3gwZnd2Y0Fzcm9HUS9wNWxMaHhCdVFUU1VDQ0FuckxwMURjY0dWcm12U0pJWUQ5cHpWU0pCZkZBZ0R6Ujl1ZlhGMlliTXU4YkVDbDNpWnVJVU9RdXNTVHJqUU5ZZmIrMlFTaVh1cWhWbTUrbUJ6VUxjQjJPaTEzY1JBd2dtZnppbjhnWldsN0JhWkFnTUJBQUVDZ1lCRWErMTl5RkUvU0haTFNVTlcrNWJzRlgwWlNuVE5zZjdLOFk3a0MzK1Jmb0VmK0kzaFZyT25iWEZmWDFDbzhla1ZEZ0Q3bUpTWTQweTVnRDJOVW96U3JKK0FZdmZZU05aSklBRFZteVp0UWIzSDQ4VjA2UnlaWTh6OUJuSVF5TlU3NjVvY002SjB0Z1hmeWpUdjVXR1BQdFBGa25wTVRjOXpSRFlGK1liRUFRSkJBT3NJbU5hbVVQdElEZEI0WXptSUlqYkVRcCtkMGhkbXlQU09waW1KRTBaejBSVkd5dTRKSndjVjNueWJIbXdQN0tWZ2h1bDQyU1QyVFNvcnNIUkt3dkVDUVFEU21NSGtZbmN5SklCSE9iS05oUVE5NGgzYUtxek1kTW1LWW93ZlpmN1cvSWNQQ0NVOHVJSG5mcUJYRm5IaG44aG1wSk5lNGJVeit1aUdwSTFWaEw0cEFrQWJ0RGEzem9EckxiSWJLYVdnaVhCbS9qbFFNVDByYnYwdS9vV05qYy9NMVNnSEZ3SWlqc2VmNlM4Qmh4Tk43NDZZYThsYlNkaG1wL1d1aTluUHkyUXhBa0JTTmkvNjZEVi83Wi9tTGkvMTd5ZG93UXpHVGtLR3psQ2ZKVGx1U0hMam1sRmZITjdzaFZxdzlFM0RzWTlqZE96V3BRaWkwWm5iWU9mRktCZkVlcjJCQWtFQTZRY3RjU3lHV29lcjNaWVZjWUVHajF3MnR6d1JYZUZGc2V6Q3grL3BoYllUTk9KTE1JSzg0alE2aC9mQ0RvK1c3M3ZkMjJuWndXQklRVGFDbFVHOTF3PT0=",
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
