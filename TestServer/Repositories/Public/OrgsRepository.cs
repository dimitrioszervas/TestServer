using AutoMapper;
using TestServer.Contracts.Public;
using TestServer.Models;
using TestServer.Models.Public;
using TestServer.Repositories;

namespace TestServer.Repositories.Public
{
    public class OrgsRepository : PublicGenericRepository<TestServer.Models.Public.Org>, IOrgsRepository
    {
        public OrgsRepository(PublicDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
