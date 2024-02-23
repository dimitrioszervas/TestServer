namespace TestServer.Server.Responses
{
    public sealed class GetNodeKeyResponse : BaseResponse
    {
        public string CurrentNodeAesWrapByParentNodeAes {  get; set; }
    }
}
