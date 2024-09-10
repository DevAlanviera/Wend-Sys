using Xunit;
using WendlandtVentas.Infrastructure.Services;
using WendlandtVentas.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WendlandtVentas.Core.Models.Enums;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Assert = Xunit.Assert;

namespace WendlandtVentas.Tests.Functionality
{
    public class NotificationShould
    {
        private OneSignalConfiguration _oneSignalConfiguration;

        public NotificationShould()
        {
            _oneSignalConfiguration = ConfigurationHelper.GetApplicationConfiguration(TestContext.CurrentContext.TestDirectory);
        }

        [Fact]
        public async Task ValidateSendNotificationAsync()
        {
            var mockLogger = new Mock<ILogger<OneSignalService>>();
            var mockOneSignalConfiguration = Options.Create(new OneSignalConfiguration()
            {
                apiKey = _oneSignalConfiguration.apiKey,
                appId = _oneSignalConfiguration.appId
            });            
            var oneSignalService = new OneSignalService(mockLogger.Object, mockOneSignalConfiguration);
            var result = await oneSignalService.SendNotificationAsync(Tag.email, "contacto@monobits.mx", "Prueba", "Probando notificación");
            
            Assert.True(result);
        }   
    }
}