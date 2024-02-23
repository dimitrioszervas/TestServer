namespace TestServer.Server.Requests
{
    public sealed class SetControlledFlagRequest : BaseRequest
    {
        public string ID { get; set; }
        public bool controlledFlag { get; set; }
    }
}
