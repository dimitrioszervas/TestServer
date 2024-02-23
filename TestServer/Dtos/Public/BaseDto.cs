namespace TestServer.Dtos.Public
{
    public abstract class BaseDto
    {
        public ulong UserId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
