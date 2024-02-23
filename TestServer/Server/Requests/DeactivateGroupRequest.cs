namespace TestServer.Server.Requests
{
    public sealed class DeactivateGroupRequest : BaseRequest
    {
        public string GroupID { get; set; }
    }
}
