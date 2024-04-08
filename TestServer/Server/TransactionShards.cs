using Newtonsoft.Json;
using System.Text;

namespace TestServer.Server
{
    /// <summary>
    /// Rebuilds a transaction from received transaction shards.
    /// </summary>    
    public sealed class TransactionShards
    {

        private byte[][] shards;
        private bool[] shardsPresent;

        private int nDataShards;
        private int nTotalShards;
        private int nParityShards;
        private int shardLength;

        private int nReceivedShards;

        private bool reconstructed;
    
        private byte[] transactionBytes;    
     
        private List<long> serversModifiedTime = new List<long>();

        private long timestamp;               
       

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shardPacket"></param>
        public TransactionShards(ShardsPacket shardPacket)
        {
            try
            {
                this.nReceivedShards = 0;

                this.reconstructed = false;
              
                this.shards = new byte[shardPacket.NumTotalShards][];
               
                this.shardsPresent = new bool[shardPacket.NumTotalShards];

                this.nDataShards = shardPacket.NumDataShards;
                this.nParityShards = shardPacket.NumParityShards;
                this.nTotalShards = shardPacket.NumTotalShards;
             
                this.timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

        }

        /// <summary>
        /// Gets the rebuilt transtcion's bytes.
        /// </summary>
        /// <returns>byte array</returns>
        public byte [] GetRebuiltTransactionBytes()
        {
            return this.transactionBytes;
        }

        /// <summary>
        /// Adds modified time.
        /// </summary>
        /// <param name="t"></param>
        public void AddModifiedtime(long t)
        {
            this.serversModifiedTime.Add(t);
        }

        /*
        public bool AllModifiedTimesReceived()
        {
            return this.serversModifiedTime.Count() == ServerService.NUMBER_OF_SERVERS;
        }
        */
        /// <summary>
        /// Get received modified times.
        /// </summary>
        /// <returns>ListFiles<long></returns>
        public List<long> GetServersModifiedTime()
        {
            return this.serversModifiedTime;
        }

        /// <summary>
        /// Is shard present.
        /// </summary>
        /// <param name="i"></param>
        /// <returns>bool</returns>
        public bool IsShardPresent(int i)
        {
            return this.shardsPresent[i];
        }

        /// <summary>
        /// Get current number of received shards.
        /// </summary>
        /// <returns>int</returns>
        public int GetNumReceivedShards()
        {
            return this.nReceivedShards;
        }

      
        public void SetShard(int shardNo, byte[] encryptedShard, byte[] src, byte [] hmacResult, bool useLogins)
        {
            try
            {
                if (!this.shardsPresent[shardNo])
                {
                    int numShards = nTotalShards;
                    int numShardsPerServer = numShards / Servers.NUM_SERVERS;
                    int keyIndex = (shardNo / numShardsPerServer) + 1;

                    List<byte[]> encrypts = !useLogins ? KeyStore.Inst.GetENCRYPTS(src) : KeyStore.Inst.GetREKEYS(src);

                    // decrypt shard                
                    byte[] shard = CryptoUtils.Decrypt(encryptedShard, encrypts[keyIndex], src);

                    //List<byte[]> signs = !useLogins ? KeyStore.Inst.GetSIGNS(src) : KeyStore.Inst.GetREKEYS(src);
                    //bool verified = CryptoUtils.HashIsValid(signs[keyIndex], shard, hmacResult);
                    //Console.WriteLine($"Shard No {shardNo} Verified: {verified}");

                    this.shards[shardNo] = new byte[shard.Length];
                    Array.Copy(shard, this.shards[shardNo], shard.Length);
                    this.shardsPresent[shardNo] = true;
                    this.nReceivedShards++;

                    this.shardLength = shard.Length;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets timestamp.
        /// </summary>
        /// <returns>long</returns>
        public long GetTimestamp()
        {
            return this.timestamp;
        } 

        /// <summary>
        /// Checks is data are reconstructed.
        /// </summary>
        /// <returns>bool</returns>
        public bool AreReconstructed()
        {
            return this.reconstructed;
        }
           
        /// <summary>
        /// Checks if enough shards are received.
        /// </summary>
        /// <returns>bool</returns>
        public bool AreEnoughShards()
        {
            return this.nReceivedShards >= this.nDataShards;
        }

        /// <summary>
        /// Strips padding from data. 
        /// </summary>
        /// <param name="paddedData"></param>
        /// <returns>a byte array</returns>
        private byte[] StripPadding(byte[] paddedData)
        {
            try
            {
                int padding = 1;
                for (int i = paddedData.Length - 1; i >= 0; i--)
                {
                    if (paddedData[i] == 0)
                    {
                        padding++;
                    }
                    else
                    {
                        break;
                    }
                }

                byte[] strippedData = new byte[paddedData.Length - padding];
                Array.Copy(paddedData, 0, strippedData, 0, strippedData.Length);

                return strippedData;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        /// Rebuild transaction using Reed-Solomon.
        /// </summary>
        public void RebuildTransactionUsingReedSolomon()
        {
            try
            {
                // allocated memory for the missing shards
                for (int i = 0; i < shards.Length; i++)
                {
                    if (shards[i] == null)
                    {
                        shards[i] = new byte[shardLength];
                    }
                }

                // Replicate the other shards using Reeed-Solomom.
                var reedSolomon = new ReedSolomon.ReedSolomon(this.shardsPresent.Length - this.nParityShards, this.nParityShards);
                reedSolomon.DecodeMissing(this.shards, this.shardsPresent, 0, shardLength);

                // Write the Reed-Solomon matrix of shards to a 1D array of bytes
                byte [] buffer = new byte[this.shards.Length * shardLength];
                int offSet = 0;

                for (int j = 0; j < this.shards.Length - this.nParityShards; j++)
                {
                    Array.Copy(this.shards[j], 0, buffer, offSet, shardLength);
                    offSet += shardLength;
                }

                // Remove padding
                this.transactionBytes = StripPadding(buffer);            
  
                // Set that the transaction assembled to true
                this.reconstructed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the shard length.
        /// </summary>
        /// <returns>int</returns>
        public int GetShardLength()
        {
            return this.shardLength;
        }

        /// <summary>
        /// Gets number of total shards.
        /// </summary>
        /// <returns></returns>
        public int GetNumTotalShards()
        {
            return this.nTotalShards;
        }

        /// <summary>
        /// Gets number of data shards.
        /// </summary>
        /// <returns></returns>
        public int GetNumDataShards()
        {
            return this.nDataShards;
        }
      
    }
}

