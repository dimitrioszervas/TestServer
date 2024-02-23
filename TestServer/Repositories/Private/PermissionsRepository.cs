using AutoMapper;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Models.Private;
using TestServer.Repositories;

namespace TestServer.Repositories.Private
{
    public class PermissionsRepository : GenericRepository<Permission>, IPermissionsRepository
    {
        private readonly EndocloudDbContext _context;

        public PermissionsRepository(EndocloudDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
        }

        public async Task<Permission> GetByNodeIdAndGranteeId(ulong? nodeId, ulong granteeId)
        {
            var permission = (from p in _context.Permissions
                              where p.NodeId == nodeId && p.GranteeId == granteeId && p.Flags != (ulong)PermissionType.None
                              orderby p.Timestamp descending
                              select p).FirstOrDefault();

            return permission;
        }

        public async Task<List<Permission>> GetPermissionsByNodeId(ulong? nodeId)
        {
            var permissions = from p in _context.Permissions
                              where p.NodeId == nodeId && !p.Revoked && p.Flags != (ulong)PermissionType.None
                              orderby p.Timestamp ascending
                              select p;

            return permissions.ToList();
        }

        public async Task<List<Permission>> GetSharedFilesByUserId(ulong? userId)
        {
            var permissions = from p in _context.Permissions
                              where p.GranteeId == userId &&
                                    p.UserNodeId != userId &&
                                    p.Flags != (ulong)PermissionType.None &&
                                    !p.Revoked &&
                                    (p.Node.Type == (byte)NodeType.File || p.Node.Type == (byte)NodeType.Folder)
                              orderby p.Timestamp descending
                              select p;

            return permissions.ToList();
        }

        public async Task<List<Permission>> GetGroupsByGranteeId(ulong? granteeId)
        {
            var permissions = from p in _context.Permissions
                              where p.GranteeId == granteeId && !p.Revoked && p.Node.Type == (byte)NodeType.Group
                              select p;

            return permissions.ToList();
        }

        public async Task<Permission> GetSharedByNodeIdAndGranteeId(ulong? nodeId, ulong granteeId)
        {
            var permission = (from p in _context.Permissions
                              where p.NodeId == nodeId &&
                                    p.GranteeId == granteeId &&
                                    p.UserNodeId != granteeId &&
                                   !p.Revoked &&
                                    p.Flags != (ulong)PermissionType.None &&
                                   (p.Node.Type == (byte)NodeType.File || p.Node.Type == (byte)NodeType.Folder)
                              orderby p.Timestamp descending
                              select p).FirstOrDefault();

            return permission;
        }

        public async Task<Permission> GetByNodeIdAndUserNodeIdAndGranteedNodeId(ulong? nodeId, ulong userNodeId, ulong granteeNodeId)
        {
            var permission = (from p in _context.Permissions
                              where p.NodeId == nodeId &&
                                    p.UserNodeId == userNodeId &&
                                    p.GranteeId == granteeNodeId
                              orderby p.Timestamp descending
                              select p).FirstOrDefault();

            return permission;
        }
    }
}
