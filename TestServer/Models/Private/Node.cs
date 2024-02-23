using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public enum NodeType : byte
    {
        Org,
        UsersFolder,
        User,
        Folder,
        File,
        GroupsFolder,
        Group,
        GroupMember,
        Permissions,
        Json,
        Device,
        Blockchain,
    }

    public class Node : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }
        public byte Type { get; set; }
        public int? CurrentParentId { get; set; }
        public ulong? CurrentVersionId { get; set; }
        public ulong? CurrentOwnerId { get; set; }
        public string? CurrentNodeAesWrapByParentNodeAes { get; set; }
    }
}
