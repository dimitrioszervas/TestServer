using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Public
{
    public class Email : BaseTable
    {
        public int Id { get; set; }
        public string EmailAddress { get; set; }
        [ForeignKey(nameof(UserId))]
        public ulong UserId { get; set; }
        public PublicUser User { get; set; }
    }
}
