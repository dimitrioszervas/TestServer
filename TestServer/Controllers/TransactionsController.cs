using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PeterO.Cbor;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks.Dataflow;
using TestServer.Contracts;
using TestServer.Dtos.Server;
using TestServer.ReedSolomon;
using TestServer.Server;
using TestServer.Server.Requests;
using TestServer.Server.Responses;



namespace TestServer.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private const string TRANSACTIONS_RECEIVE_SHARD_END_POINT = "api/Transactions/receive-shard";

        private readonly ILogger<FilesController> _logger;
        private readonly IMapper _mapper;
        //private readonly IServerService _serverService;

        static ConcurrentDictionary<Guid, ShardsPacketConsumer> _transactions = new ConcurrentDictionary<Guid, ShardsPacketConsumer>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="mapper"></param>
        /// <param name="serverService"></param>
        public TransactionsController(ILogger<FilesController> logger, IMapper mapper)//, IServerService serverService)
        {
            _logger = logger;
            _mapper = mapper;
            //_serverService = serverService;
        }


        /// <summary>
        /// Replicates received metadata shards (sends shards to the other servers).
        /// </summary>
        /// <param name="shardsPacket"></param>
        //private void ReplicateMetadataShards(ShardsPacket shardsPacket)
        private void ReplicateMetadataShards(byte[] requestBytes)
        {
            /*
            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }
            */
            try
            {
                Servers.Instance.ReplicateMetadataShards(requestBytes, TRANSACTIONS_RECEIVE_SHARD_END_POINT);
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
        //public async Task<ActionResult> ReceiveShardsPacketFromOtherServer([FromBody] ShardsPacketDto shardsPacketDto)
        public async Task<ActionResult> ReceiveShardsPacketFromOtherServer([FromBody] byte[] requestBytes)
        {
            /*
            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }
            */

            CBORObject requestCBOR = CBORObject.DecodeFromBytes(requestBytes);
          
            var shardsPacket = _mapper.Map<ShardsPacket>(requestCBOR.ToJSONString());

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

        // Extracts the shards from the JSON string an puts the to a 2D byte array (matrix)
        // needed for rebuilding the data using Reed-Solomon.
        private static byte[][] GetShardsFromCBOR(byte[] shardsCBORBytes, ref byte[] src, bool useLogins)
        {
            CBORObject shardsCBOR = CBORObject.DecodeFromBytes(shardsCBORBytes);

            // allocate memory for the data shards byte matrix
            // Last element in the string array is not a shard but the loginID array 
            int numShards = shardsCBOR.Values.Count - 1;
            int numShardsPerServer = numShards / Servers.NUM_SERVERS;

            src = shardsCBOR[shardsCBOR.Values.Count - 1].GetByteString();

            List<byte[]> encrypts = !useLogins ? KeyStore.Inst.GetENCRYPTS(src) : KeyStore.Inst.GetLOGINS(src);

            byte[][] dataShards = new byte[numShards][];
            for (int i = 0; i < numShards; i++)
            {
                // we start inviteENCRYPTS[1] we don't use inviteENCRYPTS[0]
                // we may have more than on shard per server 
                int encryptsIndex = i / numShardsPerServer + 1;

                byte[] encryptedShard = shardsCBOR[i].GetByteString();

                // decrypt string array                
                byte[] shardBytes = CryptoUtils.Decrypt(encryptedShard, encrypts[encryptsIndex], src);

                //Console.WriteLine($"Encrypts Index: {encryptsIndex}");

                // copy shard to shard matrix
                dataShards[i] = new byte[shardBytes.Length];
                Array.Copy(shardBytes, dataShards[i], shardBytes.Length);
            }

            return dataShards;
        }

        public static string GetTransactionFromCBOR(byte[] requestBytes, ref byte[] src, bool useLogins)
        {
            // Decode request's CBOR bytes  
            CBORObject requestCBOR = CBORObject.DecodeFromBytes(requestBytes);

            byte[] transanctionShardsCBORBytes = requestCBOR[0].GetByteString();
            byte[] hmacResultBytes = requestCBOR[1].GetByteString();

            byte[][] transactionShards = GetShardsFromCBOR(transanctionShardsCBORBytes, ref src, useLogins);

            List<byte[]> signs = !useLogins ? KeyStore.Inst.GetSIGNS(src) : KeyStore.Inst.GetLOGINS(src);

            bool verified = CryptoUtils.HashIsValid(signs[0], transanctionShardsCBORBytes, hmacResultBytes);

            Console.WriteLine($"CBOR Shard Data Verified: {verified}");

            // Extract the shards from shards CBOR and put them in byte matrix (2D array of bytes).

            byte[] cborTransactionBytes = ReedSolomonUtils.RebuildDataUsingReeedSolomon(transactionShards);

            CBORObject rebuiltTransactionCBOR = CBORObject.DecodeFromBytes(cborTransactionBytes);

            string rebuiltDataJSON = rebuiltTransactionCBOR.ToJSONString();

            return rebuiltDataJSON;
        }

        private static HttpResponseMessage ReturnBytes(byte[] bytes)
        {
            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new ByteArrayContent(bytes);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            Console.WriteLine();
            Console.WriteLine(bytes.Length);
            Console.WriteLine(CryptoUtils.ByteArrayToStringDebug(result.Content.ReadAsByteArrayAsync().Result));

            return result;
        }
      
        // Invite endpoint
        [HttpPost]
        [Route("Invite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Invite(byte[] requestBytes)
        //public async Task<HttpResponseMessage> Invite()
        {
            /*
            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }
            */

            //servers receive + validate the invite transaction

            try
            {
                _logger.LogInformation("FilesController CreateFolder");

                CBORObject requestCBOR = CBORObject.DecodeFromBytes(requestBytes);

                var shardsPacket = _mapper.Map<ShardsPacket>(requestCBOR.ToJSONString());

                if (!_transactions.ContainsKey(shardsPacket.SessionId))
                {
                    _transactions.TryAdd(shardsPacket.SessionId, new ShardsPacketConsumer());
                }

                var consumerTask = _transactions[shardsPacket.SessionId].ConsumeAsync(_transactions[shardsPacket.SessionId].Buffer, BaseRequest.Invite);
                _transactions[shardsPacket.SessionId].Buffer.Post(shardsPacket);
                ReplicateMetadataShards(requestBytes);
                var results = await consumerTask;
                ShardsPacketConsumer consumer;
                _transactions.TryRemove(shardsPacket.SessionId, out consumer);

                if (results.Count == 0)
                {
                    return StatusCode(400);
                }
              
                string jsonString = results[BaseRequest.Invite];

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

        // Register endpoint
        [HttpPost]
        [Route("Register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Register()
        {
            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }

            // Decode request's CBOR bytes   
            byte[] src = new byte[CryptoUtils.SRC_SIZE_8];
            string rebuiltDataJSON = GetTransactionFromCBOR(requestBytes, ref src, false);
            Console.WriteLine("Register:");
            Console.WriteLine($"Rebuilt Data: {rebuiltDataJSON} ");
            Console.WriteLine();

            RegisterRequest transactionObj =
               JsonConvert.DeserializeObject<RegisterRequest>(rebuiltDataJSON);

            byte[] NONCE = CryptoUtils.CBORBinaryStringToBytes(transactionObj.NONCE);
            byte[] wTOKEN = CryptoUtils.CBORBinaryStringToBytes(transactionObj.wTOKEN);
            byte[] deviceID = CryptoUtils.CBORBinaryStringToBytes(transactionObj.deviceID);

            // servers create SE[] = create ECDH key pair        
            List<byte[]> SE_PUB = new List<byte[]>();
            List<byte[]> SE_PRIV = new List<byte[]>();
            for (int n = 0; n <= Servers.NUM_SERVERS; n++)
            {
                var keyPairECDH = CryptoUtils.CreateECDH();
                SE_PUB.Add(CryptoUtils.ConverCngKeyBlobToRaw(keyPairECDH.PublicKey));
                SE_PRIV.Add(keyPairECDH.PrivateKey);
            }

            // servers store wTOKEN + NONCE                    
            KeyStore.Inst.StoreNONCE(deviceID, NONCE);
            KeyStore.Inst.StoreWTOKEN(deviceID, wTOKEN);


            // server response is ok
            var cbor = CBORObject.NewMap().Add("REGISTER", "SUCCESS");

            return Ok(cbor.EncodeToBytes());
        }

        // Register endpoint
        [HttpPost]
        [Route("Rekey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Rekey()
        {
            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }

            // Decode request's CBOR bytes   
            byte[] deviceID = new byte[CryptoUtils.SRC_SIZE_8];
            string rebuiltDataJSON = GetTransactionFromCBOR(requestBytes, ref deviceID, false);
            Console.WriteLine("Rekey:");
            Console.WriteLine($"Rebuilt Data: {rebuiltDataJSON} ");
            Console.WriteLine();

            RekeyRequest transactionObj =
               JsonConvert.DeserializeObject<RekeyRequest>(rebuiltDataJSON);

            byte[] DS_PUB = CryptoUtils.CBORBinaryStringToBytes(transactionObj.DS_PUB);
            byte[] DE_PUB = CryptoUtils.CBORBinaryStringToBytes(transactionObj.DE_PUB);
            byte[] NONCE = CryptoUtils.CBORBinaryStringToBytes(transactionObj.NONCE);

            // servers create SE[] = create ECDH key pair        
            List<byte[]> SE_PUB = new List<byte[]>();
            List<byte[]> SE_PRIV = new List<byte[]>();
            for (int n = 0; n <= Servers.NUM_SERVERS; n++)
            {
                var keyPairECDH = CryptoUtils.CreateECDH();
                SE_PUB.Add(CryptoUtils.ConverCngKeyBlobToRaw(keyPairECDH.PublicKey));
                SE_PRIV.Add(keyPairECDH.PrivateKey);
            }

            // servers unwrap wKEYS using NONCE + store KEYS
            List<byte[]> ENCRYPTS = new List<byte[]>();
            List<byte[]> SIGNS = new List<byte[]>();
            byte[] oldNONCE = KeyStore.Inst.GetNONCE(deviceID);
            for (int n = 0; n < CryptoUtils.NUM_SIGNS_OR_ENCRYPTS; n++)
            {
                byte[] wENCRYPT = CryptoUtils.CBORBinaryStringToBytes(transactionObj.wENCRYPTS[n]);
                byte[] unwrapENCRYPT = CryptoUtils.Unwrap(wENCRYPT, oldNONCE);
                ENCRYPTS.Add(unwrapENCRYPT);

                byte[] wSIGN = CryptoUtils.CBORBinaryStringToBytes(transactionObj.wSIGNS[n]);
                byte[] unwrapSIGN = CryptoUtils.Unwrap(wSIGN, oldNONCE);
                SIGNS.Add(unwrapSIGN);
            }
            KeyStore.Inst.StoreENCRYPTS(deviceID, ENCRYPTS);
            KeyStore.Inst.StoreSIGNS(deviceID, SIGNS);

            // servers store DS PUB + NONCE
            KeyStore.Inst.StoreDS_PUB(deviceID, DS_PUB);
            KeyStore.Inst.StoreNONCE(deviceID, NONCE);

            // servers foreach (n > 0),  store LOGINS[n] = ECDH.derive (SE.PRIV[n], DE.PUB) for device.id
            List<byte[]> LOGINS = new List<byte[]>();
            for (int n = 0; n <= Servers.NUM_SERVERS; n++)
            {
                byte[] derived = CryptoUtils.ECDHDerive(SE_PRIV[n], DE_PUB);
                LOGINS.Add(derived);
            }
            KeyStore.Inst.StoreLOGINS(deviceID, LOGINS);

            byte[] wTOKEN = KeyStore.Inst.GetWTOKEN(deviceID);

            //  response is wTOKEN, SE.PUB[] 
            var cbor = CBORObject.NewMap()
                .Add("wTOKEN", wTOKEN)
                .Add("SE_PUB", SE_PUB);

            return Ok(cbor.EncodeToBytes());
        }


        // Login endpoint
        [HttpPost]
        [Route("Login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Login()
        {
            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }

            // Decode request's CBOR bytes
            // servers receive + validate the login transaction
            byte[] deviceID = new byte[CryptoUtils.SRC_SIZE_8];
            string rebuiltDataJSON = GetTransactionFromCBOR(requestBytes, ref deviceID, true);
            Console.WriteLine("Login");
            Console.WriteLine($"Rebuilt Data: {rebuiltDataJSON} ");
            Console.WriteLine();

            LoginRequest transactionObj =
               JsonConvert.DeserializeObject<LoginRequest>(rebuiltDataJSON);

            // servers get LOGINS[] for device
            // servers SIGNS[] = ENCRYPTS[] = LOGINS[]                
            List<byte[]> LOGINS = KeyStore.Inst.GetLOGINS(deviceID);

            // servers unwrap + store wSIGNS + wENCRPTS using stored NONCE for device.
            List<byte[]> ENCRYPTS = new List<byte[]>();
            List<byte[]> SIGNS = new List<byte[]>();
            byte[] NONCE = CryptoUtils.CBORBinaryStringToBytes(transactionObj.NONCE);// KeyStore.Inst.GetNONCE(deviceID);
            for (int n = 0; n < CryptoUtils.NUM_SIGNS_OR_ENCRYPTS; n++)
            {
                byte[] wENCRYPT = CryptoUtils.CBORBinaryStringToBytes(transactionObj.wENCRYPTS[n]);
                byte[] unwrapENCRYPT = CryptoUtils.Unwrap(wENCRYPT, NONCE);
                ENCRYPTS.Add(unwrapENCRYPT);

                byte[] wSIGN = CryptoUtils.CBORBinaryStringToBytes(transactionObj.wSIGNS[n]);
                byte[] unwrapSIGN = CryptoUtils.Unwrap(wSIGN, NONCE);
                SIGNS.Add(unwrapSIGN);
            }
            KeyStore.Inst.StoreENCRYPTS(deviceID, ENCRYPTS);
            KeyStore.Inst.StoreSIGNS(deviceID, SIGNS);

            // servers store NONCE? 
            // KeyStore.Inst.StoreNONCE(deviceID, CryptoUtils.CBORBinaryStringToBytes(transactionObj.NONCE)); 

            // servers response = wTOKEN   
            var cbor = CBORObject.NewMap().Add("wTOKEN", KeyStore.Inst.GetWTOKEN(deviceID));

            return Ok(cbor.EncodeToBytes());
        }

        // Session endpoint
        [HttpPost]
        [Route("Session")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Session()
        {
            byte[] requestBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                requestBytes = ms.ToArray();
            }

            // Decode request's CBOR bytes
            // servers receive + validate the login transaction
            byte[] deviceID = new byte[CryptoUtils.SRC_SIZE_8];
            string rebuiltDataJSON = GetTransactionFromCBOR(requestBytes, ref deviceID, false);
            Console.WriteLine("Session");
            Console.WriteLine($"Rebuilt Data: {rebuiltDataJSON} ");
            Console.WriteLine();

            SessionRequest transactionObj =
               JsonConvert.DeserializeObject<SessionRequest>(rebuiltDataJSON);


            // servers response = Ok   
            var cbor = CBORObject.NewMap().Add("MSG", transactionObj.MSG);

            return Ok(cbor.EncodeToBytes());
        }
    }
}
