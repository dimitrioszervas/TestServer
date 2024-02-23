namespace TestServer.Server.Requests
{
    public sealed class ActivateUserRequest : BaseRequest
    {
        public string UserID { get; set; }
        public string UsersAesWrapByDeriveGranteeDhPubGranterDhPriv {  get; set; }
        public string GroupsAesWrapByDeriveGranteeDhPubGranterDhPriv { get; set; }
    }
}
