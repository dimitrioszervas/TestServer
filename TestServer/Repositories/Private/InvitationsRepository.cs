using AutoMapper;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Models.Private;
using TestServer.Repositories;

namespace TestServer.Repositories.Private
{
    public class InvitationsRepository : GenericRepository<Invitation>, IInvitationsRepository
    {
        public InvitationsRepository(EndocloudDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
