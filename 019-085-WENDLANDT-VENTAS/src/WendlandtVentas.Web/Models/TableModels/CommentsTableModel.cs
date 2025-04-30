using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WendlandtVentas.Web.Models.TableModels
{
    public class CommentsTableModel
    {
        public int Id { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsDeleted { get; set; }  // Nueva propiedad
    
    }
}
