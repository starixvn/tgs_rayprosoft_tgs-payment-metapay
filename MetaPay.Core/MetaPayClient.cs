using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using Microsoft.Extensions.Logging;
using Tgs.Payment.MetaPay.Client;

namespace Tgs.Payment.MetaPay;

internal class MetaPayClient
{
    private readonly HttpClient _httpClient;
    private readonly MetaPaySigner _signer;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<MetaPayClient> _logger;

    private readonly IReadOnlyDictionary<MetaPayDepositType, string> _verificationTypeMap =
        new Dictionary<MetaPayDepositType, string>
        {
            [MetaPayDepositType.UnionbankInternetBanking] = "UB_ONLINE"
        };

    public MetaPayClient(
        HttpClient httpClient,
        MetaPaySigner signer,
        ILogger<MetaPayClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<MetaPayClientDepositResponse> DepositAsync(string appId, string publicKey, string privateKey, DepositOrder order, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string?>
        {
            ["appId"] = appId,
            ["channel"] = "3",
            ["referenceNo"] = order.FormId.ToString("N"),
            ["amount"] = order.Amount.ToString(),
            ["mobile"] = order.Mobile,
            ["userName"] = order.UserName,
            ["address"] = "",
            ["remark"] = "",
            ["email"] = "",
            ["productType"] = order.PayType.ToTypeValue(),
            ["notificationURL"] = order.CallbackUrl.AbsoluteUri,
        };

        var sign = _signer.CreateSign(data, privateKey);

        data["sign"] = sign;

        var response = await _httpClient
         .PostAsJsonAsync("/openapi/payment/collect/collect", data, cancellationToken)
         .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("MetaPayClient Deposit Body:{body}", body);

        var node = JsonSerializer.Deserialize<JsonNode>(body, _jsonSerializerOptions);

        if (node is null)
            throw new ApiFailException("Deposit response can't read.");

        if (node["platRespCode"]!.ToString() != "0")
            throw new ApiFailException(node["msg"]!.ToString());

        return new MetaPayClientDepositResponse(
            AppId: node["appId"]!.ToString(),
            PlatRespCode: node["platRespCode"]!.GetValue<int>(),
            FormId: Guid.Parse(node["referenceNo"]!.ToString()),
            ThirdPartyOrderNo: node["transId"]!.ToString(),
            Amount: decimal.Parse(node["amount"]!.ToString()),
            RedirectUrl: new Uri(node["url"]!.ToString()),
            Msg: node["msg"]!.ToString(),
            Sign: node["sign"]!.ToString());
    }

    public async ValueTask<MetaPayClientWithdrawResponse> WithdrawAsync(string appId, string privateKey, WithdrawOrder order, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string?>
        {
            ["appId"] = appId,
            ["pickupCenter"] = order.PayType.ToTypeValue(),
            ["referenceNo"] = order.FormId.ToString("N"),
            ["collectedAmount"] = order.Amount.ToString(),
            ["accountNo"] = order.AccountNo,
            ["userName"] = order.UserName,
            ["birthDate"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["mobileNumber"] = order.MobileNumber,
            ["certificateType"] = "SSS",
            ["certificateNo"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["address"] = "190 Poblacion Street",
            ["city"] = "HOUSTON",
            ["province"] = "TEXAS",
            ["notificationURL"] = order.CallbackUrl.AbsoluteUri,
        };

        if (order.PayType == MetaPayWithdrawType.BANKNRT || order.PayType == MetaPayWithdrawType.BANKRT)
        {
            var productType = order.PayType.ToTypeValue() switch
            {
                "12" => "BANKNRT",
                "8" => "BANKRT",
                _ => throw new NotSupportedException()
            };

            var bankList = await BankListAsync(productType, cancellationToken)
                .ConfigureAwait(false);

            if (!bankList.TryGetValue(order.BankName!, out var bankCode))
                _logger.LogWarning("Bank Code Not Found.");

            data.Add("bankCode", bankCode);
        }

        var sign = _signer.CreateSign(data, privateKey);

        data["sign"] = sign;

        var response = await _httpClient
            .PostAsJsonAsync("/openapi/payment/remit/payout", data)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        _logger.LogInformation("MetaPayClient Withdraw Body:{body}", body);

        var node = JsonSerializer.Deserialize<JsonNode>(body, _jsonSerializerOptions);

        if (node is null)
            throw new ApiFailException("Withdraw response can't read.");

        if (node["platRespCode"]!.ToString() != "0")
            throw new ApiFailException(node["msg"]!.ToString());

        return new MetaPayClientWithdrawResponse(
            AppId: node["appId"]!.ToString(),
            PlatRespCode: int.Parse(node["platRespCode"]!.ToString()),
            ThirdPartyOrderNo: node["transId"]!.ToString(),
            FormId: Guid.Parse(node["referenceNo"]!.ToString()),
            PickupCenter: node["pickupCenter"]!.ToString(),
            Amount: decimal.Parse(node["amount"]!.ToString()),
            Msg: node["msg"]!.ToString(),
            Sign: node["sign"]!.ToString());
    }

    public async ValueTask<Dictionary<string, string>> BankListAsync(string productType, CancellationToken cancellationToken = default)
    {
        var queryStrings = HttpUtility.ParseQueryString(string.Empty);

        queryStrings.Add("productType", productType);

        var response = await _httpClient
            .GetAsync($"/openapi/metapay/data/bankList?{queryStrings}", cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var contentJson = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        var node = JsonSerializer.Deserialize<JsonNode>(contentJson, _jsonSerializerOptions);

        if (node is null)
            throw new ApiFailException("BankList response can't read.");

        var records = node["records"]!.AsArray();

        if (records is null || !records.Any())
            return new Dictionary<string, string>();

        return records.ToDictionary(
            r => r!["bankName"]!.ToString(),
            r => r!["bankCode"]!.ToString());
    }

    public async ValueTask<MetaPayClientQueryDepositResponse> QueryDepositFormStatusAsync(string appId, string privateKey, string thirdPartyOrderNo, Guid formId, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string?>
        {
            ["appId"] = appId,
            ["transId"] = thirdPartyOrderNo,
            ["referenceNo"] = formId.ToString()
        };

        var sign = _signer.CreateSign(data, privateKey);

        data["sign"] = sign;

        var response = await _httpClient
         .PostAsJsonAsync("/openapi/payment/collect/queryCollectOrder", data, cancellationToken)
         .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("MetaPayClient QueryDeposit Body:{body}", body);

        var node = JsonSerializer.Deserialize<JsonNode>(body, _jsonSerializerOptions);

        if (node is null)
            throw new ApiFailException("QueryDeposit response can't read.");

        if (node["platRespCode"]!.ToString() != "0")
            throw new ApiFailException();

        return new MetaPayClientQueryDepositResponse(node["status"]!.GetValue<int>());
    }
}
