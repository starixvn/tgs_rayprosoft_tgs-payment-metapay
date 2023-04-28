using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Payment.MetaPay;

namespace MetaPay.Core.UnitTests;

public class SignerTests
{
    [Fact]
    public void Create_Sign()
    {
        var privateKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==";

        var dict = new Dictionary<string, string?>
        {
            { "appId", "2081536946682450"},
            { "channel", 3.ToString() },
            { "referenceNo", "871232323122" },
            { "amount", "1000" },
            { "mobile","09123123123" },
            { "userName","SANTOS,CRUZ,JUAN" },
            { "address","address" },
            { "remark","remark" },
            { "email","werwe@gmail.com" },
            { "productType","CASHIER_DESK"},
        };

        var expected = "vdN/hulucS/63//662PPI7wvjGb9x77ELFXa8kmbOxj0UCYurbuOGdYRif//8aen+0rcvaxWFluqBHyv1ZfXAv1dsLKA/vdLW+s3pBUVEojuahDnH2fPIqFuguI4IbUgHsILC656X/NbJz9xGUbqLoiWA66fz3Dp6IKFbmqy0ZA=";

        var sut = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var actual = sut.CreateSign(dict, privateKey);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Verify_Deposit_Sign()
    {
        var dict = new Dictionary<string, object>
        {
            { "msg","Repayment code generated successfully" },
            { "amount","1000" },
            { "referenceNo","871232323122" },
            { "transId","871232323122" },
            { "appId","2081536946682450" },
            { "sign","BLQmlc/GDHd3Udqtm70kR8KZzBu0392JW0ng5Zo6m5QqMnYRfIPA6BSkDDsS9aPkw4SONAj/qC8z20cMSm4zwOJkeFgcrxLDnKr3Bf7Gd1qpUUqCmkTW/Jq20UyvwMZVmjFg/zdq/j/P5jvZr5sOIx1Nrtcb9lyRVIf+SRFRv4WHY9hjf7wqPVeYqk3/Vy/eC5Iq1LIrbpxnLCdiNYh8Ms5dJO3upOjyTpuU3h4ZLFNo+KbB2FeTLdlSiWMoZykZCTXt9OWo8HsvTxlju0fFk6hEy4xO7epkjReSKtYBFWgdYdToTzVJHgl3/Rr5U8tItyMPUdGyUmlfgTBKPwvNHg==" },
            { "platRespCode",0 },
            { "url", "https://merchant-test.globalcbtis.com/front/alipayplus/cashier/e1b18109b5c8f8b27fa541366f6a2a7251e4db8a4eb2d70289b74518b778fa9f"}
        };

        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";

        var sut = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var actual = sut.VerifySign(dict, publicKey);

        Assert.True(actual);
    }

    [Fact]
    public void Verify_Deposit_Callback_Sign()
    {
        var dict = new Dictionary<string, object>
        {
            { "appId", "2081536946682450" },
            { "status", "0" },
            { "referenceNo", "65c7fdc6bd4f4f90b47bc46e04d88a9c" },
            { "transId", "276501765834" },
            { "amount", "100.00"},
            { "transactionDate", "2023-04-27 18:16:35" },
            { "sign", "w/Q5rPSvmiFRshDgB5yG5hXJNmVNsdps6UjO7GlQqqcOZuzKFzBx4jBm4QuVWbzkGn3hVrMLH4VDvP48r4BJiVa6glDv34HxWetYvlXngYa1Wqj2uhOBaFgnR2aDmdkGZC1egR+m5/JUXIw7Xb4opmJWKvwbLV5S7yTbPVprNMk=" }
        };

        var sut = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";
        var actual = sut.VerifySign(dict, publicKey);

        Assert.True(actual);
    }

    [Fact]
    public void Verify_Withdraw_Callback_Sign()
    {
        var dict = new Dictionary<string, object>
        {
            { "appId", "2081536946682450" },
            { "status", "0" },
            { "referenceNo", "c44fe5a1f8c54304b3290d911166ed12" },
            { "transId", "TTT42304280941038821" },
            { "amount", "100.00"},
            { "transactionDate", "2023-04-28 09:59:12" },
            { "sign", "r50F9dzV0RQIPtgOG+rFWZt4nJNVtMLb/3QXOUfb6S4pSIPCBeqwRsMJQVuml0QIjVcyb9fitQsz7AQcd5q4wDLucTNH3hDqQg88vd45FXdVtF1m5VeekuiGDepaSIFHalJCxP5ReKDu1n8eENbjA/fSbrp26Cu86svXNwdRGHE=" }
        };

        var sut = new MetaPaySigner(NullLogger<MetaPaySigner>.Instance);

        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";
        var actual = sut.VerifySign(dict, publicKey);

        Assert.True(actual);
    }
}
