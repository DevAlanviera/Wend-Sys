using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monobits.SharedKernel.Interfaces
{
    public interface IAsyncRepository
    {
        Task<T> GetByIdAsync<T>(int id) where T : BaseEntity, IAggregateRoot;

        Task<T> GetByIdAsync<T>(string id) where T : BaseEntity, IAggregateRoot;

        Task<T> GetAsync<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot;

        Task<IReadOnlyList<T>> ListAllAsync<T>() where T : BaseEntity, IAggregateRoot;
        Task<IReadOnlyList<T>> ListAllExistingAsync<T>() where T : BaseEntity, IAggregateRoot;

        Task<IReadOnlyList<T>> ListAsync<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot;
        Task<IReadOnlyList<T>> ListExistingAsync<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot;

        Task<T> AddAsync<T>(T entity) where T : BaseEntity, IAggregateRoot;

        Task<List<T>> AddRangeAsync<T>(List<T> entities) where T : BaseEntity, IAggregateRoot;

        Task UpdateAsync<T>(T entity) where T : BaseEntity, IAggregateRoot;

        Task DeleteAsync<T>(T entity) where T : BaseEntity, IAggregateRoot;

        Task DeleteRangeAsync<T>(List<T> entities) where T : BaseEntity, IAggregateRoot;

        Task<int> CountAsync<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot;
        IQueryable<T> GetQueryable<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot;

        IQueryable<T> GetQueryableExisting<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot;

        IQueryable<T> GetQueryableExisting<T>() where T : BaseEntity, IAggregateRoot;

        IQueryable<T> GetQueryable<T>() where T : BaseEntity, IAggregateRoot;
    }
}