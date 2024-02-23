using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public class Invitation : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string InviteeEmail { get; set; }
        public bool Revoked { get; set; }

    }
}
