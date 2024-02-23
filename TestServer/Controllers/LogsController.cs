using Microsoft.AspNetCore.Mvc;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TestServer.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private const string LOGS_DIR = "logs";

        [HttpGet]
        [Route("GetLog")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        // Get: api/Logs/GetLog
        public async Task<ActionResult> GetLog()
        {
            DateTime currentDateTime = DateTime.Now;
            string currentDateString = currentDateTime.ToString("yyyy-MM-dd");


            if (!Directory.Exists(LOGS_DIR))
            {
                return Ok("No logs file found!");
            }

            string[] listOfFiles = Directory.GetFiles(LOGS_DIR);

            if (listOfFiles.Length == 0)
            {
                return Ok("No logs file found!");
            }

            Array.Sort(listOfFiles);

            string[] latestLogFile = System.IO.File.ReadAllLines(@listOfFiles[listOfFiles.Length - 1]);

            StringBuilder sb = new StringBuilder();

            foreach (string line in latestLogFile)
            {
                if (line.StartsWith(currentDateString))
                {
                    sb.AppendLine(line);
                }
            }

            return Ok(sb.ToString());
        }
    }
}
