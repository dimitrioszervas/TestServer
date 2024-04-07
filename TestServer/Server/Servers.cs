using TestServer.ReedSolomon;
using Serilog;
using System.Net.Http.Headers;

namespace TestServer.Server
{
    // Singleton class
    /// <summary>
    /// Singleton class that deals with communicating with the other servers and the client (api layer)
    /// </summary>
    public sealed class Servers
    {
        public const int NUM_SERVERS = 3;

        // Folder used to store uploaded files/versions before uploading them to a cloud (AWS for example) 
        public const string TMP_STORAGE_DIR = "TMP_STORAGE";

     
        // Current server 0, 1, 2, ... n
        public int CurrentServer;

        // Other servers as HTTP clients
        private HttpClient[] HttpClients;
        private MediaTypeWithQualityHeaderValue Header = new MediaTypeWithQualityHeaderValue("application/octet-stream");
           
        // This is required to make the Servers singleton class thread safe
        private static readonly Lazy<Servers> lazy = new Lazy<Servers>(() => new Servers());

        public HttpClient[] GetHttpClients() { return HttpClients; }

        ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddSerilog());
        ILogger<Servers> _logger;

        public Servers()
        {
            _logger = factory.CreateLogger<Servers>();
        }

        // Static instance of the Servers class
        public static Servers Instance { get { return lazy.Value; } }

        public bool LoadSettings1()
        {
            CurrentServer = 0;
            HttpClients = new HttpClient[2];
                       
            try
            {
                HttpClients[0] = new()
                {
                    BaseAddress = new Uri("http://localhost:5099"),//new Uri("http://poc.sealstone:5276"),
                };
                HttpClients[0].Timeout = TimeSpan.FromSeconds(1);
                HttpClients[0].DefaultRequestHeaders.Accept.Add(Header);
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoadSettings1: {ex}");
                return false;
            }

            try
            {
                HttpClients[1] = new()
                {
                    BaseAddress = new Uri("http://localhost:5200"), // new Uri("http://poc.sealstone:5277"),
                };
                HttpClients[1].Timeout = TimeSpan.FromSeconds(1);
                HttpClients[1].DefaultRequestHeaders.Accept.Add(Header);
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoadSettings1: {ex}");
                return false;
            }

            _logger.LogInformation("HttpClients Initialised!");

            return true;
        }

        public bool LoadSettings2()
        {
            CurrentServer = 1;
            HttpClients = new HttpClient[2];

            try
            {
                HttpClients[0] = new()
                {
                    BaseAddress = new Uri("http://localhost:5110"),// new Uri("http://poc.sealstone:5275"),
                };
                HttpClients[0].Timeout = TimeSpan.FromSeconds(1);
                HttpClients[0].DefaultRequestHeaders.Accept.Add(Header);
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoadSettings2: {ex}");
                return false;
            }

            try
            {
                HttpClients[1] = new()
                {
                    BaseAddress = new Uri("http://localhost:5200"), // new Uri("http://poc.sealstone:5277"),
                };
                HttpClients[1].Timeout = TimeSpan.FromSeconds(1);
                HttpClients[1].DefaultRequestHeaders.Accept.Add(Header);
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoadSettings2: {ex}");
                return false;
            }

            _logger.LogInformation("HttpClients Initialised!");

            return true;
        }

        public bool LoadSettings3()
        {
            CurrentServer = 2;
            HttpClients = new HttpClient[2];

            try
            {
                HttpClients[0] = new()
                {
                    BaseAddress = new Uri("http://localhost:5110"),// new Uri("http://poc.sealstone:5275"),
                };
                HttpClients[0].Timeout = TimeSpan.FromSeconds(1);
                HttpClients[0].DefaultRequestHeaders.Accept.Add(Header);
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoadSettings3: {ex}");;
                return false;
            }

            try
            {
                HttpClients[1] = new()
                {
                    BaseAddress = new Uri("http://localhost:5099"),//new Uri("http://poc.sealstone:5276"),
                };
                HttpClients[1].Timeout = TimeSpan.FromSeconds(1);
                HttpClients[1].DefaultRequestHeaders.Accept.Add(Header);
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoadSettings3: {ex}");;
                return false;
            }

            _logger.LogInformation("HttpClients Initialised!");

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="bytes"></param>
        /// <param name="endPoint"></param>
        private void PostAsync(HttpClient httpClient, byte[] bytes, string endPoint)
        {
            Task.Run(async () =>
            {
                try
                {
                    using (var content = new ByteArrayContent(bytes))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        using HttpResponseMessage response = await httpClient.PostAsync(endPoint, content);
                    }
                }
                catch (Exception)
                {
                    return;
                }
            });
        }            

        public void ReplicateMetadataShards(byte [] bytes, string endPoint)
        {
            foreach (var client in HttpClients)
            {
                PostAsync(client, bytes, endPoint);
            }
        }

        /// <summary>
        /// Gets the coresponding data shard corresponding to a particular server from the data.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>ShardsPacket</returns>
        public ShardsPacket GetShardPacket(byte[] bytes)
        {
            try { 

                int totalNumberOfServers = HttpClients.Length + 1;
                CalculateReedSolomonShards calculateReedSolomonShards = new CalculateReedSolomonShards(bytes, totalNumberOfServers);

                byte[][] transactionShards = calculateReedSolomonShards.Shards;

                // Send shards packet to client

                int currentShard = 0;

                Guid sessionId = Guid.NewGuid();

                ShardsPacket shardsPacket = new ShardsPacket();

                for (int serverNo = 0; serverNo < totalNumberOfServers; serverNo++)
                {
                    if (serverNo == CurrentServer)
                    {
                        shardsPacket.SessionId = sessionId;
                        shardsPacket.NumShardsPerServer = calculateReedSolomonShards.NumShardsPerServer;
                        shardsPacket.NumTotalShards = calculateReedSolomonShards.TotalNShards;
                        shardsPacket.NumDataShards = calculateReedSolomonShards.DataNShards;
                        shardsPacket.NumParityShards = calculateReedSolomonShards.ParityNShards;
                        shardsPacket.DataShardLength = calculateReedSolomonShards.ShardLength;
                    }

                    for (int shardNo = 0; shardNo < calculateReedSolomonShards.NumShardsPerServer; shardNo++)
                    {
                        if (serverNo == CurrentServer)
                        {
                            shardsPacket.AddDataShard(transactionShards[currentShard]);
                            shardsPacket.AddShardNo(currentShard);
                        }
                        // Progress to next msgNo //////////////////////////////////////
                        currentShard++;
                    }

                    if (serverNo == CurrentServer)
                    {
                        break;
                    }

                }

                return shardsPacket;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetShardPacket: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Uploads file shards to cloud.
        /// </summary>
        /// <param name="shardsPacket"></param>
        /// <param name="versionId"></param>
        /// <returns>bool</returns>
        public bool UploadFileShards(ShardsPacket shardsPacket, string versionId)
        {
            try { 

                char separator = Path.DirectorySeparatorChar;
                string tmpFileToBeUploadedPath = TMP_STORAGE_DIR + separator + versionId;

                using (FileStream fs = File.OpenWrite(tmpFileToBeUploadedPath))
                {
                    using (BinaryWriter w = new BinaryWriter(fs))
                    {
                        w.Write(shardsPacket.NumTotalShards);
                        w.Write(shardsPacket.NumDataShards);
                        w.Write(shardsPacket.NumParityShards);
                        w.Write(shardsPacket.DataShardLength);

                        w.Write(shardsPacket.NumShardsPerServer);
                        for (int i = 0; i < shardsPacket.NumShardsPerServer; i++)
                        {
                            w.Write(shardsPacket.ShardNo[i]);
                            w.Write(shardsPacket.DataShards[i], 0, shardsPacket.DataShardLength);
                        }

                        w.Flush();
                        w.Close();
                    }
                    fs.Close();
                }

                // To be implemented...
                // Upload to Cloud
                // Delete tmp file/version after upload to cloud
                //File.Delete(tmpFileToBeUploadedPath); 

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"UploadFileShards: {ex}");
                throw;
            }
        }
    }
}
