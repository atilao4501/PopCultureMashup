using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PopCultureMashup.Infrastructure.External.DTOs;

namespace PopCultureMashup.Infrastructure.External.Converters
{
    /// <summary>
    /// OpenLibrary "description" can be either a string or an object { "value": "..." }.
    /// This converter normalizes both into DescriptionUnion { value = "..." }.
    /// </summary>
    public sealed class DescriptionUnionConverter : JsonConverter<OpenLibWorkDto.DescriptionUnion?>
    {
        public override OpenLibWorkDto.DescriptionUnion? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            // Case 1: description is a plain string
            if (reader.TokenType == JsonTokenType.String)
            {
                return new OpenLibWorkDto.DescriptionUnion { value = reader.GetString() };
            }

            // Case 2: description is an object (common shape: { "type": "...", "value": "..." })
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                string? val = null;
                if (root.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String)
                    val = v.GetString();

                // Fallback: keep the raw object as string if no "value" string was found
                return new OpenLibWorkDto.DescriptionUnion { value = val ?? root.ToString() };
            }

            // Case 3: null
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            throw new JsonException($"Unsupported token for description: {reader.TokenType}");
        }

        public override void Write(
            Utf8JsonWriter writer,
            OpenLibWorkDto.DescriptionUnion? value,
            JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // serialize back as a simple string
            writer.WriteStringValue(value.value);
        }
    }
}