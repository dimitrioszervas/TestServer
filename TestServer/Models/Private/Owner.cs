using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public class Owner : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public ulong NodeId { get; set; }
        public byte[] EncNodeKey { get; set; }
    }
}
