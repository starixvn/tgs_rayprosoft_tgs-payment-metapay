using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Payment.MetaPay.JsonConverters;

internal class MetaPayWithdrawTypeConverter : JsonConverter<MetaPayWithdrawType>
{
    public override MetaPayWithdrawType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var readPaymentType = reader.GetString();

        return MetaPayWithdrawType.Parse(readPaymentType!);
    }

    public override void Write(Utf8JsonWriter writer, MetaPayWithdrawType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToTypeValue());
}
