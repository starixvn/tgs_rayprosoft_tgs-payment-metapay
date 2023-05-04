using System.Net;
using System.Web;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.MetaPay;
using Tgs.Payment.MetaPay.Client;

namespace MetaPay.Core.UnitTests;

public class MetaPayClientTests
{
    [Fact]
    public async Task 存款_GCashOnline_成功()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var signer = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var sut = new MetaPayClient(httpClient, signer, NullLogger<MetaPayClient>.Instance);

        var appId = "2081536946682450";
        var privateKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==";
        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";
        var formId = Guid.Parse("f2f8ad10b3544b2cb0a801a7504ae11b");

        _ = mockHttp.Expect(HttpMethod.Post, "http://localhost/openapi/payment/collect/collect")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
				{
				    "amount": "100",
				    "appId": "2081536946682450",
				    "msg": "Repayment code generated successfully",
				    "platRespCode": 0,
				    "referenceNo": "f2f8ad10b3544b2cb0a801a7504ae11b",
				    "sign": "nrjGfYL5KxhyVMeSrn/vMcMJX0hGctxOo4d8RvUEKbQQVGJDKCI/p8N+/iy7/YNoUvF+trZ1PUlPGnI7zD0xvfwXNxaQPDl7BRa/mvhpRpgBNtMcuGv0Jha6Gi+HVP/ZG2gyB+Cv+LdQX8oGWVK7MHnMARAH78YkcdxgMbbi2zd/k/OdR52SNWwfa6/2L81hyB6GAtXtL3b2XMBEtujpwTr3ZSouymjPpkM01WAXDQqDzX7Ac/CAVOlXdWvbhO4YhROzxEN8pmJ/CTU1nQCTQx0xIp+tEiOAnqcO/aw1p/SPdIyBOt9QbKuSBc0F7ap1QuNCyxCeiFOsj0ADhj6WGEWJP0fk45k5DBouVDgBdvBI3rz5kjs+aWtNpgk9dUWCbrZSbfYDMSZTCZfcunPl6F+miMX+Gthn9g5vXLjOfJVcmCIbAJLjWnWazsEA0g2N1CGx34x3qjgiFbtM5DwjBnXVRaMSVwd/7HwOzGpb3KfBlAZXwKta+0WBAxCYTIqk",
				    "transId": "f2f8ad10b3544b2cb0a801a7504ae11b",
				    "url": "https://open-sea-global.alipayplus.com/api/open/v1/ac/cashier/self/codevalue/checkout.htm?codeValue=281666040099hsLH0cQ4A7RL9T9h8zf6H8Lu&loadMode=2"
				}
				"""));

        var order = new DepositOrder(
            MetaPayDepositType.GCashOnline,
            formId,
            100M,
            new Uri("https://dev-payment.rpgp.cc/metapay/deposit/callback"),
            ReturnUrl: new Uri("http://localhost"),
            Mobile: "0922189379",
            UserName: "Satya,Hernandez,Kevin");

        var actual = await sut.DepositAsync(appId, publicKey, privateKey, order);

        Assert.Equal(100M, actual.Amount);
    }

    [Fact]
    public async Task 存款訂單查詢成功()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var signer = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var sut = new MetaPayClient(httpClient, signer, NullLogger<MetaPayClient>.Instance);

        var appId = "2081536946682450";
        var privateKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==";

        var formId = Guid.Parse("ba902203b0534b02a93f2e5e653da49a");

        _ = mockHttp.Expect(HttpMethod.Post, "http://localhost/openapi/payment/collect/queryCollectOrder")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
				{
				    "amount": "120.00",
				    "appId": "2081536946682450",
				    "platRespCode": 0,
				    "referenceNo": "ba902203b0534b02a93f2e5e653da49a",
				    "sign": "m/RZ4fYNhndZc2GIJulY2apNpMNo6FEBSbRvixVtcVsswkuYfLMJnpeTb2WT9xtloTrnyXd7mZ7GslVlJ5Z07+c3qUGy0qf+Uhj9MPF1FJgnoQi0HCBK4uY/VZpUVOsWRqOMeh+4AozXIqOl87VY9Bde3j9SyO66a+b3VpOcvzY=",
				    "status": 1,
				    "transId": "ba902203b0534b02a93f2e5e653da49a"
				}
				"""));

        var actual = await sut.QueryDepositFormStatusAsync(
            appId,
            privateKey,
            thirdPartyOrderNo: "ba902203b0534b02a93f2e5e653da49a",
            formId: formId);

        Assert.Equal(1, actual.Status);
    }

    [Fact]
    public async Task 提款_GCASH_成功()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var signer = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var sut = new MetaPayClient(httpClient, signer, NullLogger<MetaPayClient>.Instance);

        var appId = "2081536946682450";
        var privateKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==";
        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";

        var formId = Guid.Parse("c44fe5a1f8c54304b3290d911166ed12");

        _ = mockHttp.Expect(HttpMethod.Post, "http://localhost/openapi/payment/remit/payout")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
				{
				    "amount": "100",
				    "appId": "2081536946682450",
				    "msg": "Withdrawal code generated successfully",
				    "pickupCenter": 7,
				    "platRespCode": 0,
				    "referenceNo": "c44fe5a1f8c54304b3290d911166ed12",
				    "sign": "N/Zscw2TrkgOM+4qEu3yYGPPK2rKqbVsTHLWmE65alYH6wpUYs9RRS60fIWYQ2cdrAvvv4LEEmhPYM+q+OmYwhvNFgr0ZZURoU7WoU4ouk4qnwvYDsHfF+2NdMYRkzr/7+Z+4SyS9O4GLy6SwDvB0I6cKLuEW0gcHKczTJHhWiY=",
				    "transId": "TTT42304280941038821"
				}
				"""));

        var order = new WithdrawOrder(
            100M,
            MetaPayWithdrawType.GCASH,
            formId,
            AccountNo: "09112233888",
            UserName: "SANTOS,CRUZ,JUAN",
            MobileNumber: "09112233888",
            BankName: null,
            new Uri("https://dev-payment.rpgp.cc/metapay/withdraw/callback"));

        var actual = await sut.WithdrawAsync(appId, publicKey, privateKey, order);

        Assert.Equal(100M, actual.Amount);
    }

    [Fact]
    public void 提款_參數錯誤()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var signer = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var sut = new MetaPayClient(httpClient, signer, NullLogger<MetaPayClient>.Instance);

        var appId = "2081536946682450";
        var privateKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==";
        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";

        var formId = Guid.Parse("c5d9ed5e96d440a4877c84fe6b8b0b35");

        _ = mockHttp.Expect(HttpMethod.Post, "http://localhost/openapi/payment/remit/payout")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
				{
				    "appId": "2081536946682450",
				    "msg": "Withdrawal account parameter error",
				    "platRespCode": -1,
				    "sign": "QLb7qduhrsPy0LrQqEVz0hj5rrt46Z1yfwIULZ9HqRPcCMT5/DL07det1CBhvJvIJBxIp94ldtVay889FfHnxpyMsCVM+q8GY1QQFFZlc+d3sPaSG3w2K/xVh2W+vVzSrPiPgpYL8JDllVJBEKgWZIRflWZM84oicGFWRfy/2A8="
				}
				"""));

        var order = new WithdrawOrder(
            100M,
            MetaPayWithdrawType.BANKRT,
            formId,
            AccountNo: "",
            UserName: "",
            MobileNumber: "09112233888",
            BankName: "Asia United Bank",
            new Uri("https://dev-payment.rpgp.cc/metapay/withdraw/callback"));

        _ = Assert.ThrowsAsync<ApiFailException>(async () => await sut.WithdrawAsync(appId, publicKey, privateKey, order));
    }

    [Fact]
    public void 提款_商戶訂單號重覆()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var signer = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var sut = new MetaPayClient(httpClient, signer, NullLogger<MetaPayClient>.Instance);

        var appId = "2081536946682450";
        var privateKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==";
        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";

        var formId = Guid.Parse("9c8fded3b5a0459f9aa7cc905df031d4");

        _ = mockHttp.Expect(HttpMethod.Post, "http://localhost/openapi/payment/remit/payout")
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
				{
				    "appId": "2081536946682450",
				    "msg": "Duplicate order number",
				    "platRespCode": 5007,
				    "sign": "WQbN+t0hPV8GNxNjprcjc281ae3pMNZ7g0vF97RXdGadNA5XLImfYy5xDavpKXA+a5caMyNRB7FQRGSxmfG6MgozG11mu/jaZKSPPjxjOnxrbOro+XVxcoWprxv3xLTARB0OksarW4PBAuR4s6YeNMDPGhwKlxTPdd3aNdPQo7k="
				}
				"""));

        var order = new WithdrawOrder(
            120M,
            MetaPayWithdrawType.BANKRT,
            formId,
            AccountNo: "",
            UserName: "",
            MobileNumber: "09112233888",
            BankName: "Asia United Bank",
            new Uri("https://dev-payment.rpgp.cc/metapay/withdraw/callback"));

        _ = Assert.ThrowsAsync<ApiFailException>(async () => await sut.WithdrawAsync(appId, publicKey, privateKey, order));
    }

    [Fact]
    public async Task 銀行字典查詢BANKNRT成功()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var signer = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var sut = new MetaPayClient(httpClient, signer, NullLogger<MetaPayClient>.Instance);

        var productType = "BANKNRT";

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["productType"] = productType;

        _ = mockHttp.Expect(HttpMethod.Get, "http://localhost/openapi/metapay/data/bankList")
            .WithQueryString(query.ToString())
            .Respond(HttpStatusCode.OK, new StringContent("""
				{
				    "code": "1000",
				    "message": "success",
				    "records": [
				        {
				            "bankCode": "10047",
				            "bankName": "Al-Amanah Islamic Investment Bank",
				            "brstn": "010360015"
				        },
				        {
				            "bankCode": "10059",
				            "bankName": "ALLBANK (A Thrift Bank), Inc.",
				            "brstn": "011290016"
				        },
				        {
				            "bankCode": "10011",
				            "bankName": "Asia United Bank",
				            "brstn": "011020011"
				        },
				        {
				            "bankCode": "10010",
				            "bankName": "Australia and New Zealand Bank",
				            "brstn": "010700015"
				        },
				        {
				            "bankCode": "10012",
				            "bankName": "Banco De Oro Unibank, Inc.",
				            "brstn": "010530667"
				        },
				        {
				            "bankCode": "10106",
				            "bankName": "BANCO LAGUNA, INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10073",
				            "bankName": "Bangko Kabayan, Inc. (A Private Development Bank)",
				            "brstn": "191600018"
				        },
				        {
				            "bankCode": "10053",
				            "bankName": "Bangko Mabuhay (A Rural Bank), Inc.",
				            "brstn": "419070016"
				        },
				        {
				            "bankCode": "10065",
				            "bankName": "Bangko Nuestra Se\u00f1ora Del Pilar, Inc. (A Rural Bank)",
				            "brstn": "061370012"
				        },
				        {
				            "bankCode": "10013",
				            "bankName": "Bangkok Bank Public Co., Ltd.",
				            "brstn": "010670019"
				        },
				        {
				            "bankCode": "10014",
				            "bankName": "Bank Of America, National Association",
				            "brstn": "010120019"
				        },
				        {
				            "bankCode": "10001",
				            "bankName": "Bank Of China",
				            "brstn": "011140014"
				        },
				        {
				            "bankCode": "10015",
				            "bankName": "Bank Of Commerce",
				            "brstn": "010440016"
				        },
				        {
				            "bankCode": "10009",
				            "bankName": "BANK OF FLORIDA INC.",
				            "brstn": "061400018"
				        },
				        {
				            "bankCode": "10064",
				            "bankName": "Bank of Makati",
				            "brstn": "011210012"
				        },
				        {
				            "bankCode": "10085",
				            "bankName": "BANK OF MONTALBAN, INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10016",
				            "bankName": "Bank Of The Philippine Islands",
				            "brstn": "010040018"
				        },
				        {
				            "bankCode": "10119",
				            "bankName": "BANK OF TOKYO-MITS., LIMITED",
				            "brstn": null
				        },
				        {
				            "bankCode": "10056",
				            "bankName": "BDO Network Bank",
				            "brstn": "31189974"
				        },
				        {
				            "bankCode": "10154",
				            "bankName": "binan rural bank, inc.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10076",
				            "bankName": "BPI Direct Banko, Inc., A Savings Bank",
				            "brstn": null
				        },
				        {
				            "bankCode": "10061",
				            "bankName": "Camalig Bank",
				            "brstn": "161460013"
				        },
				        {
				            "bankCode": "10069",
				            "bankName": "Cantilan Bank, Inc. (A Rural Bank)",
				            "brstn": "591490019"
				        },
				        {
				            "bankCode": "10086",
				            "bankName": "CATHAY UNITED BANK CO. LTD",
				            "brstn": null
				        },
				        {
				            "bankCode": "10054",
				            "bankName": "Cebuana Lhuillier Rural Bank, Inc.",
				            "brstn": "011440015"
				        },
				        {
				            "bankCode": "10004",
				            "bankName": "China Bank Savings",
				            "brstn": "011129996"
				        },
				        {
				            "bankCode": "10017",
				            "bankName": "China Banking Corporation",
				            "brstn": "010100013"
				        },
				        {
				            "bankCode": "10058",
				            "bankName": "CIMB Bank Philippines Inc",
				            "brstn": "018010011"
				        },
				        {
				            "bankCode": "10018",
				            "bankName": "Citibank N.A.",
				            "brstn": "010070017"
				        },
				        {
				            "bankCode": "10131",
				            "bankName": "city savings bank",
				            "brstn": null
				        },
				        {
				            "bankCode": "10071",
				            "bankName": "Community Rural Bank of Romblon, Inc.",
				            "brstn": "291520014"
				        },
				        {
				            "bankCode": "10139",
				            "bankName": "COOPERATIVE BANK OF QUEZON PROVINCE",
				            "brstn": null
				        },
				        {
				            "bankCode": "10067",
				            "bankName": "Country Builders Bank, Inc. (A Rural Bank)",
				            "brstn": "011450018"
				        },
				        {
				            "bankCode": "10041",
				            "bankName": "CTBC Bank (Philippines) Corp.",
				            "brstn": "010690015"
				        },
				        {
				            "bankCode": "10055",
				            "bankName": "DCPay Philippines, Inc.",
				            "brstn": "018030017"
				        },
				        {
				            "bankCode": "10020",
				            "bankName": "Deutsche Bank",
				            "brstn": "010650013"
				        },
				        {
				            "bankCode": "10019",
				            "bankName": "Development Bank of the Philippines",
				            "brstn": "010590018"
				        },
				        {
				            "bankCode": "10122",
				            "bankName": "DEVT. BANK OF THE PHILIPPINES",
				            "brstn": null
				        },
				        {
				            "bankCode": "10091",
				            "bankName": "DM BANK",
				            "brstn": null
				        },
				        {
				            "bankCode": "10072",
				            "bankName": "Dumaguete City Development Bank, Inc.",
				            "brstn": "201530010"
				        },
				        {
				            "bankCode": "10143",
				            "bankName": "DUNGGANON BANK ",
				            "brstn": null
				        },
				        {
				            "bankCode": "10050",
				            "bankName": "Dungganun Bank, Inc.",
				            "brstn": "049020012"
				        },
				        {
				            "bankCode": "10158",
				            "bankName": "EAST WEST BANKING CORPORATION",
				            "brstn": null
				        },
				        {
				            "bankCode": "10057",
				            "bankName": "East West Rural Bank, Inc.",
				            "brstn": "019050012"
				        },
				        {
				            "bankCode": "10021",
				            "bankName": "East-West Banking Corporation",
				            "brstn": "010620014"
				        },
				        {
				            "bankCode": "10005",
				            "bankName": "Equicom  Savings Bank, Inc.",
				            "brstn": "010960017"
				        },
				        {
				            "bankCode": "10127",
				            "bankName": "Equicom Savings Bank",
				            "brstn": null
				        },
				        {
				            "bankCode": "10006",
				            "bankName": "First Consolidated Bank Inc.",
				            "brstn": "020780012"
				        },
				        {
				            "bankCode": "10164",
				            "bankName": "GM BANK OF LUZON,INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10155",
				            "bankName": "GO TYME PHILIPPINES",
				            "brstn": null
				        },
				        {
				            "bankCode": "10066",
				            "bankName": "Guagua Rural Bank, Inc.",
				            "brstn": "061390018"
				        },
				        {
				            "bankCode": "10022",
				            "bankName": "HK and Shanghai Banking Corp.",
				            "brstn": "010060014"
				        },
				        {
				            "bankCode": "10063",
				            "bankName": "HSBC Savings Bank, Inc.",
				            "brstn": "010899997"
				        },
				        {
				            "bankCode": "10068",
				            "bankName": "Industrial & Commercial Bank of China, Ltd",
				            "brstn": "011470014"
				        },
				        {
				            "bankCode": "10045",
				            "bankName": "Industrial Bank of Korea - Manila",
				            "brstn": "011310019"
				        },
				        {
				            "bankCode": "10049",
				            "bankName": "ING Bank N.V.",
				            "brstn": "010660029"
				        },
				        {
				            "bankCode": "10074",
				            "bankName": "Innovative Bank, Inc. (A Rural Bank)",
				            "brstn": "019060015"
				        },
				        {
				            "bankCode": "10023",
				            "bankName": "JPMorgan Chase Bank",
				            "brstn": "010720011"
				        },
				        {
				            "bankCode": "10062",
				            "bankName": "KEB Hana Bank",
				            "brstn": "010710018"
				        },
				        {
				            "bankCode": "10042",
				            "bankName": "Korea Exchange Bank",
				            "brstn": "010710018"
				        },
				        {
				            "bankCode": "10115",
				            "bankName": "LAGUNA PRESTIGE BANKING CORPORATION",
				            "brstn": null
				        },
				        {
				            "bankCode": "10024",
				            "bankName": "Land Bank Of The Philippines",
				            "brstn": "010350025"
				        },
				        {
				            "bankCode": "10157",
				            "bankName": "LOLC BANK PHILIPPINES, INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10098",
				            "bankName": "LULU FINANCIAL SERVICES (PHILS), INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10100",
				            "bankName": "MALARAYAT RURAL BANK, INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10007",
				            "bankName": "Malayan Bank",
				            "brstn": "010820018"
				        },
				        {
				            "bankCode": "10082",
				            "bankName": "MAYA BANK",
				            "brstn": null
				        },
				        {
				            "bankCode": "10025",
				            "bankName": "Maybank Philippines, Inc.",
				            "brstn": "010220016"
				        },
				        {
				            "bankCode": "10003",
				            "bankName": "Mega Intl Comml Bank Co. Ltd",
				            "brstn": "010560019"
				        },
				        {
				            "bankCode": "10026",
				            "bankName": "Metropolitan Bank and Trust Co",
				            "brstn": "010269996"
				        },
				        {
				            "bankCode": "10087",
				            "bankName": "METROSOUTH COOPERATIVE BANK",
				            "brstn": null
				        },
				        {
				            "bankCode": "10163",
				            "bankName": "MINDANAO CONSOLIDATED COOPERATIVE BANK",
				            "brstn": null
				        },
				        {
				            "bankCode": "10027",
				            "bankName": "Mizuho Bank, Ltd.",
				            "brstn": "010640010"
				        },
				        {
				            "bankCode": "10146",
				            "bankName": "MONEY MALL RURAL BANK, INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10040",
				            "bankName": "MUFG Bank, Ltd",
				            "brstn": "010460012"
				        },
				        {
				            "bankCode": "10077",
				            "bankName": "MVSM BANK",
				            "brstn": null
				        },
				        {
				            "bankCode": "10159",
				            "bankName": "NEW RURAL BANK OF SAN LEONARDO (NUEVA ECIJA), INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10084",
				            "bankName": "One Network Bank",
				            "brstn": null
				        },
				        {
				            "bankCode": "10028",
				            "bankName": "Philippine Bank of Communications",
				            "brstn": "010110016"
				        },
				        {
				            "bankCode": "10051",
				            "bankName": "Philippine Business Bank",
				            "brstn": "010970010"
				        },
				        {
				            "bankCode": "10029",
				            "bankName": "Philippine National Bank",
				            "brstn": "010080010"
				        },
				        {
				            "bankCode": "10030",
				            "bankName": "Philippine Savings Bank",
				            "brstn": "010470992"
				        },
				        {
				            "bankCode": "10031",
				            "bankName": "Philippine Trust Company",
				            "brstn": "010090039"
				        },
				        {
				            "bankCode": "10032",
				            "bankName": "Philippine Veterans Bank",
				            "brstn": "010330016"
				        },
				        {
				            "bankCode": "10109",
				            "bankName": "PORAC BANK (A RURAL BANK)",
				            "brstn": null
				        },
				        {
				            "bankCode": "10048",
				            "bankName": "Producers Savings Bank Corporation",
				            "brstn": "011220015"
				        },
				        {
				            "bankCode": "10060",
				            "bankName": "Queen City Development Bank, Inc.",
				            "brstn": "010230019"
				        },
				        {
				            "bankCode": "10075",
				            "bankName": "Rang-Ay Bank, Inc. (A Rural Bank)",
				            "brstn": "019080011"
				        },
				        {
				            "bankCode": "10078",
				            "bankName": "RBT BANK",
				            "brstn": null
				        },
				        {
				            "bankCode": "10033",
				            "bankName": "Rizal Commercial Banking Corp.",
				            "brstn": "010280014"
				        },
				        {
				            "bankCode": "10034",
				            "bankName": "Robinsons Bank Corporation",
				            "brstn": "011070016"
				        },
				        {
				            "bankCode": "10159",
				            "bankName": "RURAL BANK OF BACOLOD CITY",
				            "brstn": null
				        },
				        {
				            "bankCode": "10088",
				            "bankName": "RURAL BANK OF BAUANG, INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10070",
				            "bankName": "Rural Bank of Digos, Inc.",
				            "brstn": "031510019"
				        },
				        {
				            "bankCode": "10079",
				            "bankName": "Rural Bank of Guinobatan, Inc.",
				            "brstn": "161480019"
				        },
				        {
				            "bankCode": "10144",
				            "bankName": "RURAL BANK OF LA PAZ, INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10108",
				            "bankName": "RURAL BANK OF LEBAK (SULTAN KUDARAT), INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10103",
				            "bankName": "RURAL BANK OF ROSARIO (LU), INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10165",
				            "bankName": "RURAL BANK OF SAGAY,INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10135",
				            "bankName": "RURAL BANK OF SAN MEDJUGORJE",
				            "brstn": null
				        },
				        {
				            "bankCode": "10140",
				            "bankName": "RURAL BANK OF STA. IGNACIA, INC. ",
				            "brstn": null
				        },
				        {
				            "bankCode": "10161",
				            "bankName": "SEABANK PH",
				            "brstn": null
				        },
				        {
				            "bankCode": "10035",
				            "bankName": "Security Bank Corporation",
				            "brstn": "010140015"
				        },
				        {
				            "bankCode": "10044",
				            "bankName": "Shinhan Bank",
				            "brstn": "011300016"
				        },
				        {
				            "bankCode": "10002",
				            "bankName": "Sterling Bank Of Asia",
				            "brstn": "011190019"
				        },
				        {
				            "bankCode": "10043",
				            "bankName": "Sumimoto Mitsui Banking Corp",
				            "brstn": "011280013"
				        },
				        {
				            "bankCode": "10162",
				            "bankName": "SUMITOMO MITSUI BANKING CORP",
				            "brstn": null
				        },
				        {
				            "bankCode": "10097",
				            "bankName": "TAGCASH LTD. INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10083",
				            "bankName": "TAYOCASH INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10036",
				            "bankName": "The Standard Chartered Bank",
				            "brstn": "010050011"
				        },
				        {
				            "bankCode": "10008",
				            "bankName": "Tong Yang Savings Bank,Inc.",
				            "brstn": "011130011"
				        },
				        {
				            "bankCode": "10080",
				            "bankName": "Tonik Digital Bank",
				            "brstn": null
				        },
				        {
				            "bankCode": "10039",
				            "bankName": "Union Bank of the Philippines",
				            "brstn": "010419995"
				        },
				        {
				            "bankCode": "10120",
				            "bankName": "UNION DIGITAL BANK",
				            "brstn": null
				        },
				        {
				            "bankCode": "10038",
				            "bankName": "United Coconut Planters Bank",
				            "brstn": "010299995"
				        },
				        {
				            "bankCode": "10037",
				            "bankName": "United Overseas Bank Phils.",
				            "brstn": "010270341"
				        },
				        {
				            "bankCode": "10166",
				            "bankName": "UNO BANK",
				            "brstn": null
				        },
				        {
				            "bankCode": "10116",
				            "bankName": "USSC MONEY SERVICES, INC.",
				            "brstn": null
				        },
				        {
				            "bankCode": "10046",
				            "bankName": "Wealth Development Bank, Inc.",
				            "brstn": "021200054"
				        },
				        {
				            "bankCode": "10052",
				            "bankName": "Yuanta Savings Bank, Inc.",
				            "brstn": "011130011"
				        },
				        {
				            "bankCode": "10125",
				            "bankName": "ZYBI TECH, INC. ",
				            "brstn": null
				        }
				    ],
				    "totalRecords": 121
				}
				"""));

        var actual = await sut.BankListAsync(productType);

        Assert.True(actual.Any());
    }

    [Fact]
    public async Task 銀行字典查詢BANKRT成功()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var signer = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var sut = new MetaPayClient(httpClient, signer, NullLogger<MetaPayClient>.Instance);

        var productType = "BANKRT";

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["productType"] = productType;

        _ = mockHttp.Expect(HttpMethod.Get, "http://localhost/openapi/metapay/data/bankList")
            .WithQueryString(query.ToString())
            .Respond(HttpStatusCode.OK, new StringContent("""
				{
				    "code": "1000",
				    "message": "success",
				    "records": [
				        {
				            "bankCode": "20043",
				            "bankName": "ALLBANK(A Thrift Bank)Inc."
				        },
				        {
				            "bankCode": "20005",
				            "bankName": "Asia United Bank"
				        },
				        {
				            "bankCode": "20035",
				            "bankName": "Bangko Mabuhay(A Rural Bank)Inc "
				        },
				        {
				            "bankCode": "20007",
				            "bankName": "Bank Of Commerce"
				        },
				        {
				            "bankCode": "20041",
				            "bankName": "BDO Network Bank"
				        },
				        {
				            "bankCode": "20006",
				            "bankName": "Banco De Oro Unibank Inc."
				        },
				        {
				            "bankCode": "20053",
				            "bankName": "Binangonan Rural Bank Inc"
				        },
				        {
				            "bankCode": "20008",
				            "bankName": "Bank of The Philippine Islands/BPI Family Bank "
				        },
				        {
				            "bankCode": "20045",
				            "bankName": "BPI Direct Banko Inc.,A Savings Bank "
				        },
				        {
				            "bankCode": "20054",
				            "bankName": "Card Bank Inc "
				        },
				        {
				            "bankCode": "20103",
				            "bankName": "China Bank Savings"
				        },
				        {
				            "bankCode": "20036",
				            "bankName": "CTBC Bank(Philippines)Inc."
				        },
				        {
				            "bankCode": "20040",
				            "bankName": "DCPay Philippines Inc."
				        },
				        {
				            "bankCode": "20118",
				            "bankName": "Dungganon Bank Inc."
				        },
				        {
				            "bankCode": "20042",
				            "bankName": "East West Rural Bank"
				        },
				        {
				            "bankCode": "20019",
				            "bankName": "Equicom Savings Bank"
				        },
				        {
				            "bankCode": "20038",
				            "bankName": "ING BANK N.V. "
				        },
				        {
				            "bankCode": "20030",
				            "bankName": "Isla Bank(A Thrift Bank)Inc. "
				        },
				        {
				            "bankCode": "20025",
				            "bankName": "Land Bank of the Philippines "
				        },
				        {
				            "bankCode": "20003",
				            "bankName": "Malayan Bank Savings And Mortgage Bank Inc. "
				        },
				        {
				            "bankCode": "20010",
				            "bankName": "Maybank Philippines Inc."
				        },
				        {
				            "bankCode": "20031",
				            "bankName": "Partner Rural Bank(Cotabato)Inc."
				        },
				        {
				            "bankCode": "20032",
				            "bankName": "Philippine Business Bank"
				        },
				        {
				            "bankCode": "20013",
				            "bankName": "PNB(Philippine National Bank)"
				        },
				        {
				            "bankCode": "20020",
				            "bankName": "PSBank(Philippine Savings Bank)"
				        },
				        {
				            "bankCode": "20026",
				            "bankName": "Philippine Trust Company"
				        },
				        {
				            "bankCode": "20033",
				            "bankName": "Philippine Veterans Bank"
				        },
				        {
				            "bankCode": "20012",
				            "bankName": "Philippine Bank of Communications"
				        },
				        {
				            "bankCode": "20055",
				            "bankName": "Producers Savings Bank"
				        },
				        {
				            "bankCode": "20047",
				            "bankName": "Queen City Development Bank "
				        },
				        {
				            "bankCode": "20039",
				            "bankName": "Quezon Capital Rural Bank"
				        },
				        {
				            "bankCode": "20021",
				            "bankName": "Robinsons Bank"
				        },
				        {
				            "bankCode": "20048",
				            "bankName": "Rural Bank Of Guinobatan Inc. "
				        },
				        {
				            "bankCode": "20271",
				            "bankName": "SEABANK PH "
				        },
				        {
				            "bankCode": "20014",
				            "bankName": "Security Bank Corporation"
				        },
				        {
				            "bankCode": "20002",
				            "bankName": "Sterling Bank Of Asia"
				        },
				        {
				            "bankCode": "20001",
				            "bankName": "Sun Savings Bank"
				        },
				        {
				            "bankCode": "20058",
				            "bankName": "Tayocash "
				        },
				        {
				            "bankCode": "20022",
				            "bankName": "United Coconut Planters Bank(UCPB)"
				        },
				        {
				            "bankCode": "20029",
				            "bankName": "UCPB Savings Bank Inc."
				        },
				        {
				            "bankCode": "20023",
				            "bankName": "Union Bank of the Philippines "
				        },
				        {
				            "bankCode": "20091",
				            "bankName": "USSC Money Services"
				        },
				        {
				            "bankCode": "20027",
				            "bankName": "Wealth Development Bank Corporation "
				        },
				        {
				            "bankCode": "20073",
				            "bankName": "LULU FINANCIAL SERVICES(PHILS),INC."
				        },
				        {
				            "bankCode": "20011",
				            "bankName": "METROPOLITAN BANK AND TRUST CO"
				        },
				        {
				            "bankCode": "20273",
				            "bankName": "PalawanPay "
				        },
				        {
				            "bankCode": "20274",
				            "bankName": "CIS BAYAD CENTER INC"
				        },
				        {
				            "bankCode": "20024",
				            "bankName": "RIZAL COMMERCIAL BANKING CORP."
				        },
				        {
				            "bankCode": "20052",
				            "bankName": "CAMALIG BANK "
				        },
				        {
				            "bankCode": "20065",
				            "bankName": "CIMB BANK PHILIPPINES INC"
				        },
				        {
				            "bankCode": "20097",
				            "bankName": "DEVT.BANK OF THE PHILIPPINES"
				        },
				        {
				            "bankCode": "20100",
				            "bankName": "ZYBI TECH,INC."
				        },
				        {
				            "bankCode": "20275",
				            "bankName": "ShopeePay Philippines Inc"
				        },
				        {
				            "bankCode": "20104",
				            "bankName": "EAST-WEST BANKING CORPORATION"
				        },
				        {
				            "bankCode": "20276",
				            "bankName": "CARD SME BANK INC"
				        },
				        {
				            "bankCode": "20112",
				            "bankName": "CHINA BANKING CORPORATION"
				        },
				        {
				            "bankCode": "20037",
				            "bankName": "CEBUANA LHUILLIER BANK"
				        },
				        {
				            "bankCode": "20277",
				            "bankName": "Alipay Philippines Inc"
				        },
				        {
				            "bankCode": "20278",
				            "bankName": "LEGAZPI SAVINGS BANK INC"
				        },
				        {
				            "bankCode": "20120",
				            "bankName": "COMMUNITY RURAL BANK OF ROMBLON,INC.(NetBank) "
				        },
				        {
				            "bankCode": "20123",
				            "bankName": "DUMAGUETE CITY DEVELOPMENT BANK,INC."
				        },
				        {
				            "bankCode": "20126",
				            "bankName": "THE STANDARD CHARTERED BANK"
				        },
				        {
				            "bankCode": "20279",
				            "bankName": "MINDANAO CONSOLIDATED COOPERATIVE "
				        }
				    ],
				    "totalRecords": 63
				}
				"""));

        var actual = await sut.BankListAsync(productType);

        Assert.True(actual.Any());
    }
}
