using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public enum AuditType : byte
    {
        Create,
        Register,
        Activate,
        Deactivate,
        Revoke,
        Login,
        Read,
        Rename,
        Delete,
        Move,
        Copy,
        Update,

        GrantedRead,
        GrantedCreate,
        GrantedUpdate,
        GrantedDelete,
        GrantedReview,
        GrantedApprove,
        GrantedRelease,
        GrantedShare,

        RevokedRead,
        RevokedCreate,
        RevokedUpdate,
        RevokedDelete,
        RevokedApprove,
        RevokedReview,
        RevokedRelease,
        RevokedShare,

        AddFileTag,
        RemoveFileTag,
        AddUserToGroup,
        RemoveUserFromGroup,
        AddReview,
        Approval,
        Release
    }

    public class Audit : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public byte Type { get; set; }
        public ulong NodeId { get; set; }
    }
}
