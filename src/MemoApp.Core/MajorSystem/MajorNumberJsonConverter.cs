using System.Text.Json;
using System.Text.Json.Serialization;

namespace MemoApp.Core.MajorSystem;

/// <summary>
/// JSON converter for MajorNumber struct to ensure proper serialization/deserialization.
/// </summary>
public class MajorNumberJsonConverter : JsonConverter<MajorNumber>
{
    public override MajorNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of JSON object for MajorNumber");
        }

        int value = 0;
        bool isZeroPrefixed = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "Value":
                        value = reader.GetInt32();
                        break;
                    case "IsZeroPrefixed":
                        isZeroPrefixed = reader.GetBoolean();
                        break;
                    default:
                        // Skip unknown properties
                        reader.Skip();
                        break;
                }
            }
        }

        return new MajorNumber(value, isZeroPrefixed);
    }

    public override void Write(Utf8JsonWriter writer, MajorNumber value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("Value", value.Value);
        writer.WriteBoolean("IsZeroPrefixed", value.IsZeroPrefixed);
        writer.WriteEndObject();
    }
}