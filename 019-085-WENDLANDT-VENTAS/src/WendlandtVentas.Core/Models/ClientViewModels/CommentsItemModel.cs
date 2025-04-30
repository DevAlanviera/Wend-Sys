using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core.Models.ClientViewModels
{
    public class CommentsItemModel
    {
        public int Id { get; set; }

        [JsonProperty("comments")]
        public string Comments { get; set; }
        
    }
}
