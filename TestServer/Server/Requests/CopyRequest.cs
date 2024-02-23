namespace TestServer.Server.Requests
{
    public sealed class CopyRequest : BaseRequest
    {
        public string ID { get; set; }
        public string pID { get; set; }
        public string encKEY { get; set; }
        public List<string> newIDs { get; set; } = new List<string>();
    }
}
