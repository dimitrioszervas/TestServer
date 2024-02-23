using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Server.Requests
{
    public sealed class AddReviewRequest : BaseRequest
    {
        public string ID { get; set; }
        public string rID { get; set; }
        public byte rTYP { get; set; }
        public string? encCom { get; set; }        
        public string vID { get; set; }
    }
}
