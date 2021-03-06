﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DuetAPI.Utility
{
    /// <summary>
    /// Class to convert enums to and from lowercase JSON strings
    /// </summary>
    public class JsonLowerCaseStringEnumConverter : JsonConverter<object>
    {
        /// <summary>
        /// Checks if the type can be converted
        /// </summary>
        /// <param name="typeToConvert">Type to convert</param>
        /// <returns>True if the type can be converted</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        /// <summary>
        /// Read an enum value from JSON
        /// </summary>
        /// <param name="reader">JSON reader</param>
        /// <param name="typeToConvert">Type to convert</param>
        /// <param name="options">Read options</param>
        /// <returns>Deserialized enum value</returns>
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32();
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                return Enum.Parse(typeToConvert, reader.GetString(), true);
            }
            throw new JsonException($"Invalid {typeToConvert.Name}");
        }

        /// <summary>
        /// Write an enum value to lowercase JSON
        /// </summary>
        /// <param name="writer">JSON writer</param>
        /// <param name="value">Value to write</param>
        /// <param name="options">Write options</param>
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(Enum.GetName(value.GetType(), value).ToLowerInvariant());
        }
    }
}
