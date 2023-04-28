namespace Tgs.Payment.MetaPay.Client;

public class MetaPayDepositType
{
    public static readonly MetaPayDepositType UnionbankInternetBanking = new("UB_ONLINE");//银行
    public static readonly MetaPayDepositType InstapayUusingAnyBankOrEwallet = new("INSTAPAY");
    public static readonly MetaPayDepositType PESONetFromAnyBankOrEwallet = new("PESONET");
    public static readonly MetaPayDepositType GCashOnline = new("GCASH_ONLINE");//电子钱包
    public static readonly MetaPayDepositType BPIOnlineMobile = new("BPI_ONLINE");//BPI银行
    public static readonly MetaPayDepositType RCBCOnlineBanking = new("RCBC_ONLINE");//银行
    public static readonly MetaPayDepositType GrabPayOnline = new("GRABPAY_ONLINE");//电子钱包
    public static readonly MetaPayDepositType Coins = new("COINS");//电子钱包
    public static readonly MetaPayDepositType Paymaya = new("PAYMAYA");//电子钱包
    public static readonly MetaPayDepositType QrCode = new("QRCODE");//扫码支付，支持所有电子钱包，银行等通道
    public static readonly MetaPayDepositType Other = new("OTHER");//7-Eleven ,Ecpay,BDO BANK等渠道
    public static readonly MetaPayDepositType Cashier_Desk = new("CASHIER_DESK");//收银台，整合以上所有渠道

    private static readonly IReadOnlyDictionary<string, MetaPayDepositType> _PaymentTypes = new Dictionary<string, MetaPayDepositType>
    {
        ["UB_ONLINE"] = UnionbankInternetBanking,
        ["INSTAPAY"] = InstapayUusingAnyBankOrEwallet,
        ["PESONET"] = PESONetFromAnyBankOrEwallet,
        ["GCASH_ONLINE"] = GCashOnline,
        ["BPI_ONLINE"] = BPIOnlineMobile,
        ["RCBC_ONLINE"] = RCBCOnlineBanking,
        ["GRABPAY_ONLINE"] = GrabPayOnline,
        ["COINS"] = Coins,
        ["PAYMAYA"] = Paymaya,
        ["QRCODE"] = QrCode,
        ["OTHER"] = Other,
        ["CASHIER_DESK"] = Cashier_Desk,
    };

    private readonly string _typeValue;

    private MetaPayDepositType(string typeValue)
    {
        _typeValue = typeValue;
    }

    public static MetaPayDepositType? Parse(string typeValue)
        => _PaymentTypes.TryGetValue(typeValue, out var result)
            ? result
            : null;

    public string ToTypeValue()
        => _typeValue;
}