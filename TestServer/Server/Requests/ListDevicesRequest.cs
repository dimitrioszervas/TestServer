namespace TestServer.Server.Requests
{
    public sealed class ListDevicesRequest : BaseRequest
    {
        public string UserID { get; set; }
    }
}
