using AutoMapper;
using TestServer.Contracts.Public;
using TestServer.Models;
using TestServer.Models.Public;
using TestServer.Repositories;

namespace TestServer.Repositories.Public
{
    public class PublicUsersRepository : PublicGenericRepository<PublicUser>, IPublicUsersRepository
    {
        public PublicUsersRepository(PublicDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
