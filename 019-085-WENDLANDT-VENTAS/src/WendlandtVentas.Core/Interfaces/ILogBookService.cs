using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WendlandtVentas.Core.Models;

namespace WendlandtVentas.Core.Interfaces
{
    public interface ILogBookService
    {
        Task<List<LogBookModel>> CreateLogBook(ChangeTracker changeTracker);
        Task<bool> SendPost(List<LogBookModel> Uri);
        Task<LogBookModel> GetLog(int Id, string clientId); 
        Task<LogBookResModel> GetData(LogBookFilterModel filters);
    }
}
