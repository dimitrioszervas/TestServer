using AutoMapper;
using TestServer.Contracts;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Models.Private;
using TestServer.Repositories;

namespace TestServer.Repositories.Private
{
    public class NodesRepository : GenericRepository<Node>, INodesRepository
    {

        public NodesRepository(EndocloudDbContext context, IMapper mapper) : base(context, mapper)
        {

        }

    }
}
