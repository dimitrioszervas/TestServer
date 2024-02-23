namespace TestServer.Server.Responses
{
    public sealed class RemoveUserFromGroupResponse : BaseResponse
    {
        public string ID { get; set; }
        public string encNAM { get; set; }
        public bool success { get; set; }
    }
}
