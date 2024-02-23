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
                for (var row = 0; row < shardPacket.NumTotalShards; row++)
                {
                    this.shards[row] = new byte[shardPacket.MetadataShardLength];
                }
                this.shardsPresent = new bool[shardPacket.NumTotalShards];

                this.nDataShards = shardPacket.NumDataShards;
                this.nParityShards = shardPacket.NumParityShards;
                this.nTotalShards = shardPacket.NumTotalShards;
                this.shardLength = shardPacket.MetadataShardLength;

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

        /// <summary>
        /// Set a shard to its corresponding index in the shards matrix. 
        /// </summary>
        /// <param name="shardNo"></param>
        /// <param name="shardIn"></param>
        public void SetShard(int shardNo, byte[] shardIn)
        {
            try
            {
                Array.Copy(shardIn, 0, this.shards[shardNo], 0, this.shardLength);
                this.shardsPresent[shardNo] = true;
                this.nReceivedShards++;
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
                // Replicate the other shards using Reeed-Solomom.
                var reedSolomon = new ReedSolomon.ReedSolomon(this.shardsPresent.Length - this.nParityShards, this.nParityShards);
                reedSolomon.DecodeMissing(this.shards, this.shardsPresent, 0, this.shardLength);

                // Write the Reed-Solomon matrix of shards to a 1D array of bytes
                byte [] buffer = new byte[this.shards.Length * this.shardLength];
                int offSet = 0;

                for (int j = 0; j < this.shards.Length - this.nParityShards; j++)
                {
                    Array.Copy(this.shards[j], 0, buffer, offSet, this.shardLength);
                    offSet += this.shardLength;
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

