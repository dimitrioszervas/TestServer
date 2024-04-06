using TestServer.ReedSolomon;
using Serilog;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
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

        // Folder for storing the blockchains files on disk 
        public const string BLOCKCHAINS_DIR = "blockchains";

        public const string PUBLIC_BLOCKCHAIN_FILENAME = "public_blockchain";

        // Current server 0, 1, 2, ... n
        public int CurrentServer;

        // Other servers as HTTP clients
        private HttpClient[] HttpClients;
        private MediaTypeWithQualityHeaderValue Header = new MediaTypeWithQualityHeaderValue("application/octet-stream");

        // Private thread safe Dictionary/HashMap that stores the private blockchains using the org GUID they
        // belong as a key/index
        private ConcurrentDictionary<string, Blockchain> Blockchains = new ConcurrentDictionary<string, Blockchain>();

        // Public blockchain
        private Blockchain PublicBlockchain;

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


        /// <summary>
        /// Gets the private blockchain.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Blockchain</returns>
        public Blockchain GetPrivateBlockchain(string id)
        {
            try
            {
                return Blockchains[id];
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetPrivateBlockchain: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Inserts a transaction to the public blockchain.
        /// </summary>
        /// <param name="transactionBytes"></param>
        public void InsertToPublicBlockchain(byte[] transactionBytes)
        {
            try { 
                PublicBlockchain.Add(transactionBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"InsertToPublicBlockchain: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Gets the public blockchain.
        /// </summary>
        /// <returns>Blockchain</returns>
        public Blockchain GetPublicBlockchain()
        {
            try { 
                return PublicBlockchain;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetPrivateBlockchain: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Inserts a transaction to the private blockchain.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transactionBytes"></param>
        public void InsertToPrivateBlockchain(string id, byte[] transactionBytes)
        {
            try { 
                Blockchains[id].Add(transactionBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"InsertToPrivateBlockchain: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Creates the required folder used by the server (for storing tempory files, blockchains, etc).
        /// </summary>
        public void CreateRequiredFolders()
        {
            if (!Directory.Exists(TMP_STORAGE_DIR))
            {
                Directory.CreateDirectory(TMP_STORAGE_DIR);
            }

            char separator = Path.DirectorySeparatorChar;

            if (!Directory.Exists(BLOCKCHAINS_DIR))
            {
                Directory.CreateDirectory(BLOCKCHAINS_DIR);

                // Create public blockchain
                PublicBlockchain = new Blockchain(BLOCKCHAINS_DIR + separator + PUBLIC_BLOCKCHAIN_FILENAME, Guid.NewGuid().ToString());
            }
            else
            {
                // If public blockchain does not exists create it
                if (!File.Exists(BLOCKCHAINS_DIR + separator + PUBLIC_BLOCKCHAIN_FILENAME))
                {
                    // Create public blockchain
                    PublicBlockchain = new Blockchain(BLOCKCHAINS_DIR + separator + PUBLIC_BLOCKCHAIN_FILENAME, Guid.NewGuid().ToString());

                }
            }
        }

        /// <summary>
        /// Imports public & private blockchains.
        /// </summary>
        /// <returns></returns>
        public bool ImportOrgBlockchains()
        {

            if (!Directory.Exists(BLOCKCHAINS_DIR))
            {
                return false;
            }

            string[] listOfFiles = Directory.GetFiles(BLOCKCHAINS_DIR);

            char separator = Path.DirectorySeparatorChar;

            foreach (string filePath in listOfFiles)
            {
                
                if (filePath.Equals(BLOCKCHAINS_DIR + separator + PUBLIC_BLOCKCHAIN_FILENAME))
                {
                    PublicBlockchain = new Blockchain(filePath);
                    continue;
                }

                // Ignore blockchain index files
                if (filePath.EndsWith(Blockchain.INDEX_FILE)) {
                    continue;
                }                

                Blockchain orgBlockchain = new Blockchain(filePath);
                Blockchains.TryAdd(Path.GetFileName(filePath), orgBlockchain);
            }
        
            return true;
        }

        /// <summary>
        /// Creates private blockchain. 
        /// </summary>
        /// <param name="orgGUID"></param>
        /// <param name="blockchainGUID"></param>
        public void CreatePrivateBlockchain(string orgGUID, string blockchainGUID)
        {
            try { 
                char separator = Path.DirectorySeparatorChar;
                Blockchains.TryAdd(orgGUID, new Blockchain(BLOCKCHAINS_DIR + separator + orgGUID, blockchainGUID));
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetPrivateBlockchain: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Loads settings files containig the other servers addresses.
        /// </summary>
        /// <returns>bool/returns>
        public bool LoadSettings()
        {
            const string SETTINGS_FILE = "settings.txt";
            List<string> hosts = new List<string>();
            try
            {
                string[] lines = System.IO.File.ReadAllLines(@SETTINGS_FILE);

                int lineNo = 1;

                foreach (string line in lines)
                {
                    string[] fields = line.Split(" ");
                    if (lineNo == 1)
                    {
                        CurrentServer = int.Parse(fields[0]);
                    }
                    else
                    {
                        string host = fields[1];
                        hosts.Add(host);
                    }

                    lineNo++;
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"LoadSettings: {ex}");
                return false;
            }

            HttpClients = new HttpClient[hosts.Count];

            for (int i = 0; i < hosts.Count; i++)
            {
                try
                {
                    HttpClients[i] = new()
                    {
                        BaseAddress = new Uri("http://" + hosts[i]),
                    };
                    HttpClients[i].Timeout = TimeSpan.FromSeconds(1);
                }
                catch (Exception ex)
                {
                    _logger.LogError($" {ex}");;
                    return false;
                }
            } // end for

            _logger.LogInformation("HttpClients Initialised!");

            return true;
        }

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
                _logger.LogError($"LoadSettings1: {ex}");;
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
                _logger.LogError($"LoadSettings1: {ex}");;
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
                _logger.LogError($"LoadSettings2: {ex}");;
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
                _logger.LogError($"LoadSettings2: {ex}");;
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

        /// <summary>
        /// Downloads files shards from cloud.
        /// </summary>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public ShardsPacket DownloadFileShards(string versionId)
        {
            try {
                lock (this)
                {
                    char separator = Path.DirectorySeparatorChar;
                    string tmpFileToBeUploadedPath = TMP_STORAGE_DIR + separator + versionId;

                    // To be implemented...
                    // download file from Cloud to the TMP_STORAGE_DIR folder



                    ShardsPacket shardsPacket = new ShardsPacket();

                    using (FileStream fs = File.OpenRead(tmpFileToBeUploadedPath))
                    {
                        using (BinaryReader reader = new BinaryReader(fs))
                        {
                            shardsPacket.NumTotalShards = reader.ReadInt32();
                            shardsPacket.NumDataShards = reader.ReadInt32();
                            shardsPacket.NumParityShards = reader.ReadInt32();
                            shardsPacket.DataShardLength = reader.ReadInt32();

                            shardsPacket.NumShardsPerServer = reader.ReadInt32();
                            for (int i = 0; i < shardsPacket.NumShardsPerServer; i++)
                            {
                                int shardNo = reader.ReadInt32();
                                shardsPacket.ShardNo.Add(shardNo);

                                byte[] shard = reader.ReadBytes(shardsPacket.DataShardLength);
                                shardsPacket.DataShards.Add(shard);
                            }
                            reader.Close();
                        }
                        fs.Close();
                    }

                    // Delete tmp file after download from cloud
                    //File.Delete(tmpFileToBeUploadedPath); 

                    return shardsPacket;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"DownloadFileShards: {ex}");
                throw;
            }
        }
    }
}
