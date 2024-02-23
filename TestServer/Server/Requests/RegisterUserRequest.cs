namespace TestServer.Server.Requests
{
    public sealed class RegisterUserRequest : BaseRequest
    {
        public string oneTimeCode { get; set; }
        public string passwordHASH { get; set; }
        public string verifyKEY { get; set; }
    }
}
