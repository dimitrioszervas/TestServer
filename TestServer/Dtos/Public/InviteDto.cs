namespace TestServer.Dtos.Public
{
    public class InviteDto : BaseDto
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public string InviteeEmail { get; set; }
        public bool Revoked { get; set; }
        public ulong AccessCode { get; set; }
    }
}
