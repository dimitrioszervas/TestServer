namespace TestServer.Server.Responses
{
    public class LoginResponse : BaseResponse
    {     
        public string UserID { get; set; }
        public string UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv { set; get; }
        public string UserDhPub { set; get; }
        public string GranterDhPub { set; get; }
        public string OrgNodeAesWrapByDeriveUserDhPubDeviceDhPriv { set; get; }
        public string OrgNodeAesWrapByDeriveUserDhPrivDeviceDhPub { set; get; }
        public string ChainNodeAesWrapByOrgNodeAes {  set; get; }
        public string UserNodeAesWrapByUsersNodeAes { set; get; }
        public string UserNameEncByParentEncKey {  set; get; }
        public string UsersNodeAesWrapByChainNodeAes { set; get; }
        public string GroupsNodeAesWrapByChainNodeAes { set; get; }
        public string UsersAesWrapByDeriveGranteeDhPubGranterDhPriv { set; get; }
        public string GroupsAesWrapByDeriveGranteeDhPubGranterDhPriv { set; get; }
        public string BlockchainID { get; set; }
        public string LastLoginTime { get; set; } = null;
    }

}
