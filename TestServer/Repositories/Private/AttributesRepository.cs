using AutoMapper;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Models.Private;
using Newtonsoft.Json.Linq;
using TestServer.Repositories;

namespace TestServer.Repositories.Private
{
    public class AttributesRepository : GenericRepository<Models.Private.Attribute>, IAttributesRepository
    {
        private readonly EndocloudDbContext _context;

        public AttributesRepository(EndocloudDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
        }

        public async Task<Models.Private.Attribute> GetFirstByNodeIdAndType(ulong nodeId, byte type)
        {
            var attribute = (from a in _context.Attributes
                             where a.NodeId == nodeId && a.AttributeType == type
                             orderby a.Timestamp descending
                             select a).Take(1);

            return attribute.FirstOrDefault();
        }

        public async Task<Models.Private.Attribute> GetByAttributeValue(string value)
        {
            var attribute = from a in _context.Attributes
                            where a.AttributeValue == value
                            select a;

            return attribute.FirstOrDefault();
        }

        public async Task<string> GetFirstValueByNodeIdAndType(ulong nodeId, byte type)
        {
            var attribute = (from a in _context.Attributes
                             where a.NodeId == nodeId && a.AttributeType == type
                             orderby a.Timestamp descending
                             select a).Take(1);

            return attribute.FirstOrDefault().AttributeValue;
        }

        public async Task<ulong> GetNodeIdByCode(string code)
        {
            var attribute = from a in _context.Attributes
                            where a.AttributeValue == code
                            select a;

            return attribute.FirstOrDefault().NodeId;
        }

        public async Task<List<Models.Private.Attribute>> GetListByNodeIdAndType(ulong nodeId, byte type)
        {
            var attributes = from a in _context.Attributes
                             where a.NodeId == nodeId && a.AttributeType == type
                             select a;

            return attributes.ToList();
        }

        public async Task<Models.Private.Attribute> GetByNodeIdAndAttributeValue(ulong nodeId, string value)
        {
            var attribute = (from a in _context.Attributes
                             where a.NodeId == nodeId && a.AttributeValue == value
                             orderby a.Timestamp descending
                             select a).Take(1);

            return attribute.FirstOrDefault();
        }

        public async Task<Models.Private.Attribute> GetFirstByNodeIdAndTypeAndUserNodeId(ulong nodeId, byte type, ulong userNodeId)
        {
            var attribute = (from a in _context.Attributes
                             where a.NodeId == nodeId && a.AttributeType == type && a.UserNodeId == userNodeId
                             orderby a.Timestamp descending
                             select a).Take(1);

            return attribute.FirstOrDefault();
        }
    }
}
