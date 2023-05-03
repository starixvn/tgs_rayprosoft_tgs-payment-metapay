using System.Text.Json.Serialization;
using Tgs.Payment.MetaPay.Client.JsonConverters;

namespace Tgs.Payment.MetaPay.Client;
public record MetaPayWithdrawSettings(
    string AppId,
    string PrivateKey,
    string PublicKey,
    [property: JsonConverter(typeof(MetaPayWithdrawTypeConverter))]
    MetaPayWithdrawType PaymentType,
    Guid FormId,
    string AccountNo,
    string UserName,
    string MobileNumber,
    string? BankName);
