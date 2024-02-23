namespace TestServer.Server.Requests
{
    public sealed class AddFileTagRequest : BaseRequest
    {
        public string ID { get; set; }
        public string tag { get; set; }
    }
}
