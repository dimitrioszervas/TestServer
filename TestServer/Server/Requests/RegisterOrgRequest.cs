namespace TestServer.Server.Requests
{
    public sealed class RegisterOrgRequest : BaseRequest
    {     
        public string Code { get; set; }      
        public string OrgID { get; set; }
        public string ChainID { get; set; }
        public string UsersID { get; set; }
        public string UserID { get; set; }
        public string DeviceID { get; set; }
        public string GroupsID {  get; set; }

        public string OrgName { get; set; }
        public string ChainName { get; set; }
        public string UsersName { get; set; } = "USERS";
        public string UserName { get; set; }
        public string DeviceName { get; set; }
        public string GroupsName { get; set; } = "GROUPS";

        public string DeviceDsaPub { get; set; }
        public string DeviceDhPub { get; set; }
        public string UserDhPub { get; set; }
        public string UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv { get; set; }
        public string UsersAesWrapByDeriveGranteeDhPubGranterDhPriv { get; set; }
        public string GroupsAesWrapByDeriveGranteeDhPubGranterDhPriv { get; set; }
        public string OrgNodeAesWrapByDeriveUserDhPubDeviceDhPriv { get; set; }
        public string ChainNodeAesWrapByOrgNodeAes { get; set; }
        public string UsersNodeAesWrapByChainNodeAes { get; set; }
        public string UserNodeAesWrapByUsersNodeAes { get; set; }
        public string DeviceNodeAesWrapByUserNodeAes { get; set; }
        public string GroupsNodeAesWrapByChainNodeAes { get; set; }
    }
}
