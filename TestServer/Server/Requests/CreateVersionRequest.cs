namespace TestServer.Server.Requests
{
    public sealed class CreateVersionRequest : BaseRequest
    {
        public string ID { get; set; }
        public string verID { get; set; }
        public long size { get; set; }
    }
}
