using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models;
using WendlandtVentas.Core.Models.Enums;

namespace WendlandtVentas.Infrastructure.Services
{
    public class OneSignalService : IOneSignalService
    {
        private readonly OneSignalConfiguration _oneSignalConfiguration;
        private readonly ILogger<OneSignalService> _logger;
        private IRestClient restClient { get; set; }
        public OneSignalService(ILogger<OneSignalService> logger, IOptions<OneSignalConfiguration> oneSignalConfiguration)
        {
            _logger = logger;
            restClient = new RestClient();
            restClient.BaseUrl = new Uri("https://onesignal.com/api/v1/notifications");
            _oneSignalConfiguration = oneSignalConfiguration.Value;
        }

        public async Task<bool> SendNotificationAsync(Tag tag, string tagValue, string title, string message)
        {
            try
            {
                var webAlert = new OneSignalPayloadModel()
                {
                    app_id = _oneSignalConfiguration.appId,
                    url = "https://wendlandt.mnbt.net/Order",
                    isAnyWeb = true,
                    isAndroid = false,
                    isIos = false,
                    contents = new Dictionary<string, object>
                    {
                        { "en", message }
                    },
                    headings = new Dictionary<string, object>
                    {
                        { "en", title }
                    },
                    filters = new object[]
                    {
                        new {field = "tag", key = tag.ToString(), relation = "=", value = tagValue},
                    },
                };

                var request = new RestRequest();
                request.AddHeader("Authorization", $"Basic {_oneSignalConfiguration.apiKey}");
                request.AddJsonBody(webAlert);
                var result = await restClient.ExecutePostAsync(request);

                return result.IsSuccessful;
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error: {e.Message}");
                return false;
            }
        }
        private struct OneSignalPayloadModel
        {
            public IDictionary<string, object> data { get; set; }
            public string url { get; set; }
            public string app_id { get; set; }
            public IDictionary<string, object> contents { get; set; }
            public IDictionary<string, object> headings { get; set; }
            public object[] filters { get; set; }
            public string[] included_segments { get; set; }
            public bool isIos { get; set; }
            public bool isAndroid { get; set; }
            public bool isAnyWeb { get; set; }
        }
    }
}
