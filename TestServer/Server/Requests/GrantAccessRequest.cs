namespace TestServer.Server.Requests
{
    public sealed class GrantAccessRequest : BaseRequest
    { 
        public string NodeID { get; set; }      
        public string GranteeNodeID { get; set; }       
        public string NodeAesWrapByGranteeNodeAes { get; set; }
        public string NameEncByGranteeNodeAes { get; set; }
        public ulong Flags { get; set; }
    }
}
