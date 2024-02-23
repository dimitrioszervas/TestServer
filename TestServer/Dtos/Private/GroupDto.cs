using TestServer.Models.Private;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Dtos.Private
{
    public class GroupDto : BaseDto
    {
        public int Id { get; set; }
        public bool Deleted { get; set; }
        public string wrapKEY { get; set; }
        public string encUnwrapKEY { get; set; }
        public ulong NodeId { get; set; }
    }
}
