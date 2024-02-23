namespace TestServer.Server.Requests
{
    public sealed class CreateUserAndDeviceRequest : BaseRequest
    {       
        public string UserID { get; set; }
        public string DeviceID { get; set; }

        public string UserName { get; set; }
        public string DeviceName { get; set; }
       
        public string UserNodeAesWrapByUsersNodeAes { get; set; }
        public string DeviceNodeAesWrapByUserNodeAes { get; set; }

        public string Code { get; set; }
    }
}
