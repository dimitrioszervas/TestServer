using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PeterO.Cbor;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks.Dataflow;
using TestServer.Server;
using TestServer.Server.Requests;

namespace TestServer.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private const string TRANSACTIONS_RECEIVE_SHARD_END_POINT = "api/Transactions/receive-shard";

        private readonly ILogger<TransactionsController> _logger;
        //private readonly IMapper _mapper;
        //private readonly IServerService _serverService;

        static ConcurrentDictionary<Guid, ShardsPacketConsumer> _transactions = new ConcurrentDictionary<Guid, ShardsPacketConsumer>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="mapper"></param>
        /// <param name="serverService"></param>
        public TransactionsController(ILogger<TransactionsController> logger)//, IMapper mapper)//, IServerService serverService)
        {
            _logger = logger;
            //_mapper = mapper;
            //_serverService = serverService;
        }


        /// <summary>
        /// Replicates received metadata shards (sends shards to the other servers).
        /// </summary>
        /// <param name="shardsPacket"></param>
        //private void ReplicateMetadataShards(ShardsPacket shardsPacket)
        private void ReplicateMetadataShards(byte[] requestBytes)
        {
            try
            {
                Servers.Instance.ReplicateMetadataShards(requestBytes, TRANSACTIONS_RECEIVE_SHARD_END_POINT);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception caught: {ex}");
            }
        }

        /// <summary>
        /// Receives shards packets from the other servers.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/receive-shard
        [HttpPost]
        [Route("receive-shard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ReceiveShardsPacketFromOtherServer()
        {

            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }

            CBORObject requestCBOR = CBORObject.DecodeFromBytes(requestBytes);

            var jsonShardPacket = Encoding.UTF8.GetString(requestCBOR[0].GetByteString());

            var shardsPacket = JsonConvert.DeserializeObject<ShardsPacket>(jsonShardPacket);

            if (_transactions.ContainsKey(shardsPacket.SessionId))
            {
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
            }
            else
            {
                _transactions.TryAdd(shardsPacket.SessionId, new ShardsPacketConsumer());
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
            }

            return Ok();
        }

        private static HttpResponseMessage ReturnBytes(byte[] bytes, HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage result = new HttpResponseMessage(httpStatusCode);
            result.Content = new ByteArrayContent(bytes);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return result;
        }

        // Invite endpoint
        [HttpPost]
        [Route("Invite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<HttpResponseMessage> Invite()
        public async Task<ActionResult> Invite()
        {

            Console.WriteLine("TransactionsController Invite");
            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }

            // servers receive + validate the invite transaction
            try
            {
                _logger.LogInformation("TransactionsController Invite");

                CBORObject requestCBOR = CBORObject.DecodeFromBytes(requestBytes);

                var jsonShardPacket = Encoding.UTF8.GetString(requestCBOR[0].GetByteString());

                var shardsPacket = JsonConvert.DeserializeObject<ShardsPacket>(jsonShardPacket);

                if (!_transactions.ContainsKey(shardsPacket.SessionId))
                {
                    _transactions.TryAdd(shardsPacket.SessionId, new ShardsPacketConsumer());
                }

                var consumerTask = _transactions[shardsPacket.SessionId].ConsumeAsync(_transactions[shardsPacket.SessionId].Buffer, BaseRequest.Invite, false);
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
                ReplicateMetadataShards(requestBytes);
                var results = await consumerTask;
                ShardsPacketConsumer consumer;
                _transactions.TryRemove(shardsPacket.SessionId, out consumer);

                if (results.Count == 0)
                {
                    return StatusCode(400);
                    //return ReturnBytes(new byte[1], HttpStatusCode.BadRequest);
                }

                byte[] responseBytes = results[BaseRequest.Invite];

                ShardsPacket responseShardPacket = Servers.Instance.GetShardPacket(responseBytes);

                byte[] shardPacketBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseShardPacket));

                //return ReturnBytes(shardPacketBytes, HttpStatusCode.OK);
                return Ok(responseShardPacket);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
                //return ReturnBytes(new byte[1], HttpStatusCode.BadRequest);
            }
        }

        // Register endpoint
        [HttpPost]
        [Route("Register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Register()
        {
            Console.WriteLine("TransactionsController Register");
            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }

            // servers receive + validate the register transaction
            try
            {
                _logger.LogInformation("TransactionsController Register");

                CBORObject requestCBOR = CBORObject.DecodeFromBytes(requestBytes);

                var jsonShardPacket = Encoding.UTF8.GetString(requestCBOR[0].GetByteString());

                var shardsPacket = JsonConvert.DeserializeObject<ShardsPacket>(jsonShardPacket);

                if (!_transactions.ContainsKey(shardsPacket.SessionId))
                {
                    _transactions.TryAdd(shardsPacket.SessionId, new ShardsPacketConsumer());
                }

                var consumerTask = _transactions[shardsPacket.SessionId].ConsumeAsync(_transactions[shardsPacket.SessionId].Buffer, BaseRequest.Register, false);
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
                ReplicateMetadataShards(requestBytes);
                var results = await consumerTask;
                ShardsPacketConsumer consumer;
                _transactions.TryRemove(shardsPacket.SessionId, out consumer);

                if (results.Count == 0)
                {
                    return StatusCode(400);
                    //return ReturnBytes(new byte[1], HttpStatusCode.BadRequest);
                }

                byte[] responseBytes = results[BaseRequest.Register];

                ShardsPacket responseShardPacket = Servers.Instance.GetShardPacket(responseBytes);

                byte[] shardPacketBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseShardPacket));

                //return ReturnBytes(shardPacketBytes, HttpStatusCode.OK);
                return Ok(responseShardPacket);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
                //return ReturnBytes(new byte[1], HttpStatusCode.BadRequest);
            }
        }

        // Register endpoint
        [HttpPost]
        [Route("Rekey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Rekey()
        {

            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }

            // servers receive + validate the Rekey transaction
            try
            {
                _logger.LogInformation("TransactionsController Rekey");
                Console.WriteLine("TransactionsController Rekey");

                CBORObject requestCBOR = CBORObject.DecodeFromBytes(requestBytes);

                var jsonShardPacket = Encoding.UTF8.GetString(requestCBOR[0].GetByteString());

                var shardsPacket = JsonConvert.DeserializeObject<ShardsPacket>(jsonShardPacket);

                if (!_transactions.ContainsKey(shardsPacket.SessionId))
                {
                    _transactions.TryAdd(shardsPacket.SessionId, new ShardsPacketConsumer());
                }

                var consumerTask = _transactions[shardsPacket.SessionId].ConsumeAsync(_transactions[shardsPacket.SessionId].Buffer, BaseRequest.Rekey, false);
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
                ReplicateMetadataShards(requestBytes);
                var results = await consumerTask;
                ShardsPacketConsumer consumer;
                _transactions.TryRemove(shardsPacket.SessionId, out consumer);

                if (results.Count == 0)
                {
                    return StatusCode(400);
                    //return ReturnBytes(new byte[1], HttpStatusCode.BadRequest);
                }

                byte[] responseBytes = results[BaseRequest.Rekey];

                ShardsPacket responseShardPacket = Servers.Instance.GetShardPacket(responseBytes);

                byte[] shardPacketBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseShardPacket));

                //return ReturnBytes(shardPacketBytes, HttpStatusCode.OK);
                return Ok(responseShardPacket);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
                //return ReturnBytes(new byte[1], HttpStatusCode.BadRequest);
            }
        }


        // Login endpoint
        [HttpPost]
        [Route("Login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Login()
        {

            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }

            // servers receive + validate the Login transaction
            try
            {
                _logger.LogInformation("TransactionsController Login");
                Console.WriteLine("TransactionsController Login");

                CBORObject requestCBOR = CBORObject.DecodeFromBytes(requestBytes);

                var jsonShardPacket = Encoding.UTF8.GetString(requestCBOR[0].GetByteString());

                var shardsPacket = JsonConvert.DeserializeObject<ShardsPacket>(jsonShardPacket);

                if (!_transactions.ContainsKey(shardsPacket.SessionId))
                {
                    _transactions.TryAdd(shardsPacket.SessionId, new ShardsPacketConsumer());
                }

                var consumerTask = _transactions[shardsPacket.SessionId].ConsumeAsync(_transactions[shardsPacket.SessionId].Buffer, BaseRequest.Login, true);
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
                ReplicateMetadataShards(requestBytes);
                var results = await consumerTask;
                ShardsPacketConsumer consumer;
                _transactions.TryRemove(shardsPacket.SessionId, out consumer);

                if (results.Count == 0)
                {
                    return StatusCode(400);
                    //return ReturnBytes(new byte[1], HttpStatusCode.BadRequest);
                }

                byte[] responseBytes = results[BaseRequest.Login];

                ShardsPacket responseShardPacket = Servers.Instance.GetShardPacket(responseBytes);

                byte[] shardPacketBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseShardPacket));

                //return ReturnBytes(shardPacketBytes, HttpStatusCode.OK);
                return Ok(responseShardPacket);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
                //return ReturnBytes(new byte[1], HttpStatusCode.BadRequest);
            }
        }

        // Session endpoint
        [HttpPost]
        [Route("Session")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Session()
        {

            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }

            // servers receive + validate the Session transaction
            try
            {
                _logger.LogInformation("TransactionsController Session");
                Console.WriteLine("TransactionsController Session");

                CBORObject requestCBOR = CBORObject.DecodeFromBytes(requestBytes);

                var jsonShardPacket = Encoding.UTF8.GetString(requestCBOR[0].GetByteString());

                var shardsPacket = JsonConvert.DeserializeObject<ShardsPacket>(jsonShardPacket);

                if (!_transactions.ContainsKey(shardsPacket.SessionId))
                {
                    _transactions.TryAdd(shardsPacket.SessionId, new ShardsPacketConsumer());
                }

                var consumerTask = _transactions[shardsPacket.SessionId].ConsumeAsync(_transactions[shardsPacket.SessionId].Buffer, BaseRequest.Session, false);
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
                ReplicateMetadataShards(requestBytes);
                var results = await consumerTask;
                ShardsPacketConsumer consumer;
                _transactions.TryRemove(shardsPacket.SessionId, out consumer);

                if (results.Count == 0)
                {
                    return StatusCode(400);
                    //return ReturnBytes(new byte[1], HttpStatusCode.BadRequest);
                }

                byte[] responseBytes = results[BaseRequest.Session];

                ShardsPacket responseShardPacket = Servers.Instance.GetShardPacket(responseBytes);

                byte[] shardPacketBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseShardPacket));

                //return ReturnBytes(shardPacketBytes, HttpStatusCode.OK);
                return Ok(responseShardPacket);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
                //return ReturnBytes(new byte[1], HttpStatusCode.BadRequest);
            }

        }
    }
}