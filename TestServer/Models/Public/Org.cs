using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Public
{
    public class Org : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }
        public string? Name { get; set; }
    }
}
