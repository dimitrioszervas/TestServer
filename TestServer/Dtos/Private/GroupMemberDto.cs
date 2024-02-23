using TestServer.Models.Private;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Dtos.Private
{
    public class GroupMemberDto : BaseDto
    {
        public int Id { get; set; }
        public ulong? UserId { get; set; }
        public int GroupId { get; set; }
    }
}
