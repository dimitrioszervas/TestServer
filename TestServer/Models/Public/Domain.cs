using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Public
{
    public class Domain : BaseTable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [ForeignKey(nameof(OrgId))]
        public ulong OrgId { get; set; }
        public Org Org;
    }
}
