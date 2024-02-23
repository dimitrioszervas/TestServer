using AutoMapper;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Models.Private;
using TestServer.Repositories;

namespace TestServer.Repositories.Private
{
    public class GroupsRepository : GenericRepository<Group>, IGroupsRepository
    {
        public GroupsRepository(EndocloudDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
