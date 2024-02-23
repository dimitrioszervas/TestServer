using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace TestServer.Dtos.Private
{
    [Keyless]
    public class CreateOrgDto
    {
        public ulong RootId { get; set; }
        public ulong OrgId { get; set; }
        public string OrgName { get; set; }
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public byte[]? UserCertificate { get; set; }
        public byte[]? UserLegalKey { get; set; }
        public byte[]? EncNodeKey { get; set; }
        public byte[]? Hash { get; set; }
        public byte[]? Signature { get; set; }
        public bool Realtime { get; set; }
        public ulong TransactionId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public ulong BlockchainId { get; set; }
    }
}
