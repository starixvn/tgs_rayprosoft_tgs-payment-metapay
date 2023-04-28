using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Payment.MetaPay.Client.JsonConverters;

internal class MetaPayDepositTypeConverter : JsonConverter<MetaPayDepositType>
{
    public override MetaPayDepositType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var readPaymentType = reader.GetString();

        return MetaPayDepositType.Parse(readPaymentType!);
    }

    public override void Write(Utf8JsonWriter writer, MetaPayDepositType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToTypeValue());
}