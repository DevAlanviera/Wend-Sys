using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models;
using WendlandtVentas.Core.Services;
using WendlandtVentas.Infrastructure.libs;

namespace WendlandtVentas.Infrastructure.Services
{
    public class LogBookService : ILogBookService
    {
        private readonly IUserResolverService _userResolverService;
        private readonly LogBookServerSettings _logBookServerSettings;
        private readonly IHttpIdentityServerService _httpIdentityServerService;
        private readonly IdentityServerSettings _identityServerSettings;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new LogBookSerializeContractResolver()
        };

        public LogBookService(IUserResolverService userResolverService, IOptions<LogBookServerSettings> logBookServerSettings, IHttpIdentityServerService httpIdentityServerService, IOptions<IdentityServerSettings> identityServerSettings)
        {

            JsonSerializerSettings.Converters.Add(new CustomStringEnumConverter());
            _userResolverService = userResolverService;
            _logBookServerSettings = logBookServerSettings.Value;
            _httpIdentityServerService = httpIdentityServerService;
            _identityServerSettings = identityServerSettings.Value;

        }

        public async Task<List<LogBookModel>> CreateLogBook(ChangeTracker changeTracker)
        {
            var logs = new List<LogBookModel>();
            var entries = changeTracker.Entries();

            //First get user claims    

            var usuario = await _userResolverService.GetUser();
            //if (usuario == null) return logs;
            var action = "";
            foreach (var entry in entries)
            {
                var advance = false;
                switch (entry.State)
                {
                    case EntityState.Added:
                        action = "Agregó ";
                        advance = true;
                        break;

                    case EntityState.Modified:
                        action = "Modificó ";
                        advance = true;
                        break;
                    case EntityState.Deleted:
                        action = "Eliminó ";
                        advance = true;
                        break;
                }

                if (!advance) continue;

                var result = JsonConvert.SerializeObject(entry.Entity, Formatting.Indented, JsonSerializerSettings);

                if (entry.Entity != null &&
                    entry.Entity.GetType().GetProperty("LastUpdatedDateTime") != null)
                    ((dynamic)entry.Entity).LastUpdatedDateTime = DateTime.UtcNow;

                var logBook = new LogBookModel
                {
                    UserId = usuario == null ? "0" : usuario.Id,
                    User = usuario == null ? "system" :usuario.UserName,
                    ClientId = _identityServerSettings.ClientId,
                    ActionType = action,
                    Target = entry.Entity?.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true)
                            .Select(x => ((DisplayNameAttribute)x).DisplayName)
                            .DefaultIfEmpty(entry.Entity?.GetType().Name)
                            .First(),
                    Json = result
                };
                logs.Add(logBook);
            }

            return logs;
        }

        public async Task<bool> SendPost(List<LogBookModel> logs)
        {
            var request = new RestRequest();
            var token = await _httpIdentityServerService.GetToken();
            request.AddHeader("Authorization", $"Bearer {token}");
            var body = JsonConvert.SerializeObject(logs);
            request.AddJsonBody(body);
            var restClient = new RestClient();
            restClient.BaseUrl = new Uri($"{_logBookServerSettings.Server}/api/logbook");
            var result = await restClient.ExecutePostAsync(request);

            if (!result.IsSuccessful)
            {
                Console.WriteLine(result.Content);
            }
            else
            {
                var content = result.Content;
                Console.WriteLine(content);
            }

            return result.IsSuccessful;
        }

        public async Task<LogBookModel> GetLog(int Id, string clientId)
        {
            var request = new RestRequest();
            var token = await _httpIdentityServerService.GetToken();
            request.AddHeader("Authorization", $"Bearer {token}");
            dynamic data = new ExpandoObject();
            data.Id = Id;
            data.ClientId = clientId;
            var body = JsonConvert.SerializeObject(data);
            request.AddJsonBody(body);
            var restClient = new RestClient();
            restClient.BaseUrl = new Uri($"{_logBookServerSettings.Server}/api/logbook/GetLog");
            var result = await restClient.ExecutePostAsync<LogBookModel>(request);

            if (!result.IsSuccessful)
            {
                Console.WriteLine(result.Content);
            }

            return result.Data;
        }

        public async Task<LogBookResModel> GetData(LogBookFilterModel filters)
        {
            var request = new RestRequest();
            var token = await _httpIdentityServerService.GetToken();
            request.AddHeader("Authorization", $"Bearer {token}");
            var body = JsonConvert.SerializeObject(filters);
            request.AddJsonBody(body);
            var restClient = new RestClient();
            restClient.BaseUrl = new Uri($"{_logBookServerSettings.Server}/api/logbook/GetData");
            var result = await restClient.ExecutePostAsync<LogBookResModel>(request);

            if (!result.IsSuccessful)
            {
                Console.WriteLine(result.Content);
            }

            return result.Data;
        }
    }
}
