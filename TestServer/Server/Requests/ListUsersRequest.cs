namespace TestServer.Server.Requests
{
    public sealed class ListUsersRequest : BaseRequest
    {
        public bool Deleted { get; set; } = false;
    }
}
