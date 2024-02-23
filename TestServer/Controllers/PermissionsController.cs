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
    public class PermissionsController : ControllerBase
    {
        private const string PERMISSIONS_RECEIVE_SHARD_END_POINT = "api/Permissions/receive-shard";

        private readonly ILogger<PermissionsController> _logger;
        private readonly IMapper _mapper;
        private readonly IServerService _serverService;

        static ConcurrentDictionary<Guid, ShardsPacketConsumer> _transactions = new ConcurrentDictionary<Guid, ShardsPacketConsumer>();

        public PermissionsController(ILogger<PermissionsController> logger, IMapper mapper, IServerService serverService)
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
                Servers.Instance.ReplicateMetadataShards(shardsPacket, PERMISSIONS_RECEIVE_SHARD_END_POINT);
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
        // POST: api/Permissions/SharedWith
        [HttpPost]
        [Route("SharedWith")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SharedWith([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController SharedWith");

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

                var response = (SharedWithResponse)results[BaseRequest.SharedWith];

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
        // POST: api/Permissions/CreateGroup
        [HttpPost]
        [Route("CreateGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateGroup([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController CreateGroup");

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

                var response = (CreateGroupResponse)results[BaseRequest.CreateGroup];

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
        // POST: api/Permissions/GetGroup
        [HttpPost]
        [Route("GetGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetGroup([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController GetGroup");

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

                var response = (GetGroupResponse)results[BaseRequest.GetGroup];

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
        // POST: api/Permissions/ListGroups
        [HttpPost]
        [Route("ListGroups")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ListGroups([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController ListGroups");

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

                var response = (ListGroupsResponse)results[BaseRequest.ListGroups];

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
        // POST: api/Permissions/RenameGroup
        [HttpPost]
        [Route("RenameGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RenameGroup([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController RenameGroup");

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

                var response = (RenameGroupResponse)results[BaseRequest.RenameGroup];

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
        // POST: api/Permissions/RemoveGroup
        [HttpPost]
        [Route("RemoveGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RemoveGroup([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController RemoveGroup");

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

                var response = (RemoveGroupResponse)results[BaseRequest.RemoveGroup];

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
        // POST: api/Permissions/AddUserToGroup
        [HttpPost]
        [Route("AddUserToGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> AddUserToGroup([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController AddUserToGroup");

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

                var response = (AddUserToGroupResponse)results[BaseRequest.AddUserToGroup];

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
        // POST: api/Permissions/RemoveUserFromGroup
        [HttpPost]
        [Route("RemoveUserFromGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RemoveUserFromGroup([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController RemoveUserFromGroup");

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

                var response = (RemoveUserFromGroupResponse)results[BaseRequest.RemoveUserFromGroup];

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
        /// Add a file/folder to writers, readers, administrators, etc, ListFiles.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Permissions/GrantAccess
        [HttpPost]
        [Route("GrantAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GrantAccess([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController GrantAccess");

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

                var response = (GrantAccessResponse)results[BaseRequest.GrantAccess];

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
        /// Remove a file/folder from writers, readers, administrators, etc, ListFiles.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/FileManager/RevokeAccess
        [HttpPost]
        [Route("RevokeAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RevokeAccess([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController RevokeAccess");

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

                var response = (RevokeAccessResponse)results[BaseRequest.RevokeAccess];

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
        /// Remove a file/folder from writers, readers, administrators, etc, ListFiles.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/FileManager/AddPermissions
        [HttpPost]
        [Route("AddPermissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> AddPermissions([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController AddPermissions");

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

                var response = (AddPermissionsResponse)results[BaseRequest.AddPermissions];

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
        /// Remove a file/folder from writers, readers, administrators, etc, ListFiles.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/FileManager/RemovePermission
        [HttpPost]
        [Route("RemovePermission")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RemovePermission([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController RemovePermission");

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

                var response = (RemovePermissionResponse)results[BaseRequest.RemovePermission];

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
        /// Remove a file/folder from writers, readers, administrators, etc, ListFiles.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/FileManager/DisableInherit
        [HttpPost]
        [Route("DisableInherit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DisableInherit([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController DisableInherit");

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

                var response = (GenericResponse)results[BaseRequest.DisableInherit];

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
        /// Remove a file/folder from writers, readers, administrators, etc, ListFiles.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/FileManager/EnableInherit
        [HttpPost]
        [Route("EnableInherit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> EnableInherit([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController EnableInherit");

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

                var response = (GenericResponse)results[BaseRequest.EnableInherit];

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
        // POST: api/Groups/ActivateGroup
        [HttpPost]
        [Route("ActivateGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ActivateGroup([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController ActivateGroup");

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

                var response = (ActivateGroupResponse)results[BaseRequest.ActivateGroup];

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
        // POST: api/Groups/DeactivateGroup
        [HttpPost]
        [Route("DeactivateGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeactivateGroup([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController DeactivateGroup");

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

                var response = (DeactivateGroupResponse)results[BaseRequest.DeactivateGroup];

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
        // POST: api/Groups/DeleteGroup
        [HttpPost]
        [Route("DeleteGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteGroup([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController DeleteGroup");

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

                var response = (DeleteGroupResponse)results[BaseRequest.DeleteGroup];

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
        // POST: api/Permissions/GetPermissionTypes
        [HttpPost]
        [Route("GetPermissionTypes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetPermissionTypes([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController GetPermissionTypes");

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

                var response = (GetPermissionTypesResponse)results[BaseRequest.GetPermissionTypes];

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
        // POST: api/Permissions/ListNoAccess
        [HttpPost]
        [Route("ListNoAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ListNoAccess([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController ListNoAccess");

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

                var response = (ListNoAccessResponse)results[BaseRequest.ListNoAccess];

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
        // POST: api/Permissions/CanCreate
        [HttpPost]
        [Route("CanCreate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CanCreate([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController CanCreate");

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

                var response = (HasUserPermissionResponse)results[BaseRequest.CanCreate];

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
        // POST: api/Permissions/CanUpdate
        [HttpPost]
        [Route("CanUpdate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CanUpdate([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController CanUpdate");

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

                var response = (HasUserPermissionResponse)results[BaseRequest.CanUpdate];

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
        // POST: api/Permissions/CanDelete
        [HttpPost]
        [Route("CanDelete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CanDelete([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController CanDelete");

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

                var response = (HasUserPermissionResponse)results[BaseRequest.CanDelete];

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
        // POST: api/Permissions/CanShare
        [HttpPost]
        [Route("CanShare")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CanShare([FromBody] ShardsPacketDto shardPacketDto)
        {
            try
            {
                _logger.LogInformation("PermissionsController CanShare");

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

                var response = (HasUserPermissionResponse)results[BaseRequest.CanShare];

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
