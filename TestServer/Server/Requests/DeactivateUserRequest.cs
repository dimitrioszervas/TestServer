namespace TestServer.Server.Requests
{
    public sealed class DeactivateUserRequest : BaseRequest
    {
        public string UserID { get; set; }
    }
}
