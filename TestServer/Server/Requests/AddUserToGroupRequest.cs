namespace TestServer.Server.Requests
{
    public sealed class AddUserToGroupRequest : BaseRequest
    {
        public string ID { get; set; }
        public string uID { get; set; }
        public string encUnwrapKEY { get; set; }
    }
}
