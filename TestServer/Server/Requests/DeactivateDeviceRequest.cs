namespace TestServer.Server.Requests
{
    public sealed class DeactivateDeviceRequest : BaseRequest
    {
        public string DeviceID { get; set; }
    }
}
