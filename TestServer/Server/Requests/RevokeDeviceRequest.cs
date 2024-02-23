namespace TestServer.Server.Requests
{
    public sealed class RevokeDeviceRequest : BaseRequest
    {
        public string DeviceID { get; set; }
    }
}
