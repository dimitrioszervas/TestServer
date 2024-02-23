using AutoMapper;
using TestServer.Contracts;
using TestServer.Exceptions;
using TestServer.Models;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NuGet.Protocol;

namespace TestServer.Repositories
{
    public class PublicGenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly PublicDbContext _context;
        private readonly IMapper _mapper;

        public PublicGenericRepository(PublicDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<T> AddAsync(T entity)
        {
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<TResult> AddAsync<TSource, TResult>(TSource source)
        {
            var entity = _mapper.Map<T>(source);
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();
            return _mapper.Map<TResult>(entity);
        }

        public IDbContextTransaction BeginTransaction()
        {
            return _context.Database.BeginTransaction();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetAsync(id);
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByUlongIdAsync(ulong id)
        {
            var entity = await GetByUlongIdAsync(id);
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> Exists(int id)
        {
            var entity = await GetAsync(id);
            return entity != null;
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        //public async Task<PagedResult<TResult>> GetAllAsync<TResult>(QueryParameters queryParameters)
        //{
        //    var totalSize = await _context.Set<T>().CountAsync();
        //    var items = await _context.Set<T>()
        //        .Skip(queryParameters.StartIndex)
        //        .Take(queryParameters.PageSize)
        //        .ProjectTo<TResult>(_mapper.ConfigurationProvider)
        //        .ToListAsync();
        //    return new PagedResult<TResult>
        //    {
        //        Items = items,
        //        PageNumber = queryParameters.PageNumber,
        //        RecordNumber = queryParameters.PageSize,
        //        TotalCount = totalSize
        //    };
        //}

        public async Task<T> GetAsync(int? id)
        {
            if (id is null)
            {
                return null;
            }

            return await _context.Set<T>().FindAsync(id);
        }

        public async Task<T> GetByUlongIdAsync(ulong? id)
        {
            if (id is null)
            {
                return null;
            }

            return await _context.Set<T>().FindAsync(id);
        }

        public async Task UpdateAsync(T entity)
        {
            _context.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateByIdAsync<TSource>(ulong id, TSource source)
        {
            var entity = GetByUlongIdAsync(id);
            if (entity == null)
            {
                throw new NotFoundException(typeof(TSource).ToJson(), id);
            }

            await _mapper.Map(source, entity);
            _context.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}
