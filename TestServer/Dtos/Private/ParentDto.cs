namespace TestServer.Dtos.Private
{
    public class ParentDto : BaseDto
    {
        public int Id { get; set; }
        public string NameEncByParentEncKey { get; set; }
        public ulong? ParentNodeId { get; set; }
        public ulong NodeId { get; set; }
        public string? NodeKeyWrappedByParentNodeKey { get; set; }
        public bool CurrentParent { get; set; }
    }
}
