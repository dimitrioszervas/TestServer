using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Dtos.Private
{
    public class ApprovalDto : BaseDto
    {
        public ulong Id { get; set; }
        public byte Type { get; set; }
        public string? Comment { get; set; }
        public ulong VersionId { get; set; }
    }
}
