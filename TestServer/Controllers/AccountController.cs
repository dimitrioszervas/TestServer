using AutoMapper;
using TestServer.Contracts;
using TestServer.Dtos.Server;
using TestServer.Server;
using TestServer.Server.Requests;
using TestServer.Server.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace TestServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private const string ACCOUNT_RECEIVE_SHARD_END_POINT = "api/Account/receive-shard";

        private readonly ILogger<RegisterController> _logger;
        private readonly IMapper _mapper;
        private readonly IServerService _serverService;

        static ConcurrentDictionary<Guid, ShardsPacketConsumer> _transactions = new ConcurrentDictionary<Guid, ShardsPacketConsumer>();

        public AccountController(ILogger<RegisterController> logger, IMapper mapper, IServerService serverService)
        {
            _logger = logger;
            _mapper = mapper;
            _serverService = serverService;
        }

        /// <summary>
        /// Replicates received metadata shards (sends shards to the other servers).
        /// </summary>
        /// <param name="shardsPacket"></param>
        private void ReplicateMetadataShards(ShardsPacket shardsPacket)
        {
            try
            {
                Servers.Instance.ReplicateMetadataShards(shardsPacket, ACCOUNT_RECEIVE_SHARD_END_POINT);
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
        // POST: api/Register/receive-shard
        [HttpPost]
        [Route("receive-shard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ReceiveShardsPacketFromOtherServer([FromBody] ShardsPacketDto shardsPacketDto)
        {
            var shardsPacket = _mapper.Map<ShardsPacket>(shardsPacketDto);

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

        /// <summary>
        /// Creates and organisation.
        /// </summary>
        /// <param name="shardPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Account/ChangeUserPassword
        [HttpPost]
        [Route("ChangeUserPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ChangeUserPassword([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("AccountController ChangeUserPassword");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardPacketDto);

                if (!_transactions.ContainsKey(shardsPacket.SessionId))
                {
                    _transactions.TryAdd(shardsPacket.SessionId, new ShardsPacketConsumer());
                }

                var consumerTask = _transactions[shardsPacket.SessionId].ConsumeAsync(_transactions[shardsPacket.SessionId].Buffer, _serverService);
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
                ReplicateMetadataShards(shardsPacket);
                var results = await consumerTask;
                ShardsPacketConsumer consumer;
                _transactions.TryRemove(shardsPacket.SessionId, out consumer);

                if (results.Count == 0)
                {
                    return StatusCode(400);
                }

                var response = (GenericResponse)results[BaseRequest.ChangeUserPassword];

                if (response != null)
                {
                    return Ok();
                }
                else
                {
                    return StatusCode(400);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
            }
        }

        /// <summary>
        /// Creates and organisation.
        /// </summary>
        /// <param name="shardPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Account/Login
        [HttpPost]
        [Route("Login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Login([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("AccountController Login");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardPacketDto);

                if (!_transactions.ContainsKey(shardsPacket.SessionId))
                {
                    _transactions.TryAdd(shardsPacket.SessionId, new ShardsPacketConsumer());
                }

                var consumerTask = _transactions[shardsPacket.SessionId].ConsumeAsync(_transactions[shardsPacket.SessionId].Buffer, _serverService);
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
                ReplicateMetadataShards(shardsPacket);
                var results = await consumerTask;
                ShardsPacketConsumer consumer;
                _transactions.TryRemove(shardsPacket.SessionId, out consumer);

                if (results.Count == 0)
                {
                    return StatusCode(400);
                }

                var response = (LoginResponse)results[BaseRequest.Login];

                string jsonString = JsonSerializer.Serialize(response);

                byte[] responseBytes = Encoding.UTF8.GetBytes(jsonString);

                _logger.LogInformation($"response: {jsonString}");

                ShardsPacket responseShardPacket = Servers.Instance.GetShardPacket(responseBytes);

                return Ok(responseShardPacket);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
            }
        }

        /// <summary>
        /// Creates and organisation.
        /// </summary>
        /// <param name="shardPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Account/ListDevices
        [HttpPost]
        [Route("ListDevices")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ListDevices([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("AccountController ListDevices");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardPacketDto);

                if (!_transactions.ContainsKey(shardsPacket.SessionId))
                {
                    _transactions.TryAdd(shardsPacket.SessionId, new ShardsPacketConsumer());
                }

                var consumerTask = _transactions[shardsPacket.SessionId].ConsumeAsync(_transactions[shardsPacket.SessionId].Buffer, _serverService);
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
                ReplicateMetadataShards(shardsPacket);
                var results = await consumerTask;
                ShardsPacketConsumer consumer;
                _transactions.TryRemove(shardsPacket.SessionId, out consumer);

                if (results.Count == 0)
                {
                    return StatusCode(400);
                }

                var response = (ListDevicesResponse)results[BaseRequest.ListDevices];

                string jsonString = JsonSerializer.Serialize(response);

                byte[] responseBytes = Encoding.UTF8.GetBytes(jsonString);

                _logger.LogInformation($"response: {jsonString}");

                ShardsPacket responseShardPacket = Servers.Instance.GetShardPacket(responseBytes);

                return Ok(responseShardPacket);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
            }
        }
    }
}
