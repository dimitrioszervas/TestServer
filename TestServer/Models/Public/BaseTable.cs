namespace TestServer.Models.Public
{
    public abstract class BaseTable
    {
        public ulong UserId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
