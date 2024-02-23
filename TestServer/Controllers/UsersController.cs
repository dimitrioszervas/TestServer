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
    public class UsersController : ControllerBase
    {
        private const string USERS_RECEIVE_SHARD_END_POINT = "api/Users/receive-shard";

        private readonly ILogger<UsersController> _logger;
        private readonly IMapper _mapper;
        private readonly IServerService _serverService;

        static ConcurrentDictionary<Guid, ShardsPacketConsumer> _transactions = new ConcurrentDictionary<Guid, ShardsPacketConsumer>();

        public UsersController(ILogger<UsersController> logger, IMapper mapper, IServerService serverService)
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
                Servers.Instance.ReplicateMetadataShards(shardsPacket, USERS_RECEIVE_SHARD_END_POINT);
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
        // POST: api/Users/GetProfileImage
        [HttpPost]
        [Route("GetProfileImage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetProfileImage([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("UsersController GetProfileImage");

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

                var response = (GetProfileImageResponse)results[BaseRequest.GetProfileImage];

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
        // POST: api/Users/GetUser
        [HttpPost]
        [Route("GetUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetUser([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("UsersController GetUser");

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

                var response = (GetUserResponse)results[BaseRequest.GetUser];

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
        // POST: api/Users/ListUsers
        [HttpPost]
        [Route("ListUsers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ListUsers([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("UsersController ListUsers");

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

                var response = (ListUsersResponse)results[BaseRequest.ListUsers];

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
        // POST: api/Users/ActivateUser
        [HttpPost]
        [Route("ActivateUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ActivateUser([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("UsersController ActivateUser");

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

                var response = (ActivateUserResponse)results[BaseRequest.ActivateUser];

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
        // POST: api/Users/DeactivateUser
        [HttpPost]
        [Route("DeactivateUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeactivateUser([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("UsersController DeactivateUser");

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

                var response = (DeactivateUserResponse)results[BaseRequest.DeactivateUser];

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
        // POST: api/Users/DeleteUser
        [HttpPost]
        [Route("DeleteUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteUser([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("UsersController DeleteUser");

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

                var response = (DeleteUserResponse)results[BaseRequest.DeleteUser];

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
        // POST: api/Users/GetUserActions
        [HttpPost]
        [Route("GetUserActions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetUserActions([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("UsersController GetUserActions");

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

                var response = (GetUserActionsResponse)results[BaseRequest.GetUserActions];

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
