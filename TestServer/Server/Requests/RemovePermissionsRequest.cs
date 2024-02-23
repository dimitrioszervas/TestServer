namespace TestServer.Server.Requests
{
    public sealed class RemovePermissionsRequest : BaseRequest
    {
        public string granteeID { get; set; }
        public string nodeID { get; set; }
        public ulong flags { get; set; }
    }
}
