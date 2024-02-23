using System.Drawing;

namespace TestServer.Server.Requests
{
    public sealed class ActivateDeviceRequest : BaseRequest
    {
        public string DeviceID { get; set; }
        public string UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv { get; set; }
        public string? OrgNodeAesWrapByDeriveUserDhPrivDeviceDhPub { get; set; } = null;
    }
}
