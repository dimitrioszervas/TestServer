namespace TestServer.Dtos.Public
{
    public class SealDto
    {
        public int Id { get; set; }
        public byte[] PrivateBlockHash { get; set; }
        public byte[] PublicBlockHash { get; set; }
        public ulong? SealTransactionId { get; set; }
    }
}
