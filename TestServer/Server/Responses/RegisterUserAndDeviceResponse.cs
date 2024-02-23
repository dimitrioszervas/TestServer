namespace TestServer.Server.Responses
{
    public sealed class RegisterUserAndDeviceResponse : BaseResponse
    {
        public string OrgID { get; set; }     
        public string UserID { get; set; }
        public string DeviceID { get; set; }
    }
}
