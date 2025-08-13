using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using WendlandtVentas.Core;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models.OrderViewModels;

namespace WendlandtVentas.Infrastructure.Services
{
    public class TreasuryApi : ITreasuryApi
    {
        private readonly TreasuryServerSettings _treasuryServerSettings;
        private readonly ILogger<TreasuryApi> _logger;

        public TreasuryApi(IOptions<TreasuryServerSettings> treasuryServerSettings, ILogger<TreasuryApi> logger)
        {
            _treasuryServerSettings = treasuryServerSettings.Value;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, string Response)> AddIncomeAsync(OrdersIncomeDto income)
        {
            income.TransferToken = _treasuryServerSettings.TransferToken;
            try
            {
                var request = new RestRequest();
                request.AddJsonBody(income);
                Debug.WriteLine(JsonConvert.SerializeObject(income));
                var restClient = new RestClient($"{_treasuryServerSettings.Server}{_treasuryServerSettings.Endpoint}");
                var result = await restClient.ExecutePostAsync(request);

                return (result.IsSuccessful, result.Content);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error inesperado al enviar la información a tesorería");
                return (false, "Error inesperado al enviar la información a tesorería");
            }
        }

        public async Task<(bool IsSuccess, string Response)> GetBalancesAsync(OrdersIncomeDto income)
        {
            income.TransferToken = _treasuryServerSettings.TransferToken;
            try
            {
                var request = new RestRequest();
                request.AddJsonBody(income);
                Debug.WriteLine(JsonConvert.SerializeObject(income));
                var restClient = new RestClient($"{_treasuryServerSettings.Server}{_treasuryServerSettings.Endpoint}/GetBalances");
                var result = await restClient.ExecutePostAsync(request);

                return (result.IsSuccessful, result.Content);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error inesperado al enviar la información a tesorería");
                return (false, "Error inesperado al enviar la información a tesorería");
            }
        }

        public async Task<(bool IsSuccess, OrderWithShipmentsDto Response)> GetOrderWithShipmentsAsync(int orderId)
        {
            var dto = new OrderIdRequestDto
            {
                OrderId = orderId,
                TransferToken = _treasuryServerSettings.TransferToken
            };

            try
            {
                var request = new RestRequest();
                request.AddJsonBody(dto);
                var restClient = new RestClient($"{_treasuryServerSettings.Server}{_treasuryServerSettings.Endpoint}/GetOrderWithShipments");
                var result = await restClient.ExecutePostAsync(request);

                if (result.IsSuccessful)
                {
                    var response = JsonConvert.DeserializeObject<OrderWithShipmentsDto>(result.Content);
                    return (true, response);
                }

                return (false, null);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al consultar orden con shipments en tesorería");
                return (false, null);
            }
        }
    }
}