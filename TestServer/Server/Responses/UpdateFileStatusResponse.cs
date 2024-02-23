namespace TestServer.Server.Responses
{
    public class UpdateFileStatusResponse : BaseResponse
    {
        public string ID { get; set; }
        public string encNAM { get; set; }
        public bool controlledFlag { get; set; }
    }
}
