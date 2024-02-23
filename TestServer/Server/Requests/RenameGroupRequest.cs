namespace TestServer.Server.Requests
{
    public sealed class RenameGroupRequest : BaseRequest
    {
        public string ID { get; set; }
        public string encNAM { get; set; }
    }
}
