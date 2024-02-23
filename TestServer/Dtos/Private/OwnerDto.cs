namespace TestServer.Dtos.Private
{
    public class OwnerDto : BaseDto
    {
        public ulong NodeId { get; set; }
        public byte[] EncNodeKey { get; set; }
    }
}
