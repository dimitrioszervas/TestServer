namespace TestServer.Server.Responses
{
    public class GetUserResponse : BaseResponse
    {    
        public string ID { get; set; }
        public string NameEncByParentEncKey { get; set; }
        public string UserType { get; set; }
        public string LastModified { get; set; }
        public List<string> Classification { get; set; }
        public List<GroupDetailsObject> Groups { get; set; } = new List<GroupDetailsObject>();
        public int NoOfGroupsIn { get; set; }
        public string Status { get; set; }
    }
}
