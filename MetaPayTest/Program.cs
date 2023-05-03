using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.MetaPay;
using Tgs.Payment.MetaPay.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddPaymentClient()
    .AddMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigQueueOptions((opt, _) => opt.Url = "nats://localhost:4222"))
    .AddNatsGlobPatternExchange("*");

var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var appId = "2081536946682450";

var privateKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMFZTH39KdpP4/XAZ39qCGKv1LdoRMrTiwx0fwvcAsroGQ/p5lLhxBuQTSUCCAnrLp1DccGVrmvSJIYD9pzVSJBfFAgDzR9ufXF2YbMu8bECl3iZuIUOQusSTrjQNYfb+2QSiXuqhVm5+mBzULcB2Oi13cRAwgmfzin8gZWl7BaZAgMBAAECgYBEa+19yFE/SHZLSUNW+5bsFX0ZSnTNsf7K8Y7kC3+RfoEf+I3hVrOnbXFfX1Co8ekVDgD7mJSY40y5gD2NUozSrJ+AYvfYSNZJIADVmyZtQb3H48V06RyZY8z9BnIQyNU765ocM6J0tgXfyjTv5WGPPtPFknpMTc9zRDYF+YbEAQJBAOsImNamUPtIDdB4YzmIIjbEQp+d0hdmyPSOpimJE0Zz0RVGyu4JJwcV3nybHmwP7KVghul42ST2TSorsHRKwvECQQDSmMHkYncyJIBHObKNhQQ94h3aKqzMdMmKYowfZf7W/IcPCCU8uIHnfqBXFnHhn8hmpJNe4bUz+uiGpI1VhL4pAkAbtDa3zoDrLbIbKaWgiXBm/jlQMT0rbv0u/oWNjc/M1SgHFwIijsef6S8BhxNN746Ya8lbSdhmp/Wui9nPy2QxAkBSNi/66DV/7Z/mLi/17ydowQzGTkKGzlCfJTluSHLjmlFfHN7shVqw9E3DsY9jdOzWpQii0ZnbYOfFKBfEer2BAkEA6QctcSyGWoer3ZYVcYEGj1w2tzwRXeFFsezCx+/phbYTNOJLMIK84jQ6h/fCDo+W73vd22nZwWBIQTaClUG91w==";

var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDag5pcNyOafbbfPjKyxqmEbh7dNstiHrXIQDHHpTft312+XVmwVtx1+VH37RJ6BiLB/y05fi662Ds5iaY7Sw8z59ir3OkFnPjG7E5ezkzRrCq6j+e4aMO8gD05YbYLBROh3Sw9WXqVtUAyENPBFhuAavmc/AjqyEydMR20oLWRwQIDAQAB";

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var paymentClient = app.Services.GetRequiredService<IPaymentClient>();

//Deposit
var depositSettings = new MetaPayDepositSettings(
   appId,
   Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey)),
   Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey)),
   MetaPayDepositType.Cashier_Desk,
   new Uri("http://localhost"),
   Guid.NewGuid(),
   "0922189379",
   "Satya,Hernandez,Kevin");

var applyInfo = JsonSerializer.Serialize(depositSettings, MetaPayUtility.SerializerOptions);
logger.LogInformation("ApplyInfo: {applyInfo}", applyInfo);

var depositReturn = await paymentClient.DepositAsync(
    "tgs.payment.metapay.deposit",
    100M,
    applyInfo).ConfigureAwait(false);

logger.LogInformation("DepositReturn: {depositReturn}", depositReturn);


//Withdraw GCASH
//var withdrawSettings = new MetaPayWithdrawSettings(
//    appId,
//    Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey)),
//    Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey)),
//    PaymentType: MetaPayWithdrawType.GCASH,
//    FormId: Guid.NewGuid(),
//    AccountNo: "09112233888",
//    UserName: "SANTOS,CRUZ,JUAN",
//    MobileNumber: "09112233888",
//    BankName: null);

//var applyInfo = JsonSerializer.Serialize(withdrawSettings, MetaPayUtility.SerializerOptions);
//logger.LogInformation("ApplyInfo: {applyInfo}", applyInfo);

//var withdrawReturn = await paymentClient.WithdrawAsync(
//    "tgs.payment.metapay.withdraw",
//    100M,
//    applyInfo).ConfigureAwait(false);

//logger.LogInformation("WithdrawReturn: {withdrawReturn}", withdrawReturn);


//Withdraw BANKRT
//var withdrawSettings = new MetaPayWithdrawSettings(
//    appId,
//    Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey)),
//    Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey)),
//    PaymentType: MetaPayWithdrawType.BANKRT,
//    FormId: Guid.NewGuid(),
//    AccountNo: "09112233888",
//    UserName: "SANTOS,CRUZ,JUAN",
//    MobileNumber: "",
//    BankName: "Asia United Bank");//BankCode:10011

//var applyInfo = JsonSerializer.Serialize(withdrawSettings, MetaPayUtility.SerializerOptions);
//logger.LogInformation("ApplyInfo: {applyInfo}", applyInfo);

//var withdrawReturn = await paymentClient.WithdrawAsync(
//    "tgs.payment.metapay.withdraw",
//    100M,
//    applyInfo).ConfigureAwait(false);

//logger.LogInformation("WithdrawReturn: {withdrawReturn}", withdrawReturn);



Console.WriteLine("Hello, MetaPay!");
