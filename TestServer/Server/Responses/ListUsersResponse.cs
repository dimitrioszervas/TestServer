namespace TestServer.Server.Responses
{
    public sealed class GroupDetailsObject
    {
        public string ID { get; set; }
        public string NameEncByParentEncKey { get; set; }
    }

    public sealed class UserObject
    {
        public string ID { get; set; }
        public string NameEncByParentEncKey { get; set; }
        public string UserType { get; set; }
        public string LastModified { get; set; }
        public List<GroupDetailsObject> Groups { get; set; } = new List<GroupDetailsObject>();
        public int NoOfGroupsIn { get; set; }
        public string Status { get; set; }
    }

    public sealed class ListUsersResponse : BaseResponse
    {
        public List<UserObject> Users { get; set; } = new List<UserObject>();
    }
}
