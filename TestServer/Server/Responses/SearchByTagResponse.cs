namespace TestServer.Server.Responses
{
    public sealed class SearchedByTagFile
    {
        public string ID { get; set; }
        public string TYP { get; set; }
        public string encNAM { get; set; }
        public string mTS { get; set; }
        public long size { get; set; }
        public string status { get; set; }

        public SearchedByTagFile(string iD, string tYP, string encNAM, string mTS, long size, string status)
        {
            ID = iD;
            TYP = tYP;
            this.encNAM = encNAM;
            this.mTS = mTS;
            this.size = size;
            this.status = status;
        }
    }

    public class SearchByTagResponse : BaseResponse
    {
        public List<SearchedByTagFile> files { get; set; } = new List<SearchedByTagFile> ();
    }
}
