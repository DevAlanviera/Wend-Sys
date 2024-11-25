using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Infrastructure.Data;

namespace WendlandtVentas.Infrastructure.Repositories
{

    //Desarrollamos este repositorio para acceder al dato ProductPresentationId y poder obtener los demas datos del producto por medio
    //Del Id.
    public class ProductPresentationRepository : IProductPresentationRepository
    {
        private readonly AppDbContext _context;

        public ProductPresentationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<double?> GetLitersByProductPresentationIdAsync(int productPresentationId)
        {
            var productPresentation = await _context.ProductPresentations
                .Include(pp => pp.Presentation) // Incluye la relación con Presentation
                .FirstOrDefaultAsync(pp => pp.Id == productPresentationId);

            return productPresentation?.Presentation?.Liters;
        }
    }
}
