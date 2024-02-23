namespace TestServer.Server.Responses
{
    public class AddReviewResponse : BaseResponse
    {
        public string ID { get; set; }
        public string encCom { get; set; }
        public bool success { get; set; }
    }
}
