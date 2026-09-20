using System;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CaroClient.Settings
{
    public class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            if (string.IsNullOrEmpty(value)) return Color.Empty;
            
            try
            {
                return ColorTranslator.FromHtml(value);
            }
            catch
            {
                return Color.Empty;
            }
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            if (value.IsEmpty)
            {
                writer.WriteStringValue("");
                return;
            }
            writer.WriteStringValue(ColorTranslator.ToHtml(value));
        }
    }
}
