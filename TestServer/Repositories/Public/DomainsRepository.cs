using AutoMapper;
using TestServer.Contracts.Public;
using TestServer.Models;
using TestServer.Models.Public;
using TestServer.Repositories;

namespace TestServer.Repositories.Public
{
    public class DomainsRepository : PublicGenericRepository<Domain>, IDomainsRepository
    {
        public DomainsRepository(PublicDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
