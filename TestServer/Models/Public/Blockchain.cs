using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Public
{
    public class Blockchain
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }
        [ForeignKey(nameof(OrgId))]
        public ulong OrgId { get; set; }
        public Org Org;
        public string Name { get; set; }
    }
}
