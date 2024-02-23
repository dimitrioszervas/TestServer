namespace TestServer.Server.Requests
{
    public sealed class CreateGroupRequest : BaseRequest
    {
        public string ID { get; set; }
        public string encNAM { get; set; }
        public string GroupDhPub { get; set; }
        public string EncGroupDhPriv { get; set; }
        public string NodeAESWrapByDeriveGranteeDhPubGranterDhPriv { get; set; }
    }
}
