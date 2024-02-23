namespace TestServer.Server.Responses
{
    public sealed class UploadResponse : GenericResponse
    {
        public ListFilesResponse fileManagerResponse { get; set; } = new ListFilesResponse();
        public string VersionId { get; set; }       
    }
}
