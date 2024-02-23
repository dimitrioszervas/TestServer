namespace TestServer.Server.Requests
{
    public sealed class MoveRequest : BaseRequest
    {
        public string encKEY { get; set; }
        public string ID { get; set; }
        public string pID { get; set; }
        public string encNAM { get; set; }   
    }
}
