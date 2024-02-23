namespace TestServer.Dtos.Public
{
    public class EmailDto : BaseDto
    {
        public int Id { get; set; }
        public string EmailAddress { get; set; }
        public ulong UserId { get; set; }
    }
}
