using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public class Group : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public bool Deleted { get; set; }
        public string wrapKEY { get; set; }
        public string encUnwrapKEY { get; set; }

        [ForeignKey(nameof(NodeId))]
        public ulong NodeId { get; set; }
        public Node Node { get; set; }
    }
}
