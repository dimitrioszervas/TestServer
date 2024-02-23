namespace TestServer.Server.Requests
{
    public sealed class ListGroupsRequest : BaseRequest
    {
        public bool Deleted { get; set; } = false;
    }
}
