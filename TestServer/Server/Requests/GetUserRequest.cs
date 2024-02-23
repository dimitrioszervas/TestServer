namespace TestServer.Server.Requests
{
    public sealed class GetUserRequest : BaseRequest
    {
        public string userID { get; set; }
    }
}
