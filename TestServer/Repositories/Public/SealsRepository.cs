using AutoMapper;
using TestServer.Contracts.Public;
using TestServer.Models;
using TestServer.Models.Public;
using TestServer.Repositories;

namespace TestServer.Repositories.Public
{
    public class SealsRepository : PublicGenericRepository<Seal>, ISealsRepository
    {
        public SealsRepository(PublicDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
