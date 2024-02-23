namespace TestServer.Server.Requests
{
    public sealed class CreateFileRequest : BaseRequest
    {
        public string ID { get; set; }
        public string pID { get; set; }
        public string encNAM { get; set; }
        public string encKEY { get; set; }
        public string verID { get; set; }       
        public long size { get; set; }     
       
    }
}
