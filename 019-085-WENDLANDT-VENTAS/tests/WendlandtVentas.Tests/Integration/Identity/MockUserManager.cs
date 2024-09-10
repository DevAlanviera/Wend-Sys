using Microsoft.AspNetCore.Identity;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace WendlandtVentas.Tests.Integration.Identity
{
    public static class MockUserManager
    {
        public static Mock<UserManager<TUser>> UserManagerMocked<TUser>(List<TUser> ls) where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);

            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());
            mgr.Setup(x => x.DeleteAsync(It.IsAny<TUser>())).ReturnsAsync(IdentityResult.Success);
            mgr.SetupSequence(x => x.FindByEmailAsync(It.IsAny<string>()))
                 .Returns(Task.FromResult(ls.Find(d => d.Equals("pruebafail"))))
                 .Returns(Task.FromResult(ls.FirstOrDefault()));
            //Devuelve el primero que encuentra en la lista

            mgr.Setup(x => x.CreateAsync(It.IsAny<TUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success).Callback<TUser, string>((x, y) => ls.Add(x)); //Simula agregar un usuario, devuelve un success y lo agrega a la lista
            mgr.Setup(x => x.UpdateAsync(It.IsAny<TUser>())).ReturnsAsync(IdentityResult.Success);

            return mgr;
        }
    }
}