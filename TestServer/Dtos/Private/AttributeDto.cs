namespace TestServer.Dtos.Private
{
    public class AttributeDto : BaseDto
    {
        public ulong Id { get; set; }
        public byte AttributeType { get; set; }
        public string AttributeValue { get; set; }
        public ulong NodeId { get; set; }
    }
}
