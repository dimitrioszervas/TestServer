namespace TestServer.Server.Requests
{
    public sealed class ApprovalRequest : BaseRequest
    {
        public string ID { get; set; }
        public string aID { get; set; }
        public string aTYP { get; set; }
        public string encCom { get; set; }
        public string vID { get; set; }
    }
}
