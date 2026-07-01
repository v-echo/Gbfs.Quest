using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gbfs.Quest.Data
{
    internal sealed class UnixEpochConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Convention: if we encounter a timestamp, assume it's in seconds (experimentally that seems to be the case). In a real-life scenario, we'd have some logic to determine if that's the case.
            if (reader.TokenType == JsonTokenType.String)
                return reader.GetDateTime();

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt64(out ulong ts))
                return DateTime.UnixEpoch.AddSeconds(ts);

            return default;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            Span<byte> date = new byte[29];

            bool result = Utf8Formatter.TryFormat(value, date, out _, new StandardFormat('R'));
            Debug.Assert(result);

            writer.WriteStringValue(date);
        }
    }
}
