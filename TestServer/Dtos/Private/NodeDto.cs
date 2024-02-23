namespace TestServer.Dtos.Private
{
    public class NodeDto : BaseDto
    {
        public ulong Id { get; set; }
        public byte Type { get; set; }
        public int? CurrentParentId { get; set; }
        public ulong? CurrentVersionId { get; set; }
        public ulong? CurrentOwnerId { get; set; }
        public string? CurrentNodeAesWrapByParentNodeAes { get; set; }
    }
}
