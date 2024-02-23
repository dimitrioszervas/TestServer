namespace TestServer.Server.Responses
{
   
    public sealed class FileDetails
    {    
        public string ID { get; set; }
        public string TYP { get; set; }    
        public string encNAM { get; set; }
        public bool hasChild { get; set; }        
        public string mTS { get; set; }     
        public long size { get; set; }
        public string status { get; set; }
        public string modifiedBy { get; set; }
        public string modifiedByID {  get; set; }

        public FileDetails(string iD, string tYP, string encNAM, bool hasChild, string mTS, long size, string status, string modifiedBy, string modifiedByID)
        {
            ID = iD;
            TYP = tYP;
            this.encNAM = encNAM;
            this.hasChild = hasChild;
            this.mTS = mTS;
            this.size = size;
            this.status = status;
            this.modifiedBy = modifiedBy;
            this.modifiedByID = modifiedByID;
        }
    }
    
    public sealed class ListFilesResponse : BaseResponse
    {     
        public string encKEY { get; set; }
        public string wrapNodeKey { get; set; }
        public string encGroupUnwrapKey { get; set; }
        public ulong flags { get; set; } 
        public List<FileDetails> nodes { get; set; } = new List<FileDetails>();        
    }
}
