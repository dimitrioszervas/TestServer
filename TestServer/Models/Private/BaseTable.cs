using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public abstract class BaseTable
    {
        public ulong UserNodeId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
