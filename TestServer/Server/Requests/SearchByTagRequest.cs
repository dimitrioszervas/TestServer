namespace TestServer.Server.Requests
{
    public sealed class SearchByTagRequest : BaseRequest
    {
        public string tag { get; set; }
    }
}
