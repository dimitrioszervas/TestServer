using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public class Seal : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public byte Type { get; set; }
        public byte[]? PrivateBlockHash { get; set; }
        public byte[]? PublicBlockHash { get; set; }
    }
}
