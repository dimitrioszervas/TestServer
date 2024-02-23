namespace TestServer.Server.Requests
{
    public sealed class SendNewUserAndDeviceCodeRequest : BaseRequest
    {        
        public string DeviceID { get; set; }
        public string NewCode { get; set; }
    }
}
