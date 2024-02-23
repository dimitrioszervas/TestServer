namespace TestServer.Server.Requests
{
    public sealed class RevokeAccessRequest : BaseRequest
    {
        public ulong flags { get; set; }
        public string granteeID { get; set; }  
        public string nodeID { get; set; }
    }
}
