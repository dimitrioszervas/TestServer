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
    public class FilesController : ControllerBase
    {
        private const string FILES_RECEIVE_SHARD_END_POINT = "api/Files/receive-shard";

        private readonly ILogger<FilesController> _logger;
        private readonly IMapper _mapper;
        private readonly IServerService _serverService;

        static ConcurrentDictionary<Guid, ShardsPacketConsumer> _transactions = new ConcurrentDictionary<Guid, ShardsPacketConsumer>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="mapper"></param>
        /// <param name="serverService"></param>
        public FilesController(ILogger<FilesController> logger, IMapper mapper, IServerService serverService)
        {
            _logger = logger;
            _mapper = mapper;
            _serverService = serverService;
        }

        [HttpGet]
        [Route("Test")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //GET: api/Files/Test
        public async Task<ActionResult> Test()
        {
            return Ok("Hello from Server");
        }

        /// <summary>
        /// Replicates received metadata shards (sends shards to the other servers).
        /// </summary>
        /// <param name="shardsPacket"></param>
        private void ReplicateMetadataShards(ShardsPacket shardsPacket)
        {
            try
            {
                Servers.Instance.ReplicateMetadataShards(shardsPacket, FILES_RECEIVE_SHARD_END_POINT);
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
        /// Creates a folder.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/CreateFolder
        [HttpPost]
        [Route("CreateFolder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateFolder([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController CreateFolder");

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

                var response = (ListFilesResponse)results[BaseRequest.CreateFolder];

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
        /// Uploads a new file or file version.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/CreateFile
        [HttpPost]
        [Route("CreateFile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateFile([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController CreateFile");

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

                UploadResponse response = (UploadResponse)results[BaseRequest.CreateFile];

                if (response == null)
                {
                    return StatusCode(400);
                }

                var result = Servers.Instance.UploadFileShards(shardsPacket, response.VersionId);

                if (!result)
                {
                    return StatusCode(400);
                }

                string jsonString = JsonSerializer.Serialize(response.fileManagerResponse);

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
        /// Uploads a new file or file version.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/CreateVersion
        [HttpPost]
        [Route("CreateVersion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateVersion([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController CreateVersion");

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

                UploadResponse response = (UploadResponse)results[BaseRequest.CreateVersion];

                if (response == null)
                {
                    return StatusCode(400);
                }

                var result = Servers.Instance.UploadFileShards(shardsPacket, response.VersionId);

                if (!result)
                {
                    return StatusCode(400);
                }

                string jsonString = JsonSerializer.Serialize(response.fileManagerResponse);

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
        /// Opens a file.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/OpenFile
        [HttpPost]
        [Route("OpenFile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> OpenFile([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController OpenFile");

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

                var response = (OpenFileResponse)results[BaseRequest.OpenFile];

                if (response == null)
                {
                    return StatusCode(400);
                }

                //string jsonString = JsonSerializer.Serialize(response.ShardsPacket);

                //_logger.LogInformation($"response: {jsonString}");

                return Ok(response.ShardsPacket);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
            }
        }

        /// <summary>
        /// Opens a previous version of the file.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/OpenPreviousVersion
        [HttpPost]
        [Route("OpenPreviousVersion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> OpenPreviousVersion([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController OpenPreviousVersion");

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

                var response = (OpenFileResponse)results[BaseRequest.OpenPreviousVersion];

                if (response == null)
                {
                    return StatusCode(400);
                }

                //string jsonString = JsonSerializer.Serialize(response.ShardsPacket);

                //_logger.LogInformation($"response: {jsonString}");

                return Ok(response.ShardsPacket);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
            }
        }

        /// <summary>
        /// Creates a review for a file.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/AddReview
        [HttpPost]
        [Route("AddReview")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> AddReview([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController AddReview");

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

                AddReviewResponse response = (AddReviewResponse)results[BaseRequest.AddReview];

                if (response == null)
                {
                    return StatusCode(400);
                }

                var result = Servers.Instance.UploadFileShards(shardsPacket, response.ID);

                if (!result)
                {
                    return StatusCode(400);
                }

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
        /// Get a review of a file.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/GetReview
        [HttpPost]
        [Route("GetReview")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetReview([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController GetReview");

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

                var response = (OpenFileResponse)results[BaseRequest.GetReview];

                if (response == null)
                {
                    return StatusCode(400);
                }

                //string jsonString = JsonSerializer.Serialize(response.ShardsPacket);

                //_logger.LogInformation($"response: {jsonString}");

                return Ok(response.ShardsPacket);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                return StatusCode(400);
            }
        }

        /// <summary>
        /// Gets a file's info (size, date created etc).
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/GetFileInfo
        [HttpPost]
        [Route("GetFileInfo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetFileInfo([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController GetFileInfo");

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

                var response = (GetFileInfoResponse)results[BaseRequest.GetFileInfo];

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
        /// Renames a file/folder.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/Rename
        [HttpPost]
        [Route("Rename")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Rename([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController Rename");

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

                var response = (ListFilesResponse)results[BaseRequest.Rename];

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
        /// Deletes a file/folder.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/Delete
        [HttpPost]
        [Route("Delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Delete([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController Delete");

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

                var response = (ListFilesResponse)results[BaseRequest.Delete];

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
        /// Moves a file/folder.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/Move
        [HttpPost]
        [Route("Move")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Move([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController Move");

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

                var response = (ListFilesResponse)results[BaseRequest.Move];

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
        /// Copies a file/folder.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/Copy
        [HttpPost]
        [Route("Copy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Copy([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController Copy");

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

                var response = (ListFilesResponse)results[BaseRequest.Copy];

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
        /// ListFiles the contents of a folder.
        /// </summary>
        /// <param name="shardsPacketDto"></param>
        /// <returns>Status200OK & ShardsPacket.<br/>Status400BadRequest.<br/>Status500InternalServerError.</returns>
        // POST: api/Files/ListFiles
        [HttpPost]
        [Route("ListFiles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ListFiles([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController ListFiles");

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

                var response = (ListFilesResponse)results[BaseRequest.ListFiles];

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
        // POST: api/Files/GetNumberOfNodes
        [HttpPost]
        [Route("GetNumberOfNodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetNumberOfNodes([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController GetNumberOfNodes");

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

                var response = (GetNumberOfNodesResponse)results[BaseRequest.GetNumberOfNodes];

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
        // POST: api/Files/GetVersionList
        [HttpPost]
        [Route("GetVersionList")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetVersionList([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController GetVersionList");

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

                var response = (GetVersionListResponse)results[BaseRequest.GetVersionList];

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
        // POST: api/Files/SetControlledFlag
        [HttpPost]
        [Route("SetControlledFlag")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SetControlledFlag([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController SetControlledFlag");

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

                var response = (UpdateFileStatusResponse)results[BaseRequest.SetControlledFlag];

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
        // POST: api/Files/AddFileTag
        [HttpPost]
        [Route("AddFileTag")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> AddFileTag([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController AddFileTag");

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

                var response = (AddFileTagResponse)results[BaseRequest.AddFileTag];

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
        // POST: api/Files/RemoveFileTag
        [HttpPost]
        [Route("RemoveFileTag")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RemoveFileTag([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController RemoveFileTag");

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

                var response = (RemoveFileTagResponse)results[BaseRequest.RemoveFileTag];

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
        // POST: api/Files/SearchByTag
        [HttpPost]
        [Route("SearchByTag")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SearchByTag([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController SearchByTag");

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

                var response = (SearchByTagResponse)results[BaseRequest.SearchByTag];

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
        // POST: api/Files/GetFileActions
        [HttpPost]
        [Route("GetFileActions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetFileActions([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController GetFileActions");

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

                var response = (GetFileActionsResponse)results[BaseRequest.GetFileActions];

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
        // POST: api/Files/RemoveReview
        [HttpPost]
        [Route("RemoveReview")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RemoveReview([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController RemoveReview");

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

                var response = (RemoveReviewResponse)results[BaseRequest.RemoveReview];

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
        // POST: api/Files/Approval
        [HttpPost]
        [Route("Approval")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Approval([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController Approval");

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

                var response = (ApprovalResponse)results[BaseRequest.Approval];

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
        // POST: api/Files/RevokeApproval
        [HttpPost]
        [Route("RevokeApproval")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RevokeApproval([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController RevokeApproval");

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

                var response = (RevokeApprovalResponse)results[BaseRequest.RevokeApproval];

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
        // POST: api/Files/GetNodeKey
        [HttpPost]
        [Route("GetNodeKey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetNodeKey([FromBody] ShardsPacketDto shardsPacketDto)
        {
            try
            {
                _logger.LogInformation("FilesController GetNodeKey");

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

                var response = (GetNodeKeyResponse)results[BaseRequest.GetNodeKey];

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
