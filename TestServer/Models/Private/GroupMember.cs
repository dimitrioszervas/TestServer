using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public class GroupMember : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey(nameof(UserId))]
        public ulong? UserId { get; set; }
        public Node? user { get; set; }

        [ForeignKey(nameof(GroupId))]
        public int GroupId { get; set; }
        public Group groupRecord { get; set; }
    }
}
