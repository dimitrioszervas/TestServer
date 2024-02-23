namespace TestServer.Server.Requests
{
    public sealed class DeleteUserRequest : BaseRequest
    {
        public string UserID { get; set; }
    }
}
