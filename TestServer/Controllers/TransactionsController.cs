using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Transactions;

namespace TestServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        [HttpPost]
        [Route("PostTransaction")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> PostTransaction()
        {
            string body = "";
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
                Console.WriteLine(body);
            }

            return Ok("Hello!");
        }
    }
}
