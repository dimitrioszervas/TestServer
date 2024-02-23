namespace TestServer.Server.Requests
{
    public sealed class GetProfileImageRequest : BaseRequest
    {
        public string ID { get; set; }
        public string size { get; set; }
    }
}
