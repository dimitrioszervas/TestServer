using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public class Version : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }
        public long Size { get; set; }
        [ForeignKey(nameof(NodeId))]
        public ulong NodeId { get; set; }
        public Node Node { get; set; }
    }
}
