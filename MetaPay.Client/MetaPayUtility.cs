using System.Text.Json;
using System.Text.Json.Serialization;
using Tgs.Payment.MetaPay.Client.JsonConverters;

namespace Tgs.Payment.MetaPay.Client;

public static class MetaPayUtility
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = {
            new MetaPayDepositTypeConverter(),
            new MetaPayWithdrawTypeConverter()
        }
    };
}
