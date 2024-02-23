namespace TestServer.Server.Requests
{
    public sealed class RenameRequest : BaseRequest
    {
        public string encNAM { get; set; }
        public string ID { get; set; }
    }
}
