using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gbfs.Quest.Data
{
    /// <summary>
    /// A converter to handle v3.0's "name" being an array of type ["language":"en", "text":"actual_name"]. We get the first name, regardless of language.
    /// </summary>
    internal class NameArrayConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String) // 2.x is just a string
                return reader.GetString();

            if (reader.TokenType == JsonTokenType.StartArray) // 3.x is an array
            {
                string? str = null;

                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("text")) // Read tokens until we get to the actual name property
                    {
                        reader.Read(); // Need one more read to get the property value
                        str = reader.GetString();
                    }

                    reader.Read();
                }

                return str;
            }

            return null;
        }

        // N/A for this assignment, since we don't write to JSON
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
