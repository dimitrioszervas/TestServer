namespace TestServer.Server.Requests
{
    public sealed class GetAttributeRequest : BaseRequest
    {
        public string ID { get; set; }
        public byte AttrType { get; set; }
    }
}
