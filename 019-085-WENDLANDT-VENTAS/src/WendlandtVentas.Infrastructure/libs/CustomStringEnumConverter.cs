using Humanizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace WendlandtVentas.Infrastructure.libs
{
    public class CustomStringEnumConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var array = new JArray();
            using (var tempWriter = array.CreateWriter())
                base.WriteJson(tempWriter, value, serializer);
            var token = array.Single();

            if (token.Type == JTokenType.String && value != null)
            {
                var enumType = value.GetType();
                var prefix = (Enum)Enum.Parse(enumType, value.ToString());
                token = prefix.Humanize();
            }

            token.WriteTo(writer);
        }
    }
}
