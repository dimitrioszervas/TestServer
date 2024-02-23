using AutoMapper;
using TestServer.Contracts.Public;
using TestServer.Models;
using TestServer.Models.Public;
using TestServer.Repositories;

namespace TestServer.Repositories.Public
{
    public class BlockchainsRepository : PublicGenericRepository<Blockchain>, IBlockchainsRepository
    {
        public BlockchainsRepository(PublicDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
        }

        public PublicDbContext _context { get; }

        public async Task<Blockchain> GetByOrgNodeIdAsync(ulong? orgNodeId)
        {
            var blockchain = from b in _context.Blockchains
                             where b.OrgId == orgNodeId
                             select b;

            return blockchain.FirstOrDefault();
        }
    }
}
