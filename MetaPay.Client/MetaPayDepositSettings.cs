using System.Text.Json.Serialization;
using Tgs.Payment.MetaPay.Client.JsonConverters;

namespace Tgs.Payment.MetaPay.Client;
public record MetaPayDepositSettings(
    string AppId,
    string PrivateKey,
    string PublicKey,
    [property: JsonConverter(typeof(MetaPayDepositTypeConverter))]
    MetaPayDepositType PaymentType,
    Uri ReturnUrl,
    Guid FormId,
    string Phone,
    string RealName);