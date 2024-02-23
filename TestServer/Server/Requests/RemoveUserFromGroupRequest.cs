namespace TestServer.Server.Requests
{
    public sealed class RemoveUserFromGroupRequest : BaseRequest
    {
        public string ID { get; set; }
        public string uID { get; set; }
    }
}
