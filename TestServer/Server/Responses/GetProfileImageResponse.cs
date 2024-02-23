namespace TestServer.Server.Responses
{
    public class ProfileImage
    {
        public string ID { get; set; }
        public string TYP { get; set; }
        public string encFILE { get; set; }
    }

    public class GetProfileImageResponse : BaseResponse
    {
        public ProfileImage ProfileImage { get; set; } = new ProfileImage();
    }
}
