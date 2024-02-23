namespace TestServer.Dtos.Server
{
    public class ShardsPacketDto
    {
        public Guid SessionId { get; set; }
        public List<int> ShardNo { get; set; }
        public int NumShardsPerServer { get; set; }
        public int NumTotalShards { get; set; }
        public int NumDataShards { get; set; }
        public int NumParityShards { get; set; }
        public int MetadataShardLength { get; set; }
        public int DataShardLength { get; set; }
        public List<byte[]>? MetadataShards { get; set; }
        public List<byte[]>? DataShards { get; set; }
    }
}
