using AutoMapper;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Models.Private;
using TestServer.Repositories;

namespace TestServer.Repositories.Private
{
    public class ApprovalsRepository : GenericRepository<Approval>, IApprovalsRepository
    {
        public ApprovalsRepository(EndocloudDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
