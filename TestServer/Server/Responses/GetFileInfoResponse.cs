namespace TestServer.Server.Responses
{
    public class FileInfo
    {
        public string ID { set; get; }
        public string TYP { set; get; }
        public string encNAM { set; get; }
        public bool hasChild { set; get; }
        public string encOwner { set; get; }
        public string encOwnerID { set; get; }
        public string cTS { set; get; }
        public string mTS { set; get; }
        public string encMUser { set; get; }
        public string encMUserID { set; get; }
        public long size { set; get; }
        public int verNUM { set; get; }
        public int relNUM { set; get; }
        public string status { set; get; }
        public List<string> tags { set; get; } = new List<string>(); 
    }  

    public class GetFileInfoResponse : BaseResponse
    {
        public FileInfo FileInfo { set; get; } = new FileInfo();
    }
}
