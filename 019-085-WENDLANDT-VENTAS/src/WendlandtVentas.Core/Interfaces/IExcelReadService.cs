using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Models.OrderViewModels;

namespace WendlandtVentas.Core.Interfaces
{
   public interface IExcelReadService
    {
        Task<List<Client>> ExtractData(XLWorkbook spreadSheetDocument);
        Task<string> FillData(string path, OrderDetailsViewModel model);

       Task<byte[]> FillDataAndReturnPdfAsync(string path, Order order);
    }
}
