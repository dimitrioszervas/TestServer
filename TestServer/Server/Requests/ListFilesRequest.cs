namespace TestServer.Server.Requests
{
    public sealed class ListFilesRequest : BaseRequest
    {
        public string ID { get; set; }
        public string snapTS { get; set; }
        public string fromTS { get; set; }
    }
}
