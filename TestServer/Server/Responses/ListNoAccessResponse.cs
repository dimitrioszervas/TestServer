using System.Security.Cryptography.X509Certificates;

namespace TestServer.Server.Responses
{
    public sealed class NoAccessUser
    {
        public string Type { get; set; }
        public string ID { get; set; }
        public string encNAM { get; set; }  
        public string dhPublicKEY { get; set; }
    }

    public sealed class NoAccessGroup
    {
        public string ID { get; set; }
        public string encNAM { get; set; }
        public string dhPublicKEY { get; set; }
    }

    public sealed class ListNoAccessResponse : BaseResponse
    {
        public string ID { get; set; }
        public string encUsersFolderKEY { get; set; }
        public string encGroupsFolderKEY { get; set; }
        public List<NoAccessUser> uNoAccess { get; set; } = new List<NoAccessUser>();
        public List<NoAccessGroup> gNoAccess { get; set; } = new List<NoAccessGroup>();
    }
}
