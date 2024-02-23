namespace TestServer.Server.Responses
{
    public sealed class DeviceObject
    {
        public string DeviceID { get; set; }
        public string DeviceName { get; set; }
        public string DeviceDhPub {  get; set; }
        public string Status { get; set; }
        public string LastModified { get; set; }
    }

    public sealed class ListDevicesResponse : BaseResponse
    {
        public List<DeviceObject> Devices { get; set; } = new List<DeviceObject>();
        public string UserAESKey {  get; set; }
    }
}
