namespace TestServer.Server.Requests
{
    public sealed class HasUserPermissionRequest : BaseRequest
    {
        public string UserID { get; set; }
        public string NodeID { get; set; }
    }
}
