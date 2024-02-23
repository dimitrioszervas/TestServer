namespace TestServer.Server.Requests
{
    public sealed class DisableInheritRequest : BaseRequest
    {
        public string nodeID { get; set; }
        public string parentID { get; set; }
        public string ownerID { get; set; }
        public string wrappedNodeKEY { get; set; }
    }
}
