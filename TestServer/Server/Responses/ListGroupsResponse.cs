namespace TestServer.Server.Responses
{
    public sealed class UserDetailsObject
    {
        public string ID { get; set; }
        public string NameEncByParentEncKey { get; set; }
    }

    public sealed class GroupObject 
    {
        public string ID {  get; set; }
        public string NameEncByParentEncKey {  get; set; }
        public List<UserDetailsObject> Users { get; set; } = new List<UserDetailsObject>();
        public string Status { get; set; }
    }

    public sealed class ListGroupsResponse : BaseResponse
    {
        public List<GroupObject> Groups { get; set; } = new List<GroupObject> ();
    }
}
