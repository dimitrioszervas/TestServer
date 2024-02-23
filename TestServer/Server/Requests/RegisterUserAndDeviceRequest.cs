namespace TestServer.Server.Requests
{
    public sealed class RegisterUserAndDeviceRequest : BaseRequest
    {      
        public string DeviceDsaPub { get; set; }
        public string DeviceDhPub { get; set; }
       
        public string UserDhPub { get; set; }
        public string UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv { get; set; }

        public string Code { get; set; }
    }
}
