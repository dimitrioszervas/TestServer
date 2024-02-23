using TestServer.Models.Private;
using TestServer.Contracts;

namespace TestServer.Contracts.Private
{
    public interface IAttributesRepository : IGenericRepository<Models.Private.Attribute>
    {
        Task<Models.Private.Attribute> GetFirstByNodeIdAndType(ulong nodeId, byte type);
        Task<Models.Private.Attribute> GetByAttributeValue(string value);
        Task<string> GetFirstValueByNodeIdAndType(ulong nodeId, byte type);
        Task<ulong> GetNodeIdByCode(string code);
        Task<List<Models.Private.Attribute>> GetListByNodeIdAndType(ulong nodeId, byte type);
        Task<Models.Private.Attribute> GetByNodeIdAndAttributeValue(ulong nodeId, string value);
        Task<Models.Private.Attribute> GetFirstByNodeIdAndTypeAndUserNodeId(ulong nodeId, byte type, ulong userNodeId);
    }
}
