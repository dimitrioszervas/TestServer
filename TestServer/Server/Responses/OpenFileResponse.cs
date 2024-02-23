namespace TestServer.Server.Responses
{
    public sealed class OpenFileResponse : BaseResponse
    {
        public ShardsPacket ShardsPacket { get; set; } = new ShardsPacket();
    }
}
