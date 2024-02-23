namespace TestServer.Server.Responses
{
    public class GenericResponse : BaseResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}
