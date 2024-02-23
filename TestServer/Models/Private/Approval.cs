using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public enum ApprovalType : byte
    {
        Review,
        Approval,
        Release
    }

    public class Approval : BaseTable
    {

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }
        public byte Type { get; set; }
        public string? Comment { get; set; }
        [ForeignKey(nameof(VersionId))]
        public ulong VersionId { get; set; }
        public Version Version { get; set; }
    }
}
