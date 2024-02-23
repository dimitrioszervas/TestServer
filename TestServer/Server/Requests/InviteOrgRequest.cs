namespace TestServer.Server.Requests
{
    public sealed class InviteOrgRequest : BaseRequest
    {
        public string orgID { set; get; }
        public string email { set; get; }
    }
}
