namespace TestServer.Server.Responses
{
    public class GrantAccessResponse : BaseResponse
    {
        public string encUnwrapCODE { get; set; }
        public string encUnwrapKEY { get; set; }
        public string token { get; set; }
        public bool success { get; set; } = false;
    }
}
