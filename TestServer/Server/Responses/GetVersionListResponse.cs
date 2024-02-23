namespace TestServer.Server.Responses
{
    public class FileVersion
    {
        public string verID { set; get; }
        public string mTS { set; get; }
        public string encMUser { set; get; }
        public string encMUserID { set; get; }
        public long size { set; get; }
        public int verNUM { set; get; }
        public int relNUM { set; get; }
        public string status { set; get; }
        public List<string> tags { set; get; } = new List<string>();
    }

    public class FileVersions
    {
        public string ID { get; set; }
        public string TYP { get; set; }
        public string encNAM { get; set; }

        public List<FileVersion> verLIST { get; set; } = new List<FileVersion>();
    }

    public class GetVersionListResponse : BaseResponse
    {
        public FileVersions FileVersions { get; set; } = new FileVersions();
    }
}
