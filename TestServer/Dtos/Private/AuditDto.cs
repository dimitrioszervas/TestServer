namespace TestServer.Dtos.Private
{
    public class AuditDto : BaseDto
    {
        public int Id { get; set; }
        public byte Type { get; set; }
        public ulong? NodeId { get; set; }
    }
}
