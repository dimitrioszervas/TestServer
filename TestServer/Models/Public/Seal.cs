using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Public
{
    public class Seal
    {
        public int Id { get; set; }
        public byte[] PrivateBlockHash { get; set; }
        public byte[] PublicBlockHash { get; set; }
        [ForeignKey(nameof(SealTransactionId))]
        public ulong? SealTransactionId { get; set; }
        public SealTransaction SealTransaction { get; set; }
    }
}
