namespace TestServer.Dtos.Public
{
    public class DomainDto : BaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ulong OrgId { get; set; }
    }
}
