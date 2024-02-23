namespace TestServer.Server.Requests
{
    public sealed class RegisterDeviceRequest : BaseRequest
    {
        public string Code { get; set; }
        public string DeviceDsaPub { get; set; }
        public string DeviceDhPub { get; set; }
    }
}
