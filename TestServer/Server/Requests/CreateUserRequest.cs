namespace TestServer.Server.Requests
{
    public sealed class CreateUserRequest : BaseRequest
    {     
        public string ID { get; set; }
        public string encNAM { get; set; }
        public string CurrentNodeAesWrapByParentNodeAes { get; set; }
        public string encEmail { get; set; }       
    }
}
