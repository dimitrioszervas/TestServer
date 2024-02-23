namespace TestServer.Server.Responses
{
    public sealed class UserActionInfo
    {     
        public string ActionType { get; set; }       
        public string NodeID { get; set; }
        public string NodeType { get; set; }
        public string NodeEncName { get; set; }       
        public string Timestamp { get; set; } = string.Empty;
    }

    public sealed class GetUserActionsResponse : BaseResponse
    {
        public List<UserActionInfo> Actions { get; set; } = new List<UserActionInfo>();
    }
}
