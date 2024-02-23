namespace TestServer.Server.Requests
{
    public sealed class AddPermissionsRequest : BaseRequest
    {
        public string granteeID { get; set; }
        public string nodeID { get; set; }   
        public ulong flags { get; set; }
    }
}
