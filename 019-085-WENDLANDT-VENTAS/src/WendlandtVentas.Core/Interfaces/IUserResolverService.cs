using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IUserResolverService
    {
        Task<ApplicationUser> GetUser();
    }
}
