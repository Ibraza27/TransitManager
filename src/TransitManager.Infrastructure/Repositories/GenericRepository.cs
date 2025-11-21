using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Repositories
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        Task<T> UpdateAsync(T entity);
        Task<bool> RemoveAsync(T entity);
        Task<bool> RemoveRangeAsync(IEnumerable<T> entities);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        IQueryable<T> Query();
        IQueryable<T> QueryNoTracking();
    }

    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;

        public GenericRepository(IDbContextFactory<TransitContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().Where(e => e.Actif).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>()
                .Where(e => e.Actif)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().Where(predicate).Where(e => e.Actif).ToListAsync();
        }

        public virtual async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().Where(predicate).Where(e => e.Actif).SingleOrDefaultAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await context.Set<T>().AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await context.Set<T>().AddRangeAsync(entities);
            await context.SaveChangesAsync();
            return entities;
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Set<T>().Update(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<bool> RemoveAsync(T entity)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            entity.Actif = false;
            context.Set<T>().Update(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public virtual async Task<bool> RemoveRangeAsync(IEnumerable<T> entities)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            foreach (var entity in entities)
            {
                entity.Actif = false;
            }
            context.Set<T>().UpdateRange(entities);
            await context.SaveChangesAsync();
            return true;
        }

        public virtual async Task<int> CountAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().CountAsync(e => e.Actif);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().Where(predicate).CountAsync(e => e.Actif);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().Where(predicate).AnyAsync(e => e.Actif);
        }

        public virtual IQueryable<T> Query()
        {
            throw new NotSupportedException("Query method is not supported with IDbContextFactory. Use async methods instead.");
        }

        public virtual IQueryable<T> QueryNoTracking()
        {
            throw new NotSupportedException("QueryNoTracking method is not supported with IDbContextFactory. Use async methods instead.");
        }
    }
}
