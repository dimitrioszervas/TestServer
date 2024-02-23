namespace TestServer.Server.Responses
{
    public class FileAction
    {
        public string ID { get; set; }
        public string TS { get; set; }
        public string uEncNAM { get; set; }
        public string encCom { get; set; }

        public FileAction(string iD, string tS, string uEncNAM, string encCom)
        {
            ID = iD;
            TS = tS;
            this.uEncNAM = uEncNAM;
            this.encCom = encCom;
        }
    }

    public class GetFileActionsResponse : BaseResponse
    {
        public List<FileAction> actions { get; set; } = new List<FileAction>();
    }
}
