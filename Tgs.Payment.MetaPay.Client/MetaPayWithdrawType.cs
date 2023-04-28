namespace Tgs.Payment.MetaPay;

public class MetaPayWithdrawType
{
    public static readonly MetaPayWithdrawType GCASH = new("7");//电子钱包
    public static readonly MetaPayWithdrawType BANKRT = new("8");//instapay银行实时
    public static readonly MetaPayWithdrawType BANKNRT = new("12");//pesonet银行非实时
    public static readonly MetaPayWithdrawType PAYMAYA = new("9");//电子钱包
    public static readonly MetaPayWithdrawType GRABPAY = new("11");//电子钱包

    private static readonly IReadOnlyDictionary<string, MetaPayWithdrawType> _PaymentTypes = new Dictionary<string, MetaPayWithdrawType>
    {
        ["7"] = GCASH,
        ["8"] = BANKRT,
        ["12"] = BANKNRT,
        ["9"] = PAYMAYA,
        ["11"] = GRABPAY,
    };

    private readonly string _typeValue;

    private MetaPayWithdrawType(string typeValue)
    {
        _typeValue = typeValue;
    }

    public static MetaPayWithdrawType? Parse(string typeValue)
        => _PaymentTypes.TryGetValue(typeValue, out var result)
            ? result
            : null;

    public string ToTypeValue()
        => _typeValue;
}
