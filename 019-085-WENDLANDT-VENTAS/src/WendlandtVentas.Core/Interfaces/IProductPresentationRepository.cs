using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Models.OrderViewModels;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IProductPresentationRepository
    {
        Task<double?> GetLitersByProductPresentationIdAsync(int productPresentationId);
    }
}