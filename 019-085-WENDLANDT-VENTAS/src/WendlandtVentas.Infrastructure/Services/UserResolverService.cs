using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Interfaces;

namespace WendlandtVentas.Infrastructure.Services
{
    public class UserResolverService : IUserResolverService
    {
        private readonly IHttpContextAccessor _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserResolverService(IHttpContextAccessor context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ApplicationUser> GetUser()
        {
            if (_context.HttpContext == null)
                return null;

            var identityName = _context.HttpContext.User?.Identity?.Name;

            if (identityName != null)
                return await _userManager.FindByEmailAsync(identityName);

            return null;
        }
    }
}
