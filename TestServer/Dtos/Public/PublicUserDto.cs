namespace TestServer.Dtos.Public
{
    public class PublicUserDto : BaseDto
    {
        public ulong Id { get; set; }
        public ulong? OrgId { get; set; }
        public ulong? CertId { get; set; }
    }
}
