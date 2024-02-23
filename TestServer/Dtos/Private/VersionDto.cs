using TestServer.Models.Private;

namespace TestServer.Dtos.Private
{
    public class VersionDto : BaseTable
    {
        public ulong Id { get; set; }
        public long Size { get; set; }
        public ulong NodeId { get; set; }
    }
}
