using AutoMapper;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Models.Private;
using TestServer.Repositories;

namespace TestServer.Repositories.Private
{
    public class GroupMembersRepository : GenericRepository<GroupMember>, IGroupMembersRepository
    {
        public GroupMembersRepository(EndocloudDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
