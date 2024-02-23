using TestServer.Models.Private;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Dtos.Private
{
    public class InvitationDto : BaseDto
    {
        public int Id { get; set; }
        public string InviteeEmail { get; set; }
        public bool Revoked { get; set; }
    }
}
