using Humanizer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WendlandtVentas.Web.Extensions
{
    public static class KeyValuePairExtensions
    {

        public static string ExractContent(this KeyValuePair<string, string> element)
        {
            var html = string.Empty;

            try
            {
                var token = JToken.Parse(element.Value).Value<JObject>();
                var innerElements = token.Properties()
                    .Select(p => new KeyValuePair<string, string>(p.Name, p.Value?.ToString() ?? string.Empty))
                    .Where(x => !string.IsNullOrEmpty(x.Value));

                foreach (var innerElement in innerElements)
                {
                    html += "<dl class='row'>";
                    html += $"<dt class='col-sm-1'>{innerElement.Key.Humanize()}</dt>";
                    html += $"<dd class='col-sm-11'>{innerElement.ExractContent()}</dd>";
                    html += "</dl>";
                }

                return html;
            }
            catch (Exception)
            {
                switch (element.Value)
                {
                    case "True":
                        return "Sí";
                    case "False":
                        return "No";
                }

                return element.Value;
            }
        }
    }
}
