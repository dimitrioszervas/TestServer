namespace TestServer.Server.Requests
{
    public sealed class ChangeUserPasswordRequest : BaseRequest
    {
        public string oldPassHASH { get; set; }
        public string newPassHASH { get; set; }
        public string encUnwrapCODE { get; set; }
    }
}
