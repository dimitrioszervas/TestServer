namespace TestServer.Exceptions
{
    public class NotFoundException : ApplicationException
    {
        public NotFoundException(string msg, object key) : base($"{msg} with id ({key}) was not found")
        {

        }
    }
}
