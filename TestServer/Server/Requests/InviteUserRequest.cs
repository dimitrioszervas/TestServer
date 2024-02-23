namespace TestServer.Server.Requests
{
    public sealed class InviteUserRequest : BaseRequest
    {
        public string email { get; set; }
        public string ID { get; set; }
    }
}
