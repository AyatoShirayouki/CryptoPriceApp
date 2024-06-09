using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Utils
{
	public class StringToDecimalConverter : JsonConverter<decimal>
	{
		public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				var stringValue = reader.GetString();
				if (decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
				{
					return value;
				}
				throw new JsonException($"Unable to convert \"{stringValue}\" to {typeToConvert}.");
			}

			return reader.GetDecimal();
		}

		public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
		}
	}
}
