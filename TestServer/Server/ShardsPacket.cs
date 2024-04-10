﻿using Amazon.DynamoDBv2.Model;
using NuGet.Packaging.Signing;

namespace TestServer.Server
{
    public sealed class ShardsPacket
    {
        public Guid SessionId { get; set; }
        public List<int> ShardNo { get; set; } = new List<int>();
        public int NumShardsPerServer { get; set; }
        public int NumTotalShards { get; set; }
        public int NumDataShards { get; set; }
        public int NumParityShards { get; set; }
        public int MetadataShardLength { get; set; }
        public int DataShardLength { get; set; }
        public List<byte[]> MetadataShards { get; set; } = new List<byte[]>();
        public List<byte[]> DataShards { get; set; } = new List<byte[]>();
        public byte[] SRC { get; set; }
        public byte[] hmacResult { get; set; }

        public void AddMetadataShard(byte[] metadataShard)
        {
            MetadataShards.Add(metadataShard);
        }

        public void AddDataShard(byte[] dataShard)
        {
            DataShards.Add(dataShard);
        }

        public void AddShardNo(int shardNo)
        {
            ShardNo.Add(shardNo);
        }

        public void Decrypt(bool useRekeys)
        {
            int numShards = this.NumTotalShards;
            int numShardsPerServer = numShards / Servers.NUM_SERVERS;

            for (int i = 0; i < ShardNo.Count; i++)
            {
                int keyIndex = !useRekeys ? (ShardNo[i] / numShardsPerServer) + 1 : 0;

                byte[] encrypt = !useRekeys ? KeyStore.Inst.GetENCRYPTS(SRC)[keyIndex] : KeyStore.Inst.GetPreKEY(SRC);

                byte[] encryptedShard = MetadataShards[i];

                // decrypt shard                
                byte[] shard = CryptoUtils.Decrypt(encryptedShard, encrypt, SRC);

                this.MetadataShards[i] = shard;
                this.DataShardLength = shard.Length;

            }
        }
    }
}
