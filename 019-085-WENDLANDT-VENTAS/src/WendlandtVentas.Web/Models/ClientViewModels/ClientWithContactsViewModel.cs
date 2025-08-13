using System.Collections.Generic;

namespace WendlandtVentas.Web.Models.ClientViewModels
{
    public class ClientWithContactsViewModel
    {
        public ClientViewModel Client { get; set; }

        public List<ContactViewModel> Contacts { get; set; } = new List<ContactViewModel>();

    }
}
