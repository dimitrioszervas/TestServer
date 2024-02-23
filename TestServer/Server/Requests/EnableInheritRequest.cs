namespace TestServer.Server.Requests
{
    public sealed class EnableInheritRequest : BaseRequest
    {
        public string nodeID { get; set; }
        public string parentID { get; set; }      
        public string wrappedNodeKEY { get; set; }
    }
}
