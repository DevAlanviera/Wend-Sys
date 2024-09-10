using Autofac;
using Monobits.SharedKernel;
using System.Reflection;
using WendlandtVentas.Infrastructure.Data;
using Module = Autofac.Module;

namespace WendlandtVentas.Web
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // TODO: Add Registry Classes to eliminate reference to Infrastructure
            // https://ardalis.com/avoid-referencing-infrastructure-in-visual-studio-solutions
            var webAssembly = Assembly.GetExecutingAssembly();
            var coreAssembly = Assembly.GetAssembly(typeof(BaseEntity));
            var infrastructureAssembly = Assembly.GetAssembly(typeof(EfRepository)); // TODO: Move to Infrastucture Registry
            builder.RegisterAssemblyTypes(webAssembly, coreAssembly, infrastructureAssembly).AsImplementedInterfaces();
        }
    }
}