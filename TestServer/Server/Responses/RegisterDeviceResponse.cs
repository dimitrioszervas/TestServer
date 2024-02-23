namespace TestServer.Server.Responses
{
    public sealed class RegisterDeviceResponse : BaseResponse
    {  
        public string UserID { get; set; }
        public string DeviceID { get; set; }
    }
}
