namespace TestServer.Dtos.Public
{
    public class SealTransactionDto
    {
        public ulong Id { get; set; }
        public ulong BlockchainId { get; set; }
        public ulong UserId { get; set; }
        public byte[]? Hash { get; set; }
        public byte[]? Signature { get; set; }
        public long Block { get; set; }
        public bool Realtime { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
