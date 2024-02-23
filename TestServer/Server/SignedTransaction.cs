namespace TestServer.Server
{
    public sealed class SignedTransaction
    {
        public string UTX { get; set; }       
        public string SIG { get; set; }

        /// <summary>
        /// Returns the unsigned transaction's (UTX) Json string in bytes.
        /// </summary>
        /// <returns>a byte array</returns>
        public byte[] GetUTXBytes()
        {
            return Convert.FromBase64String(UTX);
        }
    }
}
