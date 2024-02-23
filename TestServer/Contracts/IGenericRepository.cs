using Microsoft.AspNetCore.OData.Results;
using Microsoft.EntityFrameworkCore.Storage;

namespace TestServer.Contracts
{
    public interface IGenericRepository<T> where T : class
    {
        IDbContextTransaction BeginTransaction();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<T> GetAsync(int? id);
        Task<T> GetByUlongIdAsync(ulong? id);
        Task<List<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task<TResult> AddAsync<TSource, TResult>(TSource source);
        Task DeleteAsync(int id);
        Task DeleteByUlongIdAsync(ulong id);
        Task UpdateAsync(T entity);
        Task UpdateByIdAsync<TSource>(ulong id, TSource source);
        Task<bool> Exists(int id);
    }
}
