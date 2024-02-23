using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Dtos.Private
{
    public class PermissionDto : BaseDto
    {
        public int Id { get; set; }
        public ulong Flags { get; set; }
        public ulong GranteeId { get; set; }
        public string? NodeAesWrapByDeriveGranteeDhPubGranterDhPriv { get; set; }
        public bool Revoked { get; set; }
        public ulong NodeId { get; set; }
    }
}
