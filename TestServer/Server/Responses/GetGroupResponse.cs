namespace TestServer.Server.Responses
{

    public class GetGroupResponse : BaseResponse
    {
        public string ID { get; set; }
        public string NameEncByParentEncKey { get; set; }
        public List<UserDetailsObject> Users { get; set; } = new List<UserDetailsObject>();
        public string Status { get; set; }
        public string Type { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
