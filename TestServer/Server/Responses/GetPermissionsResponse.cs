namespace TestServer.Server.Responses
{
    public sealed class UserPermissions
    {
        public string Name { get; set; }
        public ulong Id { get; set; }
        public ulong Permissions { get; set; } = 0;
    }

    public sealed class GetPermissionsResponse : BaseResponse
    {
        public List<UserPermissions> Users { get; set; } = new List<UserPermissions>();
    }
}
