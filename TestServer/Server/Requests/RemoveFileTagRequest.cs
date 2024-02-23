namespace TestServer.Server.Requests
{
    public sealed class RemoveFileTagRequest : BaseRequest
    {
        public string ID { get; set; }
        public string tag { get; set; }
    }
}
