namespace TestServer.Dtos.Private
{
    public abstract class BaseDto
    {
        public ulong UserNodeId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
