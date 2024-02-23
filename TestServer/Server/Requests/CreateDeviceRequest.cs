namespace TestServer.Server.Requests
{
    public sealed class CreateDeviceRequest : BaseRequest
    {
        public string DeviceID { get; set; }
        public string UserID { get; set; }
        public string DeviceName { get; set; }
        public string DeviceNodeAesWrapByUserNodeAes { get; set; }
        public string Code { get; set; }
    }
}
