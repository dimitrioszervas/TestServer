namespace TestServer.Server.Responses
{
    public class AddUserToGroupResponse : BaseResponse
    {
        public string ID { get; set; }
        public string encNAM { get; set; }
        public bool success { get; set; }
    }
}
