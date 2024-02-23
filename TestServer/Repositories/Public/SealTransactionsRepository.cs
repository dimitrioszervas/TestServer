using AutoMapper;
using TestServer.Contracts.Public;
using TestServer.Models;
using TestServer.Models.Public;
using TestServer.Repositories;

namespace TestServer.Repositories.Public
{
    public class SealTransactionsRepository : PublicGenericRepository<SealTransaction>, ISealTransactionsRepository
    {
        public SealTransactionsRepository(PublicDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
