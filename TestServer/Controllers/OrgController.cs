using AutoMapper;
using TestServer.Contracts;
using TestServer.Dtos.Server;
using TestServer.Server;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace TestServer.Controllers
{
    /// <summary>
    /// Gets organasation details.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OrgController : ControllerBase
    {
        private const string ORG_RECEIVE_SHARD_END_POINT = "api/Org/receive-shard";

        private readonly ILogger<OrgController> _logger;
        private readonly IMapper _mapper;
        private readonly IServerService _serverService;

        static ConcurrentDictionary<Guid, ShardsPacketConsumer> _transactions = new ConcurrentDictionary<Guid, ShardsPacketConsumer>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="mapper"></param>
        /// <param name="serverService"></param>
        public OrgController(ILogger<OrgController> logger, IMapper mapper, IServerService serverService)
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
                Servers.Instance.ReplicateMetadataShards(shardsPacket, ORG_RECEIVE_SHARD_END_POINT);
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
        // POST: api/Org/receive-shard
        [HttpPost]
        [Route("receive-shard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ReceiveShardsPacketFromOtherServer([FromBody] ShardsPacketDto shardsPacketDto)
        {
            var shardsPacket = _mapper.Map<ShardsPacket>(shardsPacketDto);

            if (_transactions.ContainsKey(shardsPacketDto.SessionId))
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

    }
}
