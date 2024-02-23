namespace TestServer.Server.Responses
{
    public class CreateGroupResponse : BaseResponse
    {
        public string ID { get; set; }
        public string encNAM { get; set; }     
        public string userID { get; set; }
        public string userEncName { get; set; }
        public ulong userPermissions { get; set; }
        public List<GroupObject> groups { get; set; } = new List<GroupObject>();
        public bool success { get; set; }
    }
}
