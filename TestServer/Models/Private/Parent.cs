using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public class Parent : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string NameEncByParentEncKey { get; set; }
        public ulong? ParentNodeId { get; set; }
        public ulong NodeId { get; set; }
        public string? NodeKeyWrappedByParentNodeKey { get; set; }
        public bool CurrentParent { get; set; }

    }
}
