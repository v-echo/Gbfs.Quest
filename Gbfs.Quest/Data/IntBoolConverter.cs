using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gbfs.Quest.Data
{
    internal class IntBoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
                return reader.GetBoolean();
            if (reader.TokenType == JsonTokenType.Null)
                return false;
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (!reader.TryGetInt32(out int result)) 
                    return false;

                return result switch
                {
                    0 => false,
                    1 => true,
                    _ => throw new Exception("Numeric non-bit token can't be parsed as boolean!"),
                };
            }

            throw new Exception("Unknown token can't be parsed as boolean!");
        }

        // N/A for this assignment, since we don't write to JSON
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
