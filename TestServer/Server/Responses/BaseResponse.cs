namespace TestServer.Server.Responses
{
    public abstract class BaseResponse
    {
        public const string STATUS_TYPE_DRAFT = "draft";
        public const string STATUS_TYPE_RELEASE = "release";

        public string tID { get; set; }
        public string TYP { get; set; }
    }
}
