using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monobits.SharedKernel.Interfaces
{
    public interface IRepository
    {
        T GetById<T>(int id) where T : BaseEntity, IAggregateRoot;

        T GetSingleBySpec<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot;

        IEnumerable<T> ListAll<T>() where T : BaseEntity, IAggregateRoot;

        IEnumerable<T> List<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot;

        T Add<T>(T entity) where T : BaseEntity, IAggregateRoot;

        void Update<T>(T entity) where T : BaseEntity, IAggregateRoot;

        void Delete<T>(T entity) where T : BaseEntity, IAggregateRoot;

        int Count<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot;
       
    }
}