using Microsoft.EntityFrameworkCore;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WendlandtVentas.Infrastructure.Data
{
    public class EfRepository : IAsyncRepository
    {
        private readonly AppDbContext _dbContext;

        public EfRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public T GetById<T>(int id) where T : BaseEntity, IAggregateRoot
        {
            return _dbContext.Set<T>().SingleOrDefault(e => e.Id == id);
        }

        public T GetSingleBySpec<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot
        {
            return List(spec).FirstOrDefault();
        }

        public IEnumerable<T> ListAll<T>() where T : BaseEntity, IAggregateRoot
        {
            return _dbContext.Set<T>().AsEnumerable();
        }

        public IEnumerable<T> List<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot
        {
            return ApplySpecification(spec).AsEnumerable();
        }

        public T Add<T>(T entity) where T : BaseEntity, IAggregateRoot
        {
            _dbContext.Set<T>().Add(entity);
            _dbContext.SaveChanges();

            return entity;
        }

        public void Delete<T>(T entity) where T : BaseEntity, IAggregateRoot
        {
            _dbContext.Set<T>().Remove(entity);
            _dbContext.SaveChanges();
        }

        public int Count<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot
        {
            return ApplySpecification(spec).Count();
        }

        public void Update<T>(T entity) where T : BaseEntity, IAggregateRoot
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            _dbContext.SaveChanges();
        }

        public async Task<T> GetByIdAsync<T>(int id) where T : BaseEntity, IAggregateRoot
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }

        public async Task<IReadOnlyList<T>> ListAllAsync<T>() where T : BaseEntity, IAggregateRoot
        {
            return await _dbContext.Set<T>().ToListAsync();
        }

        public async Task<IReadOnlyList<T>> ListAsync<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot
        {
            return await ApplySpecification(spec).ToListAsync();
        }

        public async Task<T> AddAsync<T>(T entity) where T : BaseEntity, IAggregateRoot
        {
            _dbContext.Set<T>().Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }

        public async Task UpdateAsync<T>(T entity) where T : BaseEntity, IAggregateRoot
        {
            var existing = await _dbContext.Set<T>().FindAsync(entity.Id);

            if (existing == null)
                throw new Exception("Entidad no encontrada para actualizar");

            _dbContext.Entry(existing).CurrentValues.SetValues(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync<T>(T entity) where T : BaseEntity, IAggregateRoot
        {
            _dbContext.Set<T>().Remove(entity);
            await _dbContext.SaveChangesAsync();

        }

        public async Task<int> CountAsync<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot
        {
            return await ApplySpecification(spec).CountAsync();
        }

        private IQueryable<T> ApplySpecification<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot
        {
            return SpecificationEvaluator<T>.GetQuery(_dbContext.Set<T>().AsQueryable(), spec);
        }

        public async Task<T> GetByIdAsync<T>(string id) where T : BaseEntity, IAggregateRoot
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }

        public async Task<T> GetAsync<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }

        public async Task<List<T>> AddRangeAsync<T>(List<T> entities) where T : BaseEntity, IAggregateRoot
        {
            await _dbContext.Set<T>().AddRangeAsync(entities);
            await _dbContext.SaveChangesAsync();

            return entities;
        }

        public async Task DeleteRangeAsync<T>(List<T> entities) where T : BaseEntity, IAggregateRoot
        {
            _dbContext.Set<T>().RemoveRange(entities);
            await _dbContext.SaveChangesAsync();
        }
        public IQueryable<T> GetQueryable<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot
        {
            return ApplySpecification(spec);
        }
        public IQueryable<T> GetQueryableExisting<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot
        {
            return ApplySpecification(spec).Where(c => !c.IsDeleted);
        }
    
        public async Task<IReadOnlyList<T>> ListAllExistingAsync<T>() where T : BaseEntity, IAggregateRoot
        {
            return await _dbContext.Set<T>().Where(c => !c.IsDeleted).ToListAsync();
        }

        public async Task<IReadOnlyList<T>> ListExistingAsync<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot
        {
            return await ApplySpecification(spec).Where(c => !c.IsDeleted).ToListAsync();
        }

        public IQueryable<T> GetQueryable<T>() where T : BaseEntity, IAggregateRoot
        {
            return _dbContext.Set<T>();
        }

        public IQueryable<T> GetQueryableExisting<T>() where T : BaseEntity, IAggregateRoot
        {
            return _dbContext.Set<T>().Where(c => !c.IsDeleted);
        }
    }
}
