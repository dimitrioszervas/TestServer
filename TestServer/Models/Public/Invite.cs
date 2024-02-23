using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Public
{
    public class Invite : BaseTable
    {
        public int Id { get; set; }
        [ForeignKey(nameof(UserId))]
        public ulong UserId { get; set; }
        public PublicUser User { get; set; }
        public string InviteeEmail { get; set; }
        public bool Revoked { get; set; }
        public ulong AccessCode { get; set; }
    }
}
