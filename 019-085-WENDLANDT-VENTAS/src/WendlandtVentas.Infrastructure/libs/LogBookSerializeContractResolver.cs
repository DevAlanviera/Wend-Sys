using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace WendlandtVentas.Infrastructure.libs
{
    public class LogBookSerializeContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyType.Name.Contains("ICollection") || property.PropertyName.Contains("Id"))
                property.ShouldSerialize = instance => false;

            return property;
        }
    }
}
