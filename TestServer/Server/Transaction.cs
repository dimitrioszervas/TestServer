namespace TestServer.Server
{
    /// <summary>
    /// Class that handles a signed transaction and stores internaly an unsigned transaction and
    /// the user's signature that performs the transaction.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Transaction
    {
        public sealed class Ephemeral
        {
            public string email { get; set; }
        }

        public SignedTransaction STX { get; set; }
        public Ephemeral? ephemeral { get; set; }
    }
}
