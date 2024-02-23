namespace TestServer.Server.Responses
{
    public class WithAccess
    {
        public string ID { get; set; }
        public string encNAM { get; set; }
        public List<PermissionTypeObject> permissionTypes { get; set; } = new List<PermissionTypeObject>();
        public List<PermissionTypeObject> inheritedPermissions { get; set; } = new List<PermissionTypeObject>();
    }

    public class SharedWithResponse : BaseResponse
    {     
        public string ID { get; set; }
        public string encUsersFolderKEY { get; set; }
        public string encGroupsFolderKEY { get; set; }
        public List<WithAccess> uWithAccess { get; set; } = new List<WithAccess>();
        public List<WithAccess> gWithAccess { get; set; } = new List<WithAccess>();       
    }
}
