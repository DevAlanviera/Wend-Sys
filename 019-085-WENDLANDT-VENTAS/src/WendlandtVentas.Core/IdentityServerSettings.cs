using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core
{
  public  class IdentityServerSettings
    {  
        public string Server { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
    }
}
