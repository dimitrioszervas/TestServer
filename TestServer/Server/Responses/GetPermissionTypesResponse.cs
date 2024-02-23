namespace TestServer.Server.Responses
{
    public sealed class PermissionTypeObject
    {
        public PermissionTypeObject(string name, ulong value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public ulong Value { get; set; }
    }

    public sealed class GetPermissionTypesResponse : BaseResponse
    {
       public List<PermissionTypeObject> PermissionTypes { get; set; } = new List<PermissionTypeObject>();
    }
}
