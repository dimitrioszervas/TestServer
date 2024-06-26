﻿using TestServer.Server.Requests;
using Newtonsoft.Json;
using Serilog;
using System.Threading.Tasks.Dataflow;
using PeterO.Cbor;
using System.Text;

namespace TestServer.Server
{
    /// <summary>
    /// Consumes shard packets from Producers (the Controllers in our case) and processes requests from clients.
    /// </summary>    
    public sealed class ShardsPacketConsumer
    {
        ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddSerilog());
        ILogger<ShardsPacketConsumer> _logger;
 

        // Buffer that stores received shard packets Posted by the produsers (the Controllers in our case)
        public BufferBlock<ShardsPacket> Buffer = new BufferBlock<ShardsPacket>();

        public ShardsPacketConsumer()
        {
            _logger = factory.CreateLogger<ShardsPacketConsumer>();
        }
      
        public async Task<Dictionary<string, byte[]>> ConsumeAsync(IReceivableSourceBlock<ShardsPacket> source, string requestType)
        {           

            ulong userId;
            ulong orgId;
            ulong deviceId;

            // Stores the received shards and rebuilds a transaction if enough shards are received
            TransactionShards shards = null;

            // Dictionary/HashMap that stores a list of reponses from multiple requests in a transaction
            // using the Request string as an index key.
            Dictionary<string, byte[]> responses = new Dictionary<string, byte[]>();

            int countShards = 0;

            // Wait for input from producer.
            while (await source.OutputAvailableAsync())
            {
                // Get shard packet from producer                 
                while (source.TryReceive(out ShardsPacket shardsPacket))
                {
                    try
                    {
                        // For debug reason count number of received shards and print them to console.
                        countShards++;
                        _logger.LogInformation($"Received Shard {countShards}");

                        // Initialise a TransactionShards class instance if is null when we receive a shard packet
                        if (shards == null)
                        {
                            shards = new TransactionShards(shardsPacket);
                        }                    

                        // Add each shard of the received paket to the TransactionShards class instance for storage
                        for (int i = 0; i < shardsPacket.MetadataShards.Count; i++)
                        { 
                            // Set received shard to appropriate position in the shards matrix (see TransactionSgards class).

                            shards.SetShard(shardsPacket.ShardNo[i], shardsPacket.MetadataShards[i]);

                            // Check if we have enough data shards to rebult the Transaction using Reed-Solomon.
                            if (shards.AreEnoughShards())
                            {
                                // Replicate the other shards using Reeed-Solomom.
                                shards.RebuildTransactionUsingReedSolomon();

                                // Get rebuilt Transaction bytes.
                                byte[] shardsBytes = shards.GetRebuiltTransactionBytes();

                                //var cbor = CBORObject.NewArray().Add(shardsBytes).Add(shardsPacket.SRC);

                                // List<byte[]> signs = !useRekeys ? KeyStore.Inst.GetSIGNS(shardsPacket.SRC) : KeyStore.Inst.GetPreKEY(shardsPacket.SRC);

                                // bool verified = CryptoUtils.HashIsValid(signs[0], cbor.EncodeToBytes(), shardsPacket.hmacResult);

                                //Console.WriteLine($"Shard Verified: {verified}");


                                // Convert Transaction's bytes to a Json string                               
                                CBORObject rebuiltTransactionCBOR = CBORObject.DecodeFromBytes(shards.GetRebuiltTransactionBytes());

                                string shardsJsonString = rebuiltTransactionCBOR.ToJSONString();

                                // Print the Transaction's Json string to console for debug purposes.
                                _logger.LogInformation(shardsJsonString);
                             
                                switch (requestType) {
                                    case BaseRequest.Invite:
                                        {
                                            Console.WriteLine("BaseRequest.Invite Start");
                                            //servers receive + validate the invite transaction

                                            InviteRequest inviteObj = JsonConvert.DeserializeObject<InviteRequest>(shardsJsonString);
                                            
                                            // servers store inviteObj.SIGNS + inviteObj.ENCRYPTS for device.id = inviteObj.id         
                                            byte[] inviteID = CryptoUtils.CBORBinaryStringToBytes(inviteObj.inviteID);

                                            byte[] ownerID = shardsPacket.SRC;

                                            // decrypt invite.ENCRYPTS + invite.SIGNS
                                            List<byte[]> inviteENCRYPTS = new List<byte[]>();
                                            List<byte[]> inviteSIGNS = new List<byte[]>();
                                            // 1st element at posistion 0 in not encrypted
                                            inviteENCRYPTS.Add(CryptoUtils.CBORBinaryStringToBytes(inviteObj.inviteENCRYPTS[0]));
                                            inviteSIGNS.Add(CryptoUtils.CBORBinaryStringToBytes(inviteObj.inviteSIGNS[0]));

                                            int n = Servers.Inst.CurrentServer + 1;

                                            byte[] decryptedInviteENCRYPT = CryptoUtils.Decrypt(
                                                CryptoUtils.CBORBinaryStringToBytes(inviteObj.inviteENCRYPTS[n]),
                                                KeyStore.Inst.GetENCRYPTS(ownerID)[1],
                                                ownerID);

                                            inviteENCRYPTS.Add(decryptedInviteENCRYPT);

                                            byte[] decryptedInviteSIGN = CryptoUtils.Decrypt(
                                                CryptoUtils.CBORBinaryStringToBytes(inviteObj.inviteSIGNS[n]),
                                                KeyStore.Inst.GetENCRYPTS(ownerID)[1],
                                                ownerID);

                                            inviteSIGNS.Add(decryptedInviteSIGN);
                                          
                                            KeyStore.Inst.StoreENCRYPTS(inviteID, inviteENCRYPTS);
                                            KeyStore.Inst.StoreSIGNS(inviteID, inviteSIGNS);

                                            // response is just OK, but any response data must be encrypted + signed using owner.KEYS
                                            var cborInviteResponse = CBORObject.NewMap().Add("INVITE", "SUCCESS");

                                            responses.Add(BaseRequest.Invite, cborInviteResponse.EncodeToBytes());

                                            Console.WriteLine("BaseRequest.Invite END");
                                        }
                                        break;

                                    case BaseRequest.Register:
                                        {
                                            // servers receive + validate the register transaction

                                            RegisterRequest registerObj = JsonConvert.DeserializeObject<RegisterRequest>(shardsJsonString);

                                            byte[] NONCE = CryptoUtils.CBORBinaryStringToBytes(registerObj.NONCE);
                                            byte[] wTOKEN = CryptoUtils.CBORBinaryStringToBytes(registerObj.wTOKEN);
                                            byte[] deviceID = CryptoUtils.CBORBinaryStringToBytes(registerObj.deviceID);

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
                                            var cborRegisterResponse = CBORObject.NewMap().Add("REGISTER", "SUCCESS");

                                            responses.Add(BaseRequest.Register, cborRegisterResponse.EncodeToBytes());
                                        }
                                        break;

                                    case BaseRequest.Rekey:
                                        {
                                            // servers receive + validate the Rekey transaction

                                            RekeyRequest rekeyObj = JsonConvert.DeserializeObject<RekeyRequest>(shardsJsonString);

                                            byte[] DS_PUB = CryptoUtils.CBORBinaryStringToBytes(rekeyObj.DS_PUB);
                                            byte[] DE_PUB = CryptoUtils.CBORBinaryStringToBytes(rekeyObj.DE_PUB);
                                            byte[] NONCE = CryptoUtils.CBORBinaryStringToBytes(rekeyObj.NONCE);

                                            // servers create SE[] = create ECDH key pair   
                                            var keyPairECDH = CryptoUtils.CreateECDH();
                                            byte[] SE_PUB = CryptoUtils.ConverCngKeyBlobToRaw(keyPairECDH.PublicKey);
                                            byte[] SE_PRIV = keyPairECDH.PrivateKey;                                          

                                            byte[] deviceID = shardsPacket.SRC;

                                            // servers unwrap wKEYS using NONCE + store KEYS
                                            List<byte[]> ENCRYPTS = new List<byte[]>();
                                            List<byte[]> SIGNS = new List<byte[]>();
                                            byte[] oldNONCE = KeyStore.Inst.GetNONCE(deviceID);
                                            for (int n = 0; n < Servers.NUM_SERVERS; n++)
                                            {
                                                if (n == 0 || n == (Servers.Inst.CurrentServer + 1))
                                                {
                                                    byte[] wENCRYPT = CryptoUtils.CBORBinaryStringToBytes(rekeyObj.wENCRYPTS[n]);
                                                    byte[] unwrapENCRYPT = CryptoUtils.Unwrap(wENCRYPT, oldNONCE);
                                                    ENCRYPTS.Add(unwrapENCRYPT);

                                                    byte[] wSIGN = CryptoUtils.CBORBinaryStringToBytes(rekeyObj.wSIGNS[n]);
                                                    byte[] unwrapSIGN = CryptoUtils.Unwrap(wSIGN, oldNONCE);
                                                    SIGNS.Add(unwrapSIGN);
                                                }
                                            }
                                            KeyStore.Inst.StoreENCRYPTS(deviceID, ENCRYPTS);
                                            KeyStore.Inst.StoreSIGNS(deviceID, SIGNS);

                                            // servers store DS PUB + NONCE
                                            KeyStore.Inst.StoreDS_PUB(deviceID, DS_PUB);
                                            KeyStore.Inst.StoreNONCE(deviceID, NONCE);

                                            // servers foreach (n > 0),  store preKEY[n] = ECDH.derive (SE.PRIV[n], DE.PUB) for device.id
                                            byte[] preKEY = CryptoUtils.ECDHDerive(SE_PRIV, DE_PUB);
                                           
                                            KeyStore.Inst.StorePreKEY(deviceID, preKEY);

                                            byte[] wTOKEN = KeyStore.Inst.GetWTOKEN(deviceID);

                                            //  response is wTOKEN, SE.PUB[] 
                                            var cborRekeyResponse = CBORObject.NewMap()
                                                .Add("wTOKEN", wTOKEN)
                                                .Add("SE_PUB", SE_PUB);

                                            responses.Add(BaseRequest.Rekey, cborRekeyResponse.EncodeToBytes());
                                        }
                                        break;
                                    case BaseRequest.Login:
                                        {
                                            // servers receive + validate the Login transaction

                                            LoginRequest transactionObj =
                                                JsonConvert.DeserializeObject<LoginRequest>(shardsJsonString);

                                            byte[] deviceID = shardsPacket.SRC;

                                            // servers get preKEY[] for device
                                            // servers SIGNS[] = ENCRYPTS[] = preKEY[]                
                                            byte[] preKEY = KeyStore.Inst.GetPreKEY(deviceID);

                                            // servers unwrap + store wSIGNS + wENCRPTS using stored NONCE for device.
                                            List<byte[]> ENCRYPTS = new List<byte[]>();
                                            List<byte[]> SIGNS = new List<byte[]>();
                                            byte[] NONCE = CryptoUtils.CBORBinaryStringToBytes(transactionObj.NONCE);// KeyStore.Inst.GetNONCE(deviceID);
                                            for (int n = 0; n < CryptoUtils.NUM_SIGNS_OR_ENCRYPTS; n++)
                                            {
                                                if (n == 0 || n == (Servers.Inst.CurrentServer + 1))
                                                {
                                                    byte[] wENCRYPT = CryptoUtils.CBORBinaryStringToBytes(transactionObj.wENCRYPTS[n]);
                                                    byte[] unwrapENCRYPT = CryptoUtils.Unwrap(wENCRYPT, NONCE);
                                                    ENCRYPTS.Add(unwrapENCRYPT);

                                                    byte[] wSIGN = CryptoUtils.CBORBinaryStringToBytes(transactionObj.wSIGNS[n]);
                                                    byte[] unwrapSIGN = CryptoUtils.Unwrap(wSIGN, NONCE);
                                                    SIGNS.Add(unwrapSIGN);
                                                }
                                            }
                                            KeyStore.Inst.StoreENCRYPTS(deviceID, ENCRYPTS);
                                            KeyStore.Inst.StoreSIGNS(deviceID, SIGNS);

                                            // servers store NONCE? 
                                            // KeyStore.Inst.StoreNONCE(deviceID, CryptoUtils.CBORBinaryStringToBytes(transactionObj.NONCE)); 

                                            // servers response = wTOKEN   
                                            var cborLoginResponse = CBORObject.NewMap().Add("wTOKEN", KeyStore.Inst.GetWTOKEN(deviceID));

                                            responses.Add(BaseRequest.Login, cborLoginResponse.EncodeToBytes());
                                        }
                                        break;
                                    case BaseRequest.Session:
                                        {
                                            // servers receive + validate the Session transaction

                                            SessionRequest transactionObj = JsonConvert.DeserializeObject<SessionRequest>(shardsJsonString);


                                            // servers response = Ok   
                                            var cborSessionResponse = CBORObject.NewMap().Add("MSG", transactionObj.MSG);

                                            responses.Add(BaseRequest.Session, cborSessionResponse.EncodeToBytes());
                                        }
                                        break;

                                }

                                return responses;

                            } // end if are enough shards
                        } // end for i
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ConsumeAsync Exception caught: {ex}");
                        _logger.LogError($"ConsumeAsync Exception caught: {ex}");
                        _logger.LogInformation("ConsumeAsync: Transaction failed!");
                        responses.Clear();
                        return responses;
                    }
                }
            } // end while OutputAvailableAsync()

            return responses;
        } // end ConsumeAsync
    }
}
