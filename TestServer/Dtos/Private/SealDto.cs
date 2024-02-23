namespace TestServer.Dtos.Private
{
    public class SealDto : BaseDto
    {
        public int Id { get; set; }
        public byte Type { get; set; }
        public byte[]? PrivateBlockHash { get; set; }
        public byte[]? PublicBlockHash { get; set; }
    }
}
