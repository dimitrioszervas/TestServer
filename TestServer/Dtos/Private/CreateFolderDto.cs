namespace TestServer.Dtos.Private
{
    public class CreateFolderDto
    {
        public ulong ParentId { get; set; }

        public ulong FolderId { get; set; }
        public string FolderName { get; set; }
        public byte[]? EncNodeKey { get; set; }

        public ulong UserId { get; set; }
        public ulong OrgId { get; set; }

        public byte[]? Hash { get; set; }
        public byte[]? Signature { get; set; }
        public bool Realtime { get; set; }
        public ulong TransactionId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public ulong BlockchainId { get; set; }
    }
}
