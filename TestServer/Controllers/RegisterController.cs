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
    /// <summary>
    /// Handles file manager stuff.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private const string REGISTER_RECEIVE_SHARD_END_POINT = "api/Register/receive-shard";

        private readonly ILogger<RegisterController> _logger;
        private readonly IMapper _mapper;
        private readonly IServerService _serverService;

        static ConcurrentDictionary<Guid, ShardsPacketConsumer> _transactions = new ConcurrentDictionary<Guid, ShardsPacketConsumer>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="mapper"></param>
        /// <param name="serverService"></param>
        public RegisterController(ILogger<RegisterController> logger, IMapper mapper, IServerService serverService)
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
                Servers.Instance.ReplicateMetadataShards(shardsPacket, REGISTER_RECEIVE_SHARD_END_POINT);
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
        /// Gets the number of the number of the children of a file/folder (is used before Copy so you will know how many new file/folder GUID ids you have to generate). 
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Register/CreateUser
        [HttpPost]
        [Route("CreateUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateUser([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController CreateUser");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardsPacketDto);

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

                var response = (CreateUserResponse)results[BaseRequest.CreateUser];

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
        /// Gets the number of the number of the children of a file/folder (is used before Copy so you will know how many new file/folder GUID ids you have to generate). 
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Register/InviteUser
        [HttpPost]
        [Route("InviteUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> InviteUser([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController InviteUser");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardsPacketDto);

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

                var response = (InviteUserResponse)results[BaseRequest.InviteUser];

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
        /// Gets the number of the number of the children of a file/folder (is used before Copy so you will know how many new file/folder GUID ids you have to generate). 
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Register/RegisterUser
        [HttpPost]
        [Route("RegisterUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RegisterUser([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController RegisterUser");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardsPacketDto);

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

                var response = (RegisterUserResponse)results[BaseRequest.RegisterUser];

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
        /// Gets the number of the number of the children of a file/folder (is used before Copy so you will know how many new file/folder GUID ids you have to generate). 
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Register/CreateDevice
        [HttpPost]
        [Route("CreateDevice")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateDevice([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController CreateDevice");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardsPacketDto);

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

                var response = (CreateDeviceResponse)results[BaseRequest.CreateDevice];

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
        /// Gets the number of the number of the children of a file/folder (is used before Copy so you will know how many new file/folder GUID ids you have to generate). 
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Register/RegisterDevice
        [HttpPost]
        [Route("RegisterDevice")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RegisterDevice([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController RegisterDevice");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardsPacketDto);

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

                var response = (RegisterDeviceResponse)results[BaseRequest.RegisterDevice];

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

        // POST: api/Register/ActivateDevice
        [HttpPost]
        [Route("ActivateDevice")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ActivateDevice([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController ActivateDevice");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardsPacketDto);

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

                var response = (ActivateDeviceResponse)results[BaseRequest.ActivateDevice];

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

        // POST: api/Register/DeactivateDevice
        [HttpPost]
        [Route("DeactivateDevice")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeactivateDevice([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController DeactivateDevice");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardsPacketDto);

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

                var response = (DeactivateDeviceResponse)results[BaseRequest.DeactivateDevice];

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

        // POST: api/Register/RevokeDevice
        [HttpPost]
        [Route("RevokeDevice")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RevokeDevice([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController RevokeDevice");

                var shardsPacket = _mapper.Map<ShardsPacket>(shardsPacketDto);

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

                var response = (RevokeDeviceResponse)results[BaseRequest.RevokeDevice];

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
        // POST: api/Register/CreateOrg
        [HttpPost]
        [Route("CreateOrg")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateOrg([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController CreateOrg");

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

                var response = (CreateOrgResponse)results[BaseRequest.CreateOrg];

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
        // POST: api/Register/InviteOrg
        [HttpPost]
        [Route("InviteOrg")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> InviteOrg([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController InviteOrg");

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

                var response = (InviteOrgResponse)results[BaseRequest.InviteOrg];

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
        // POST: api/Register/RegisterOrg
        [HttpPost]
        [Route("RegisterOrg")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RegisterOrg([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController RegisterOrg");

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

                var response = (RegisterOrgResponse)results[BaseRequest.RegisterOrg];

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
        /// 
        /// </summary>
        /// <param name="shardPacketDto"></param>
        /// <returns></returns>
        // POST: api/Register/CreateUserAndDevice
        [HttpPost]
        [Route("CreateUserAndDevice")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateUserAndDevice([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController CreateUserAndDevice");

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

                var response = (ListUsersResponse)results[BaseRequest.CreateUserAndDevice];

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
        /// 
        /// </summary>
        /// <param name="shardPacketDto"></param>
        /// <returns></returns>
        // POST: api/Register/RegisterUserAndDevice
        [HttpPost]
        [Route("RegisterUserAndDevice")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RegisterUserAndDevice([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController RegisterUserAndDevice");

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

                var response = (RegisterUserAndDeviceResponse)results[BaseRequest.RegisterUserAndDevice];

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

        // POST: api/Register/SendNewUserAndDeviceCode
        [HttpPost]
        [Route("SendNewUserAndDeviceCode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SendNewUserAndDeviceCode([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController SendNewUserAndDeviceCode");

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

                var response = (SendNewUserAndDeviceCodeResponse)results[BaseRequest.SendNewUserAndDeviceCode];

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

        // POST: api/Register/GetAttribute
        [HttpPost]
        [Route("GetAttribute")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetAttribute([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("RegisterController GetAttribute");

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

                var response = (GetAttributeResponse)results[BaseRequest.GetAttribute];

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
