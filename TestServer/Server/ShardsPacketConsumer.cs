using TestServer.Contracts;
using TestServer.Server.Requests;
using TestServer.Server.Responses;
using Newtonsoft.Json;
using Serilog;
using System.Text;
using System.Threading.Tasks.Dataflow;
using PeterO.Cbor;
using TestServer.Models.Private;
using TestServer.Models.Public;

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

        /// <summary>
        /// Consumes the received transaction shards. If enough shards are received it rebuilds the transaction using 
        /// Reed-Solomon, then depending on the tranaction rTYP it takes the appropriate action.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="serverService"></param>
        /// <returns> Dictionary<BaseRequest, BaseResponse> </returns>
        public async Task<Dictionary<string, BaseResponse>> ConsumeAsync(IReceivableSourceBlock<ShardsPacket> source, IServerService serverService)
        {
            /*
            UnsignedTransaction<CreateOrgRequest> createOrgTrans = null;
            UnsignedTransaction<RegisterOrgRequest> registerOrgTrans = null;
            UnsignedTransaction<CreateUserAndDeviceRequest> createUserAndDeviceTrans = null;
            UnsignedTransaction<RegisterUserAndDeviceRequest> registerUserAndDeviceTrans = null;
            UnsignedTransaction<RegisterDeviceRequest> registerDeviceTrans = null;
            UnsignedTransaction<SendNewUserAndDeviceCodeRequest> sendNewUserAndDeviceCodeTrans = null;
            UnsignedTransaction<CreateDeviceRequest> createDeviceTrans = null;
            UnsignedTransaction<ActivateDeviceRequest> activateDeviceTrans = null;
            UnsignedTransaction<LoginRequest> loginTrans = null;
            UnsignedTransaction<ListDevicesRequest> listDevicesTrans = null;
            UnsignedTransaction<DeactivateDeviceRequest> deactivateDeviceTrans = null;
            UnsignedTransaction<RevokeDeviceRequest> revokeDeviceTrans = null;
            UnsignedTransaction<ActivateUserRequest> activateUserTrans = null;
            UnsignedTransaction<DeactivateUserRequest> deactivateUserTrans = null;
            UnsignedTransaction<DeleteUserRequest> deleteUserTrans = null;

            UnsignedTransaction<CreateFolderRequest> createFolderTrans = null;
            UnsignedTransaction<CreateFileRequest> createFileTrans = null;
            UnsignedTransaction<CreateVersionRequest> createVersionTrans = null;
            UnsignedTransaction<OpenFileRequest> openFileTrans = null;
            UnsignedTransaction<OpenFilePreviousVersionRequest> openFilePrevVerTrans = null;
            UnsignedTransaction<AddReviewRequest> createReviewTrans = null;
            UnsignedTransaction<GetReviewRequest> getReviewTrans = null;
            UnsignedTransaction<InviteUserRequest> inviteUserTrans = null;
            UnsignedTransaction<CreateUserRequest> createUserTrans = null;
            UnsignedTransaction<ListFilesRequest> listFilesTrans = null;
            UnsignedTransaction<GrantAccessRequest> grantAccessTrans = null;
            UnsignedTransaction<RevokeAccessRequest> revokeAccessTrans = null;
            UnsignedTransaction<RenameRequest> renameTrans = null;
            UnsignedTransaction<MoveRequest> moveTrans = null;
            UnsignedTransaction<DeleteRequest> deleteTrans = null;
            UnsignedTransaction<GetFileInfoRequest> getFileInfoTrans = null;           
            UnsignedTransaction<GetUserActionsRequest> getUserActionsTrans = null;
            UnsignedTransaction<GetNumberOfNodesRequest> getNumberOfNodesTrans = null;
            UnsignedTransaction<CopyRequest> copyTrans = null;
            UnsignedTransaction<GetVersionListRequest> getVersionListTrans = null;
            UnsignedTransaction<ListUsersRequest> listUsersTrans = null;
            UnsignedTransaction<GetNodeKeyRequest> getNodeKeyTrans = null;
            UnsignedTransaction<GetUserRequest> getUserTrans = null;
            UnsignedTransaction<CreateGroupRequest> createGroupTrans = null;
            UnsignedTransaction<ListGroupsRequest> listGroupsTrans = null;
            UnsignedTransaction<GetGroupRequest> getGroupTrans = null;
            UnsignedTransaction<ActivateGroupRequest> activateGroupTrans = null;
            UnsignedTransaction<DeactivateGroupRequest> deactivateGroupTrans = null;
            UnsignedTransaction<DeleteGroupRequest> deleteGroupTrans = null;
            UnsignedTransaction<AddFileTagRequest> addFileTagTrans = null;
            UnsignedTransaction<RemoveFileTagRequest> removeFileTagTrans = null;
            UnsignedTransaction<AddUserToGroupRequest> addUserToGroupTrans = null;
            UnsignedTransaction<RemoveUserFromGroupRequest> removeUserFromGroupTrans = null;
            UnsignedTransaction<GetFileActionsRequest> getFileActionsTrans = null;
            UnsignedTransaction<GetPermissionTypesRequest> getPermissionTypesTrans = null;
            UnsignedTransaction<SharedWithRequest> sharedWithTrans = null;
            UnsignedTransaction<ListNoAccessRequest> listNoAccessTrans = null;
            UnsignedTransaction<AddPermissionsRequest> addPermissionsTrans = null;
            UnsignedTransaction<RemovePermissionsRequest> removePermissionsTrans = null;
            UnsignedTransaction<GetAttributeRequest> getAttributeTrans = null;
            UnsignedTransaction<ApprovalRequest> approvalTrans = null;
            UnsignedTransaction<HasUserPermissionRequest> hasUserPermissionTrans = null;
            */


            ulong userId;
            ulong orgId;
            ulong deviceId;

            // Stores the received shards and rebuilds a transaction if enough shards are received
            TransactionShards shards = null;

            // Dictionary/HashMap that stores a list of reponses from multiple requests in a transaction
            // using the Request string as an index key.
            Dictionary<string, BaseResponse> responses = new Dictionary<string, BaseResponse>();

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

                                // Convert Transaction's bytes to a Json string
                                var shardsJsonString = Encoding.UTF8.GetString(shards.GetRebuiltTransactionBytes());
                                                               
                                // Print the Transaction's Json string to console for debug purposes.
                                _logger.LogInformation(shardsJsonString);

                                BaseRequest baseRequest = JsonConvert.DeserializeObject<BaseRequest>(shardsJsonString);

                                /*
                                // First we map the SignedTransaction Json string to the generic BaseRequest object (all requests are
                                // derived from this class) to get the request rTYP (REQ[0].TYP), so later we can map it to
                                // the appropriate request object to get the rest of the Json request's attribute–value pairs.                                
                                Transaction transaction = JsonConvert.DeserializeObject<Transaction>(shardsJsonString);

                                byte[] unsignedTransactionBytes = transaction.STX.GetUTXBytes();

                                string unsignedTransactionJsonString = Encoding.UTF8.GetString(unsignedTransactionBytes);

                                _logger.LogInformation(unsignedTransactionJsonString);

                             
                                UnsignedTransaction<BaseRequest> unsignedTransaction = JsonConvert.DeserializeObject<UnsignedTransaction<BaseRequest>>(unsignedTransactionJsonString);

                                const string VERIFICATION_CODE = "12345";

                                // Get the TYPE of request of the first Request of the Transaction in the requests list.
                                // If it is a CreateOrg request this will be the first request in the list
                                string requestType = unsignedTransaction.REQ[0].TYP;

                                // Try to verify Transaction.
                                // If it is a CreateOrg request then use the user certificate
                                // included in the Tansaction to verify the Transaction, otherwise for
                                // any other Transactin types, use the the user certificate or legalkey
                                // stored in the database table called Certificates and pass null in
                                // the VerifyTransaction function instead of a certificate (last parameter).
                                bool verified = false;
                                if (requestType.Equals(BaseRequest.RegisterOrg))
                                {
                                    registerOrgTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RegisterOrgRequest>>(unsignedTransactionJsonString);

                                    deviceId = serverService.HexStringToULong(registerOrgTrans.REQ[0].DeviceID);
                                    userId = serverService.HexStringToULong(registerOrgTrans.REQ[0].UserID);
                                    orgId = serverService.HexStringToULong(registerOrgTrans.REQ[0].OrgID);

                                    if (registerOrgTrans.REQ[0].Code.Equals(VERIFICATION_CODE))
                                    {

                                        verified = await serverService.VerifyTransaction(
                                            unsignedTransactionBytes,
                                            transaction.STX.SIG,
                                            registerOrgTrans.REQ[0].DeviceDsaPub);

                                        if (verified)
                                        {
                                            Servers.Instance.CreatePrivateBlockchain(registerOrgTrans.REQ[0].OrgID,
                                                                                     registerOrgTrans.bID);
                                        }
                                    }
                                }   
                                else if (requestType.Equals(BaseRequest.RegisterDevice))
                                {
                                    //------------------------------------------------------
                                    //This needs a long term solution
                                    //------------------------------------------------------
                                    registerDeviceTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RegisterDeviceRequest>>(unsignedTransactionJsonString);

                                    verified = await serverService.VerifyTransaction(
                                        unsignedTransactionBytes,
                                        transaction.STX.SIG,
                                        registerDeviceTrans.REQ[0].DeviceDsaPub);

                                    userId = await serverService.GetNodeIdByCode(registerDeviceTrans.REQ[0].Code);

                                    deviceId = await serverService.GetOriginalDeviceNodeIdByUserNodeId(userId);

                                    orgId = await serverService.GetOrgNodeIdByUserNodeId(userId);
                                }
                                else if (requestType.Equals(BaseRequest.RegisterUserAndDevice))
                                {
                                    //------------------------------------------------------
                                    //This needs a long term solution
                                    //------------------------------------------------------
                                    registerUserAndDeviceTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RegisterUserAndDeviceRequest>>(unsignedTransactionJsonString);

                                    verified = await serverService.VerifyTransaction(
                                        unsignedTransactionBytes,
                                        transaction.STX.SIG,
                                        registerUserAndDeviceTrans.REQ[0].DeviceDsaPub);

                                   
                                    userId = await serverService.GetNodeIdByCode(registerUserAndDeviceTrans.REQ[0].Code);

                                    deviceId = await serverService.GetOriginalDeviceNodeIdByUserNodeId(userId);

                                    orgId = await serverService.GetOrgNodeIdByUserNodeId(userId);
                                }
                                else
                                {
                                    deviceId = serverService.HexStringToULong(unsignedTransaction.dID);

                                    userId = await serverService.GetUserNodeIdByDeviceNodeId(deviceId);
                                 
                                    orgId = await serverService.GetOrgNodeIdByUserNodeId(userId);

                                    verified = await serverService.VerifyTransaction(
                                            deviceId,
                                            userId,
                                            unsignedTransactionBytes,
                                            transaction.STX.SIG);
                                }

                                // If Transaction Verified progress further otherwise bail out
                                if (verified)
                                {                                   
                                    _logger.LogInformation("Transanction Verified!");
                                }
                                else
                                {                                   
                                    _logger.LogInformation("Transanction NOT Verified!");
                                    responses.Clear();
                                    return responses;
                                }

                                // Process each Request in the Transaction in sequence.
                                for (int requestNo = 0; requestNo < unsignedTransaction.REQ.Count; requestNo++)
                                {
                                    requestType = unsignedTransaction.REQ[requestNo].TYP;                               

                                    switch (requestType)
                                    {
                                        case BaseRequest.CreateOrg:
                                            
                                            createOrgTrans = JsonConvert.DeserializeObject<UnsignedTransaction<CreateOrgRequest>>(unsignedTransactionJsonString);

                                            var createOrg = await serverService.CreateOrg(createOrgTrans.REQ[requestNo],                                                                                        
                                                                                          createOrgTrans.TS,
                                                                                          createOrgTrans.bID,
                                                                                          createOrgTrans.tID);

                                            responses.Add(BaseRequest.CreateOrg, createOrg);
                                            break;

                                        case BaseRequest.RegisterOrg:

                                            var registerOrg = await serverService.RegisterOrg(registerOrgTrans.REQ[requestNo],
                                                                                              registerOrgTrans.TS,
                                                                                              registerOrgTrans.bID,
                                                                                              registerOrgTrans.tID);

                                            responses.Add(BaseRequest.RegisterOrg, registerOrg);
                                            break;

                                        case BaseRequest.CreateUserAndDevice:

                                            createUserAndDeviceTrans = JsonConvert.DeserializeObject<UnsignedTransaction<CreateUserAndDeviceRequest>>(unsignedTransactionJsonString);


                                            var createUserAndDevice = await serverService.CreateUserAndDevice(createUserAndDeviceTrans.REQ[requestNo],
                                                                                                              userId,
                                                                                                              createUserAndDeviceTrans.TS,
                                                                                                              createUserAndDeviceTrans.bID,
                                                                                                              createUserAndDeviceTrans.tID,
                                                                                                              deviceId);

                                            responses.Add(BaseRequest.CreateUserAndDevice, createUserAndDevice);
                                            break;

                                        case BaseRequest.RegisterUserAndDevice:

                                            registerUserAndDeviceTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RegisterUserAndDeviceRequest>>(unsignedTransactionJsonString);

                                            var registerUserAndDevice = await serverService.RegisterUserAndDevice(registerUserAndDeviceTrans.REQ[requestNo],
                                                                                                                  userId,
                                                                                                                  deviceId,
                                                                                                                  registerUserAndDeviceTrans.TS,
                                                                                                                  registerUserAndDeviceTrans.bID,
                                                                                                                  registerUserAndDeviceTrans.tID);

                                            responses.Add(BaseRequest.RegisterUserAndDevice, registerUserAndDevice);
                                            break;

                                        case BaseRequest.SendNewUserAndDeviceCode:

                                            sendNewUserAndDeviceCodeTrans = JsonConvert.DeserializeObject<UnsignedTransaction<SendNewUserAndDeviceCodeRequest>>(unsignedTransactionJsonString);

                                            var sendNewUserAndDeviceCode = await serverService.SendNewUserAndDeviceCode(
                                                                                              sendNewUserAndDeviceCodeTrans.REQ[requestNo],
                                                                                              sendNewUserAndDeviceCodeTrans.TS,
                                                                                              sendNewUserAndDeviceCodeTrans.bID,
                                                                                              sendNewUserAndDeviceCodeTrans.tID);

                                            responses.Add(BaseRequest.SendNewUserAndDeviceCode, sendNewUserAndDeviceCode);
                                            break;

                                        case BaseRequest.CreateDevice:

                                            createDeviceTrans = JsonConvert.DeserializeObject<UnsignedTransaction<CreateDeviceRequest>>(unsignedTransactionJsonString);

                                            var createDevice = await serverService.CreateDevice(createDeviceTrans.REQ[requestNo],
                                                                                              createDeviceTrans.TS,
                                                                                              createDeviceTrans.bID,
                                                                                              createDeviceTrans.tID);

                                            responses.Add(BaseRequest.CreateDevice, createDevice);
                                            break;

                                        case BaseRequest.RegisterDevice:

                                            registerDeviceTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RegisterDeviceRequest>>(unsignedTransactionJsonString);

                                            var registerDevice = await serverService.RegisterDevice(registerDeviceTrans.REQ[requestNo],
                                                                                              deviceId,
                                                                                              registerDeviceTrans.TS,                                                                                             
                                                                                              registerDeviceTrans.tID);

                                            responses.Add(BaseRequest.RegisterDevice, registerDevice);
                                            break;

                                        case BaseRequest.ActivateDevice:

                                            activateDeviceTrans = JsonConvert.DeserializeObject<UnsignedTransaction<ActivateDeviceRequest>>(unsignedTransactionJsonString);

                                            var activateDevice = await serverService.ActivateDevice(activateDeviceTrans.REQ[requestNo],
                                                                                              activateDeviceTrans.TS,
                                                                                              activateDeviceTrans.bID,
                                                                                              activateDeviceTrans.tID,
                                                                                              deviceId);

                                            responses.Add(BaseRequest.ActivateDevice, activateDevice);
                                            break;

                                        case BaseRequest.DeactivateDevice:

                                            deactivateDeviceTrans = JsonConvert.DeserializeObject<UnsignedTransaction<DeactivateDeviceRequest>>(unsignedTransactionJsonString);

                                            var deactivateDevice = await serverService.DeactivateDevice(deactivateDeviceTrans.REQ[requestNo],
                                                                                                        userId,
                                                                                                        deactivateDeviceTrans.TS,
                                                                                                        deactivateDeviceTrans.bID,
                                                                                                        deactivateDeviceTrans.tID);

                                            responses.Add(BaseRequest.DeactivateDevice, deactivateDevice);
                                            break;


                                        case BaseRequest.RevokeDevice:

                                            revokeDeviceTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RevokeDeviceRequest>>(unsignedTransactionJsonString);

                                            var revokeDevice = await serverService.RevokeDevice(revokeDeviceTrans.REQ[requestNo],
                                                                                                userId,
                                                                                                revokeDeviceTrans.TS,
                                                                                                revokeDeviceTrans.bID,
                                                                                                revokeDeviceTrans.tID);

                                            responses.Add(BaseRequest.RevokeDevice, revokeDevice);
                                            break;

                                        case BaseRequest.Login:

                                            loginTrans = JsonConvert.DeserializeObject<UnsignedTransaction<LoginRequest>>(unsignedTransactionJsonString);

                                            var login = await serverService.Login(deviceId,
                                                                                  loginTrans.TS,
                                                                                  loginTrans.tID);

                                            responses.Add(BaseRequest.Login, login);
                                            break;

                                        case BaseRequest.ListDevices:

                                            listDevicesTrans = JsonConvert.DeserializeObject<UnsignedTransaction<ListDevicesRequest>>(unsignedTransactionJsonString);

                                            var listDevices = await serverService.ListDevices(listDevicesTrans.REQ[requestNo],
                                                                                              userId,
                                                                                              orgId,
                                                                                              listDevicesTrans.TS,
                                                                                              listDevicesTrans.tID);

                                            responses.Add(BaseRequest.ListDevices, listDevices);
                                            break;

                                        case BaseRequest.CreateFolder:

                                            createFolderTrans = JsonConvert.DeserializeObject<UnsignedTransaction<CreateFolderRequest>>(unsignedTransactionJsonString);

                                            var createFolder = await serverService.CreateFolder(createFolderTrans.REQ[requestNo],
                                                                                                userId,
                                                                                                createFolderTrans.TS,
                                                                                                createFolderTrans.tID);
                                            responses.Add(BaseRequest.CreateFolder, createFolder);
                                            break;

                                        case BaseRequest.CreateFile:

                                            createFileTrans = JsonConvert.DeserializeObject<UnsignedTransaction<CreateFileRequest>>(unsignedTransactionJsonString);

                                            var createFile = await serverService.CreateFile(createFileTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            createFileTrans.TS,
                                                                                            createFileTrans.tID);
                                            responses.Add(BaseRequest.CreateFile, createFile);
                                            break;

                                        case BaseRequest.CreateVersion:
                                            _logger.LogInformation("CreateVersion BaseRequest found");
                                            createVersionTrans = JsonConvert.DeserializeObject<UnsignedTransaction<CreateVersionRequest>>(unsignedTransactionJsonString);

                                            var createVersion = await serverService.CreateVersion(createVersionTrans.REQ[requestNo],
                                                                                                  userId,
                                                                                                  createVersionTrans.TS,
                                                                                                  createVersionTrans.tID);
                                            responses.Add(BaseRequest.CreateVersion, createVersion);
                                            break;

                                        case BaseRequest.OpenFile:
 
                                            openFileTrans = JsonConvert.DeserializeObject<UnsignedTransaction<OpenFileRequest>>(unsignedTransactionJsonString);

                                            var file = await serverService.OpenFile(openFileTrans.REQ[requestNo],
                                                                                    userId,
                                                                                    openFileTrans.TS);
                                            responses.Add(BaseRequest.OpenFile, file);
                                            break;

                                        case BaseRequest.OpenPreviousVersion:

                                            openFilePrevVerTrans = JsonConvert.DeserializeObject<UnsignedTransaction<OpenFilePreviousVersionRequest>>(unsignedTransactionJsonString);

                                            var filePreviousVersion = await serverService.OpenFilePreviousVersion(openFilePrevVerTrans.REQ[requestNo],
                                                                                                                  userId,
                                                                                                                  openFilePrevVerTrans.TS);
                                            responses.Add(BaseRequest.OpenPreviousVersion, filePreviousVersion);
                                            break;

                                        case BaseRequest.AddReview:

                                            createReviewTrans = JsonConvert.DeserializeObject<UnsignedTransaction<AddReviewRequest>>(unsignedTransactionJsonString);

                                            var createReview = await serverService.AddReview(createReviewTrans.REQ[requestNo],
                                                                                             userId,
                                                                                             createReviewTrans.TS,
                                                                                             createReviewTrans.tID);
                                            responses.Add(BaseRequest.AddReview, createReview);
                                            break;

                                        case BaseRequest.GetReview:
                                            getReviewTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetReviewRequest>>(unsignedTransactionJsonString);
                                            var getReview = await serverService.GetReview(getReviewTrans.REQ[requestNo]);
                                            responses.Add(BaseRequest.GetReview, getReview);
                                            break;

                                        case BaseRequest.InviteUser:
                                            inviteUserTrans = JsonConvert.DeserializeObject<UnsignedTransaction<InviteUserRequest>>(unsignedTransactionJsonString);

                                            var inviteUser = await serverService.InviteUser(inviteUserTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            inviteUserTrans.TS,
                                                                                            inviteUserTrans.tID);
                                            responses.Add(BaseRequest.InviteUser, inviteUser);
                                            break;

                                        case BaseRequest.CreateUser:
                                            createUserTrans = JsonConvert.DeserializeObject<UnsignedTransaction<CreateUserRequest>>(unsignedTransactionJsonString);
                                            var createUser = await serverService.CreateUser(createUserTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            orgId,
                                                                                            serverService.HexStringToULong(createOrgTrans.dID),
                                                                                            createUserTrans.TS);
                                            responses.Add(BaseRequest.CreateUser, createUser);
                                            break;

                                        case BaseRequest.ListFiles:
                                            listFilesTrans = JsonConvert.DeserializeObject<UnsignedTransaction<ListFilesRequest>>(unsignedTransactionJsonString);
                                            var listFiles = await serverService.ListFiles(listFilesTrans.REQ[requestNo],
                                                                                          userId,
                                                                                          listFilesTrans.tID);
                                            responses.Add(BaseRequest.ListFiles, listFiles);
                                            break;

                                        case BaseRequest.GetNodeKey:
                                            getNodeKeyTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetNodeKeyRequest>>(unsignedTransactionJsonString);
                                            var getNodeKey = await serverService.GetNodeKey(getNodeKeyTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            getNodeKeyTrans.tID);
                                            responses.Add(BaseRequest.GetNodeKey, getNodeKey);
                                            break;

                                        case BaseRequest.GrantAccess:
                                            grantAccessTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GrantAccessRequest>>(unsignedTransactionJsonString);
                                            var grantAccess = await serverService.GrantAccess(grantAccessTrans.REQ[requestNo],
                                                                                              userId,
                                                                                              grantAccessTrans.TS,
                                                                                              grantAccessTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.GrantAccess))
                                            {
                                                responses[BaseRequest.GrantAccess] = grantAccess;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.GrantAccess, grantAccess);
                                            }
                                            break;

                                        case BaseRequest.RevokeAccess:
                                            revokeAccessTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RevokeAccessRequest>>(unsignedTransactionJsonString);
                                            var revokeAccess = await serverService.RevokeAccess(revokeAccessTrans.REQ[requestNo],
                                                                                                userId,
                                                                                                revokeAccessTrans.TS,
                                                                                                revokeAccessTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.RevokeAccess))
                                            {
                                                responses[BaseRequest.RevokeAccess] = revokeAccess;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.RevokeAccess, revokeAccess);
                                            }
                                            break;

                                        case BaseRequest.Rename:
                                            renameTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RenameRequest>>(unsignedTransactionJsonString);
                                            var rename = await serverService.Rename(renameTrans.REQ[requestNo],
                                                                                    userId,
                                                                                    renameTrans.TS,
                                                                                    renameTrans.tID);
                                            responses.Add(BaseRequest.Rename, rename);
                                            break;

                                        case BaseRequest.Move:
                                            moveTrans = JsonConvert.DeserializeObject<UnsignedTransaction<MoveRequest>>(unsignedTransactionJsonString);

                                            var move = await serverService.Move(moveTrans.REQ[requestNo],
                                                                                userId,
                                                                                moveTrans.TS,
                                                                                moveTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.Move))
                                            {
                                                responses[BaseRequest.Move] = move;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.Move, move);
                                            }
                                            break;

                                        case BaseRequest.Delete:
                                            deleteTrans = JsonConvert.DeserializeObject<UnsignedTransaction<DeleteRequest>>(unsignedTransactionJsonString);

                                            var delete = await serverService.Delete(deleteTrans.REQ[requestNo],
                                                                                    userId,
                                                                                    deleteTrans.TS,
                                                                                    deleteTrans.tID);
                                            if (responses.ContainsKey(BaseRequest.Delete))
                                            {
                                                responses[BaseRequest.Delete] = delete;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.Delete, delete);
                                            }
                                            break;

                                        case BaseRequest.GetFileInfo:
                                            getFileInfoTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetFileInfoRequest>>(unsignedTransactionJsonString);

                                            var fileInfo = await serverService.GetFileInfo(getFileInfoTrans.REQ[requestNo],
                                                                                           userId,         
                                                                                           getFileInfoTrans.tID);
                                            responses.Add(BaseRequest.GetFileInfo, fileInfo);
                                            break;

                                        case BaseRequest.GetFileActions:
                                            getFileActionsTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetFileActionsRequest>>(unsignedTransactionJsonString);

                                            var fileActions = await serverService.GetFileActions(getFileActionsTrans.REQ[requestNo],
                                                                                                 getFileActionsTrans.tID);
                                            responses.Add(BaseRequest.GetFileActions, fileActions);
                                            break;

                                        case BaseRequest.GetUserActions:
                                            getUserActionsTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetUserActionsRequest>>(unsignedTransactionJsonString);

                                            var userActions = await serverService.GetUserActions(getUserActionsTrans.REQ[requestNo], getUserActionsTrans.tID);
                                            responses.Add(BaseRequest.GetUserActions, userActions);
                                            break;

                                        case BaseRequest.GetNumberOfNodes:

                                            getNumberOfNodesTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetNumberOfNodesRequest>>(unsignedTransactionJsonString);

                                            var numberOfNodes = await serverService.GetNumberOfNodes(getNumberOfNodesTrans.REQ[requestNo]);
                                            responses.Add(BaseRequest.GetNumberOfNodes, numberOfNodes);
                                            break;

                                        case BaseRequest.Copy:
                                            copyTrans = JsonConvert.DeserializeObject<UnsignedTransaction<CopyRequest>>(unsignedTransactionJsonString);
                                            var copy = await serverService.Copy(copyTrans.REQ[requestNo],
                                                                                userId,
                                                                                copyTrans.TS,
                                                                                copyTrans.tID);
                                            responses.Add(BaseRequest.Copy, copy);
                                            break;

                                        case BaseRequest.GetVersionList:
                                            getVersionListTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetVersionListRequest>>(unsignedTransactionJsonString);

                                            var versionList = await serverService.GetVersionList(getVersionListTrans.REQ[requestNo],
                                                                                                 getVersionListTrans.tID);
                                            responses.Add(BaseRequest.GetVersionList, versionList);
                                            break;

                                        case BaseRequest.ListUsers:

                                            listUsersTrans = JsonConvert.DeserializeObject<UnsignedTransaction<ListUsersRequest>>(unsignedTransactionJsonString);

                                            var listUsers = await serverService.ListUsers(listUsersTrans.REQ[requestNo],
                                                                                          listUsersTrans.bID,
                                                                                          listUsersTrans.TS,
                                                                                          listUsersTrans.tID);

                                            responses.Add(BaseRequest.ListUsers, listUsers);
                                            break;

                                        case BaseRequest.GetUser:

                                            getUserTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetUserRequest>>(unsignedTransactionJsonString);

                                            var getUser = await serverService.GetUser(getUserTrans.REQ[requestNo],
                                                                                      getUserTrans.tID);

                                            responses.Add(BaseRequest.GetUser, getUser);
                                            break;

                                        case BaseRequest.CreateGroup:

                                            createGroupTrans = JsonConvert.DeserializeObject<UnsignedTransaction<CreateGroupRequest>>(unsignedTransactionJsonString);

                                            var createGroup = await serverService.CreateGroup(createGroupTrans.REQ[requestNo],
                                                                                              userId,
                                                                                              createGroupTrans.bID,
                                                                                              createGroupTrans.TS,
                                                                                              createGroupTrans.tID);

                                            responses.Add(BaseRequest.CreateGroup, createGroup);
                                            break;

                                        case BaseRequest.GetGroup:

                                            getGroupTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetGroupRequest>>(unsignedTransactionJsonString);

                                            var getGroup = await serverService.GetGroup(getGroupTrans.REQ[requestNo],
                                                                                        getGroupTrans.tID);

                                            responses.Add(BaseRequest.GetGroup, getGroup);
                                            break;

                                        case BaseRequest.ActivateUser:

                                            activateUserTrans = JsonConvert.DeserializeObject<UnsignedTransaction<ActivateUserRequest>>(unsignedTransactionJsonString);

                                            var activateUser = await serverService.ActivateUser(activateUserTrans.REQ[requestNo],
                                                                                              activateUserTrans.TS,
                                                                                              activateUserTrans.bID,
                                                                                              activateUserTrans.tID,
                                                                                              userId);

                                            responses.Add(BaseRequest.ActivateUser, activateUser);
                                            break;

                                        case BaseRequest.DeactivateUser:

                                            deactivateUserTrans = JsonConvert.DeserializeObject<UnsignedTransaction<DeactivateUserRequest>>(unsignedTransactionJsonString);

                                            var deactivateUser = await serverService.DeactivateUser(deactivateUserTrans.REQ[requestNo],
                                                                                              userId,
                                                                                              deactivateUserTrans.TS,
                                                                                              deactivateUserTrans.bID,
                                                                                              deactivateUserTrans.tID);

                                            responses.Add(BaseRequest.DeactivateUser, deactivateUser);
                                            break;

                                        case BaseRequest.DeleteUser:

                                            deleteUserTrans = JsonConvert.DeserializeObject<UnsignedTransaction<DeleteUserRequest>>(unsignedTransactionJsonString);

                                            var deleteUser = await serverService.DeleteUser(deleteUserTrans.REQ[requestNo],
                                                                                              userId,
                                                                                              deleteUserTrans.TS,
                                                                                              deleteUserTrans.bID,
                                                                                              deleteUserTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.DeleteUser))
                                            {
                                                responses[BaseRequest.DeleteUser] = deleteUser;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.DeleteUser, deleteUser);
                                            }
                                            break;
                                        case BaseRequest.ActivateGroup:

                                            activateGroupTrans = JsonConvert.DeserializeObject<UnsignedTransaction<ActivateGroupRequest>>(unsignedTransactionJsonString);

                                            var activateGroup = await serverService.ActivateGroup(activateGroupTrans.REQ[requestNo],
                                                                                              activateGroupTrans.TS,
                                                                                              activateGroupTrans.bID,
                                                                                              activateGroupTrans.tID,
                                                                                              userId);

                                            responses.Add(BaseRequest.ActivateGroup, activateGroup);
                                            break;

                                        case BaseRequest.DeactivateGroup:

                                            deactivateGroupTrans = JsonConvert.DeserializeObject<UnsignedTransaction<DeactivateGroupRequest>>(unsignedTransactionJsonString);

                                            var deactivateGroup = await serverService.DeactivateGroup(deactivateGroupTrans.REQ[requestNo],
                                                                                              userId,
                                                                                              deactivateGroupTrans.TS,
                                                                                              deactivateGroupTrans.bID,
                                                                                              deactivateGroupTrans.tID);

                                            responses.Add(BaseRequest.DeactivateGroup, deactivateGroup);
                                            break;                                           

                                        case BaseRequest.DeleteGroup:

                                            deleteGroupTrans = JsonConvert.DeserializeObject<UnsignedTransaction<DeleteGroupRequest>>(unsignedTransactionJsonString);

                                            var deleteGroup = await serverService.DeleteGroup(deleteGroupTrans.REQ[requestNo],
                                                                                              userId,
                                                                                              deleteGroupTrans.TS,
                                                                                              deleteGroupTrans.bID,
                                                                                              deleteGroupTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.DeleteGroup))
                                            {
                                                responses[BaseRequest.DeleteGroup] = deleteGroup;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.DeleteGroup, deleteGroup);
                                            }
                                            break;

                                        case BaseRequest.ListGroups:
                                            listGroupsTrans = JsonConvert.DeserializeObject<UnsignedTransaction<ListGroupsRequest>>(unsignedTransactionJsonString);
                                               
                                            var listGroups = await serverService.ListGroups(listGroupsTrans.REQ[requestNo],
                                                                                    listGroupsTrans.bID,
                                                                                    listGroupsTrans.TS,
                                                                                    listGroupsTrans.tID);
                                               
                                            responses.Add(BaseRequest.ListGroups, listGroups);
                                            break;
                                            

                                        case BaseRequest.AddFileTag:

                                            addFileTagTrans = JsonConvert.DeserializeObject<UnsignedTransaction<AddFileTagRequest>>(unsignedTransactionJsonString);

                                            var addFileTag = await serverService.AddFileTag(addFileTagTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            addFileTagTrans.TS,
                                                                                            addFileTagTrans.tID);

                                            responses.Add(BaseRequest.AddFileTag, addFileTag);
                                            break;

                                        case BaseRequest.RemoveFileTag:

                                            removeFileTagTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RemoveFileTagRequest>>(unsignedTransactionJsonString);

                                            var removeFileTag = await serverService.RemoveFileTag(removeFileTagTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            removeFileTagTrans.TS,
                                                                                            removeFileTagTrans.tID);

                                            responses.Add(BaseRequest.RemoveFileTag, removeFileTag);
                                          
                                            break;

                                        case BaseRequest.AddUserToGroup:

                                            addUserToGroupTrans = JsonConvert.DeserializeObject<UnsignedTransaction<AddUserToGroupRequest>>(unsignedTransactionJsonString);

                                            var addUserToGroup = await serverService.AddUserToGroup(addUserToGroupTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            addUserToGroupTrans.TS,
                                                                                            addUserToGroupTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.AddUserToGroup))
                                            {
                                                responses[BaseRequest.AddUserToGroup] = addUserToGroup;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.AddUserToGroup, addUserToGroup);
                                            }

                                            break;

                                        case BaseRequest.RemoveUserFromGroup:

                                            removeUserFromGroupTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RemoveUserFromGroupRequest>>(unsignedTransactionJsonString);

                                            var removeUserFromGroup = await serverService.RemoveUserFromGroup(removeUserFromGroupTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            removeUserFromGroupTrans.TS,
                                                                                            removeUserFromGroupTrans.tID);
                                                                                      
                                            if (responses.ContainsKey(BaseRequest.RemoveUserFromGroup))
                                            {
                                                responses[BaseRequest.RemoveUserFromGroup] = removeUserFromGroup;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.RemoveUserFromGroup, removeUserFromGroup);
                                            }

                                            break;

                                        case BaseRequest.GetPermissionTypes:

                                            getPermissionTypesTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetPermissionTypesRequest>>(unsignedTransactionJsonString);

                                            var getPermissionTypes = await serverService.GetPermissionTypes(getPermissionTypesTrans.REQ[requestNo],
                                                                                        getPermissionTypesTrans.tID);

                                            responses.Add(BaseRequest.GetPermissionTypes, getPermissionTypes);
                                            break;

                                        case BaseRequest.SharedWith:

                                            sharedWithTrans = JsonConvert.DeserializeObject<UnsignedTransaction<SharedWithRequest>>(unsignedTransactionJsonString);

                                            var sharedWith = await serverService.SharedWith(sharedWithTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            sharedWithTrans.bID,
                                                                                            sharedWithTrans.TS,
                                                                                            sharedWithTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.SharedWith))
                                            {
                                                responses[BaseRequest.SharedWith] = sharedWith;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.SharedWith, sharedWith);
                                            }

                                            break;

                                        case BaseRequest.ListNoAccess:

                                            listNoAccessTrans = JsonConvert.DeserializeObject<UnsignedTransaction<ListNoAccessRequest>>(unsignedTransactionJsonString);

                                            var listNoAccess = await serverService.ListNoAccess(listNoAccessTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            listNoAccessTrans.bID,
                                                                                            listNoAccessTrans.TS,
                                                                                            listNoAccessTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.ListNoAccess))
                                            {
                                                responses[BaseRequest.ListNoAccess] = listNoAccess;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.ListNoAccess, listNoAccess);
                                            }

                                            break;

                                        case BaseRequest.AddPermissions:

                                            addPermissionsTrans = JsonConvert.DeserializeObject<UnsignedTransaction<AddPermissionsRequest>>(unsignedTransactionJsonString);

                                            var addPermissions = await serverService.AddPermissions(addPermissionsTrans.REQ[requestNo],
                                                                                            userId,                                                                                         
                                                                                            addPermissionsTrans.TS,
                                                                                            addPermissionsTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.AddPermissions))
                                            {
                                                responses[BaseRequest.AddPermissions] = addPermissions;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.AddPermissions, addPermissions);
                                            }

                                            break;

                                        case BaseRequest.RemovePermission:

                                            removePermissionsTrans = JsonConvert.DeserializeObject<UnsignedTransaction<RemovePermissionsRequest>>(unsignedTransactionJsonString);

                                            var removePermissions = await serverService.RemovePermission(removePermissionsTrans.REQ[requestNo],
                                                                                            userId,
                                                                                            removePermissionsTrans.TS,
                                                                                            removePermissionsTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.RemovePermission))
                                            {
                                                responses[BaseRequest.RemovePermission] = removePermissions;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.RemovePermission, removePermissions);
                                            }

                                            break;

                                        case BaseRequest.GetAttribute:

                                            getAttributeTrans = JsonConvert.DeserializeObject<UnsignedTransaction<GetAttributeRequest>>(unsignedTransactionJsonString);

                                            var getAttribute = await serverService.GetAttribute(getAttributeTrans.REQ[requestNo],
                                                                                                getAttributeTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.GetAttribute))
                                            {
                                                responses[BaseRequest.GetAttribute] = getAttribute;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.GetAttribute, getAttribute);
                                            }

                                            break;

                                        case BaseRequest.Approval:

                                            approvalTrans = JsonConvert.DeserializeObject<UnsignedTransaction<ApprovalRequest>>(unsignedTransactionJsonString);

                                            var approval = await serverService.Approval(approvalTrans.REQ[requestNo],
                                                                                        approvalTrans.TS,                        
                                                                                        approvalTrans.tID);

                                            if (responses.ContainsKey(BaseRequest.Approval))
                                            {
                                                responses[BaseRequest.Approval] = approval;
                                            }
                                            else
                                            {
                                                responses.Add(BaseRequest.Approval, approval);
                                            }

                                            break;


                                        case BaseRequest.CanCreate:
                                        case BaseRequest.CanUpdate:
                                        case BaseRequest.CanDelete:
                                        case BaseRequest.CanShare:

                                            hasUserPermissionTrans = JsonConvert.DeserializeObject<UnsignedTransaction<HasUserPermissionRequest>>(unsignedTransactionJsonString);

                                            var hasUserPermission = await serverService.HasUserPermission(hasUserPermissionTrans.REQ[requestNo],
                                                                                                          userId,  
                                                                                                          hasUserPermissionTrans.tID);

                                            if (responses.ContainsKey(requestType))
                                            {
                                                responses[requestType] = hasUserPermission;
                                            }
                                            else
                                            {
                                                responses.Add(requestType, hasUserPermission);
                                            }

                                            break;

                                        default:
                                            break;

                                    }

                                } // end for each request

                                if (!requestType.Equals(BaseRequest.GetFileInfo) &&
                                    !requestType.Equals(BaseRequest.ListFiles) &&
                                    !requestType.Equals(BaseRequest.GetNumberOfNodes) &
                                    !requestType.Equals(BaseRequest.ListUsers) &
                                    !requestType.Equals(BaseRequest.ListGroups) &
                                    !requestType.Equals(BaseRequest.GetGroup))
                                {

                                    // Insert to private blockchain
                                    //Servers.Instance.InsertToPrivateBlockchain(serverService.ULongToHexString(orgId), shardsBytes);

                                    // Get current block from private blockchain
                                    //long block = Servers.Instance.GetPrivateBlockchain(serverService.ULongToHexString(orgId)).GetLastBlockNo();

                                    // Insert to public blockchain
                                    //Servers.Instance.InsertToPublicBlockchain(shardsBytes);

                                    // Get current block from public blockchain
                                    //long publicBlockchainBlock = Servers.Instance.GetPublicBlockchain().GetLastBlockNo();

                                }
                                */

                                return responses;

                            } // end if are enough shards
                        } // end for i
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"ConsumeAsync Exception caught: {ex}");
                        _logger.LogInformation("ConsumeAsync: Transaction failed!");
                        responses.Clear();
                        return responses;
                    }
                }
            } // end while OutputAvailableAsync()
                            
            return responses;
        } // end ConsumeAsync

        public async Task<Dictionary<string, string>> ConsumeAsync(IReceivableSourceBlock<ShardsPacket> source, string requestType)
        {           

            ulong userId;
            ulong orgId;
            ulong deviceId;

            // Stores the received shards and rebuilds a transaction if enough shards are received
            TransactionShards shards = null;

            // Dictionary/HashMap that stores a list of reponses from multiple requests in a transaction
            // using the Request string as an index key.
            Dictionary<string, string> responses = new Dictionary<string, string>();

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

                                // Convert Transaction's bytes to a Json string
                                var shardsJsonString = Encoding.UTF8.GetString(shards.GetRebuiltTransactionBytes());

                                // Print the Transaction's Json string to console for debug purposes.
                                _logger.LogInformation(shardsJsonString);

                                if (requestType.Equals(BaseRequest.Invite))
                                {
                                    InviteRequest transactionObj = JsonConvert.DeserializeObject<InviteRequest>(shardsJsonString);

                                    // servers store invite.SIGNS + invite.ENCRYPTS for device.id = invite.id         
                                    byte[] inviteID = CryptoUtils.CBORBinaryStringToBytes(transactionObj.inviteID);
                                    KeyStore.Inst.StoreENCRYPTS(inviteID, transactionObj.inviteENCRYPTS);
                                    KeyStore.Inst.StoreSIGNS(inviteID, transactionObj.inviteSIGNS);

                                    // response is just OK, but any response data must be encrypted + signed using owner.KEYS
                                    var cbor = CBORObject.NewMap().Add("INVITE", "SUCCESS");

                                    //return Ok(cbor.ToJSONString());
                                    //return ReturnBytes(cbor.EncodeToBytes());
                                    responses.Add(BaseRequest.Invite, cbor.ToJSONString());
                                }                             

                                return responses;

                            } // end if are enough shards
                        } // end for i
                    }
                    catch (Exception ex)
                    {
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
