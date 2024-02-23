using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    [Flags]
    public enum PermissionType : ulong
    {
        None = 0,  // 00000000
        Read = 1,  // 00000001
        Create = 2,  // 00000010
        Update = 4,  // 00000100
        Delete = 8,  // 00001000 
        Review = 16, // 00010000
        Approve = 32, // 00100000
        Release = 64, // 01000000
        Share = 128 // 10000000
    }

    public class Permission : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public ulong Flags { get; set; }
        public ulong GranteeId { get; set; }
        public string? NodeAesWrapByDeriveGranteeDhPubGranterDhPriv { get; set; }
        public bool Revoked { get; set; }

        [ForeignKey(nameof(NodeId))]
        public ulong NodeId { get; set; }
        public Node Node { get; set; }
    }
}
