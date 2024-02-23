using TestServer.Contracts;
using TestServer.Contracts.Private;
using TestServer.Contracts.Public;
using TestServer.Dtos.Private;
using TestServer.Dtos.Public;
using TestServer.Models.Private;
using TestServer.Server;
using TestServer.Server.Requests;
using TestServer.Server.Responses;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Globalization;
using System.Security.Cryptography;


namespace TestServer.Services
{
    /// <summary>
    /// Server services class handles all the updating/requesting of the DB tables.
    /// </summary>
    public class ServerService : IServerService
    {

        private readonly ILogger<ServerService> _logger;

        private readonly INodesRepository _nodesRepository;
        private readonly IParentsRepository _parentsRepository;
        private readonly IAuditsRepository _auditsRepository;
        private readonly IVersionsRepository _versionsRepository;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IPermissionsRepository _permissionsRepository;
        private readonly IOrgsRepository _publicOrgsRepository;
        private readonly IPublicUsersRepository _publicUsersRepository;
        private readonly IEmailsRepository _publicEmailRepository;
        private readonly IConfiguration _configuration;
        private readonly IBlockchainsRepository _publicBlockchainsRepository;
        private readonly IApprovalsRepository _approvalsRepository;
        private readonly IOwnersRepository _ownersRepository;
        private readonly IAttributesRepository _attributesRepository;

        private CultureInfo cultureInfo = new CultureInfo("es-ES", false);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param fileParent="nodesRepository"></param>       
        /// <param fileParent="parentsRepository"></param>
        /// <param fileParent="certificatesRepository"></param>
        /// <param fileParent="auditsRepository"></param>
        /// <param fileParent="versionsRepository"></param>
        /// <param fileParent="invitationsRepository"></param>
        /// <param fileParent="permissionsRepository"></param>
        /// <param fileParent="publicOrgsRepository"></param>
        /// <param fileParent="publicUsersRepository"></param>
        /// <param fileParent="publicEmailRepository"></param>
        /// <param fileParent="publicBlockchainsRepository"></param>
        /// <param fileParent="approvalsRepository"></param>
        /// <param fileParent="configuration"></param>
        /// <param fileParent="logger"></param>
        public ServerService(INodesRepository nodesRepository,
                             IParentsRepository parentsRepository,
                             IAuditsRepository auditsRepository,
                             IVersionsRepository versionsRepository,
                             IInvitationsRepository invitationsRepository,
                             IPermissionsRepository permissionsRepository,
                             IOrgsRepository publicOrgsRepository,
                             IPublicUsersRepository publicUsersRepository,
                             IEmailsRepository publicEmailRepository,
                             IBlockchainsRepository publicBlockchainsRepository,
                             IApprovalsRepository approvalsRepository,
                             IOwnersRepository ownersRepository,
                             IAttributesRepository attributesRepository,
                             IConfiguration configuration,
                             ILogger<ServerService> logger)
        {
            _nodesRepository = nodesRepository;
            _parentsRepository = parentsRepository;
            _auditsRepository = auditsRepository;
            _versionsRepository = versionsRepository;
            _invitationsRepository = invitationsRepository;
            _permissionsRepository = permissionsRepository;
            _publicOrgsRepository = publicOrgsRepository;
            _publicUsersRepository = publicUsersRepository;
            _publicEmailRepository = publicEmailRepository;
            _publicBlockchainsRepository = publicBlockchainsRepository;
            _approvalsRepository = approvalsRepository;
            _ownersRepository = ownersRepository;
            _attributesRepository = attributesRepository;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Converts a hexadicimal string reprecedting a number to an unsigned long number.
        /// </summary>
        /// <param fileParent="hexString"></param>
        /// <returns>ulong (unsigned long)</returns>
        public ulong HexStringToULong(string hexString)
        {
            return (ulong)Convert.ToInt64(hexString, 16);
        }

        /// <summary>
        /// Converts an unsigned long number to the hexadecimal string representation of that number. 
        /// </summary>
        /// <param fileParent="num"></param>
        /// <returns>a string represention of the unsigned long number in hexadecimal format</returns>
        public string ULongToHexString(ulong? num)
        {
            return Convert.ToString((long)num, 16);
        }

        /// <summary>
        /// Returns the Groups Folder Node for a given orgNodeID.
        /// </summary>
        /// <param name="orgNodeID"></param>
        /// <returns>a Node object for the Groups Folder if found or null</returns>
        private async Task<Node> GetGroupsFolderNodeByOrgId(ulong orgNodeID)
        {
            var orgChildren = await _parentsRepository.GetChildrenByParentNodeId(orgNodeID);
            foreach (var child in orgChildren)
            {
                var childChildren = await _parentsRepository.GetChildrenByParentNodeId(child.NodeId);
                foreach (var childChild in childChildren)
                {
                    var childChildNode = await _nodesRepository.GetByUlongIdAsync(childChild.NodeId);
                    if (childChildNode.Type == (byte)NodeType.GroupsFolder)
                    {
                        return childChildNode;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Updates the Node table.
        /// </summary>
        /// <param fileParent="nodeId"></param>
        /// <param fileParent="type"></param>
        /// <param fileParent="currentParentId"></param>
        /// <param fileParent="currentParentId"></param>
        /// <param fileParent="currentVersionId"></param>
        /// <param fileParent="currentOwnerId"></param>
        /// <param fileParent="nodeKeyWrappedByParentNodeKey"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>NodeDto</returns>
        private async Task<NodeDto> CreateNode(ulong nodeId,
                                               NodeType type,
                                               int currentParentId,
                                               ulong? currentVersionId,
                                               ulong? currentOwnerId,
                                               string? CurrentNodeAesWrapByParentNodeAes,
                                               ulong userNodeId,
                                               string timestamp)
        {

            try
            {
                // define org fileNode record - fileNode table
                NodeDto nodeDto = new NodeDto();

                nodeDto.Id = nodeId;
                nodeDto.Type = (byte)type;
                nodeDto.CurrentParentId = currentParentId;
                nodeDto.CurrentVersionId = currentVersionId;
                nodeDto.CurrentOwnerId = currentOwnerId;
                nodeDto.CurrentNodeAesWrapByParentNodeAes = CurrentNodeAesWrapByParentNodeAes;
                nodeDto.UserNodeId = userNodeId;
                nodeDto.Timestamp = DateTimeOffset.Parse(timestamp);

                // create org fileNode record
                var newNode = await _nodesRepository.AddAsync<NodeDto, NodeDto>(nodeDto);

                return newNode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Updates Parent table.
        /// </summary>
        /// <param fileParent="parentNodeId"></param>
        /// <param fileParent="nodeId"></param>
        /// <param fileParent="nodeKeyWrappedByParentNodeKey"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>ParentDto</returns>
        private async Task<ParentDto> CreateParent(string nameEncByParentEncKey,
                                                   ulong? parentNodeId,
                                                   ulong nodeId,
                                                   string? nodeKeyWrappedByParentNodeKey,
                                                   ulong userNodeId,
                                                   string timestamp)
        {
            try
            {

                // define org fileNode record - fileNode table
                ParentDto nodeParentDto = new ParentDto();

                nodeParentDto.NameEncByParentEncKey = nameEncByParentEncKey;
                nodeParentDto.ParentNodeId = parentNodeId;
                nodeParentDto.NodeId = nodeId;
                nodeParentDto.NodeKeyWrappedByParentNodeKey = nodeKeyWrappedByParentNodeKey;
                nodeParentDto.CurrentParent = true;
                nodeParentDto.UserNodeId = userNodeId;
                nodeParentDto.Timestamp = DateTimeOffset.Parse(timestamp);

                // create org fileNode record
                var newNodeParent = await _parentsRepository.AddAsync<ParentDto, ParentDto>(nodeParentDto);

                return newNodeParent;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        /// <summary>
        /// Updates PublicBlockchain table
        /// </summary>
        /// <param fileParent="nodeId"></param>
        /// <param fileParent="orgNodeId"></param>
        /// <param fileParent="name"></param>
        /// <returns>BlockchainDto</returns>
        private async Task<BlockchainDto> CreatePublicBlockchain(ulong blockchainId, ulong orgNodeId, string name)
        {
            try
            {
                BlockchainDto blockchainDto = new BlockchainDto();

                blockchainDto.Id = blockchainId;
                blockchainDto.OrgId = orgNodeId;
                blockchainDto.Name = name;

                var newBlockchain = await _publicBlockchainsRepository.AddAsync<BlockchainDto, BlockchainDto>(blockchainDto);

                return newBlockchain;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        private async Task<AttributeDto> CreateAttribute(ulong nodeId,
                                                         AttributeType attributeType,
                                                         string attributeValue,
                                                         ulong userNodeId,
                                                         string timestamp)
        {
            try
            {
                AttributeDto attributeDto = new AttributeDto();

                attributeDto.NodeId = nodeId;
                attributeDto.AttributeType = (byte)attributeType;
                attributeDto.AttributeValue = attributeValue;

                attributeDto.UserNodeId = userNodeId;
                attributeDto.Timestamp = DateTimeOffset.Parse(timestamp);

                var newAttribute = await _attributesRepository.AddAsync<AttributeDto, AttributeDto>(attributeDto);

                return newAttribute;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        /// <summary>
        /// Verifies Transaction using the public userDhPrivWrapByDeriveDeviceDhPubUserDhPriv parameter.
        /// </summary>
        /// <param fileParent="dataToVerify"></param>
        /// <param fileParent="verifyKey"></param>
        /// <param fileParent="signature"></param>
        /// <returns>bool</returns>
        /// <exception cref="NotSupportedException"></exception>
        private async Task<bool> VerifyData(byte[] dataToVerify, string verifyKey, string signature)
        {
            try
            {
                byte[] keyBytes = Convert.FromBase64String(verifyKey);

                var ecd = ECDsa.Create();

                ecd.ImportSubjectPublicKeyInfo(keyBytes, out _);

                var signatureData = Convert.FromBase64String(signature);

                return ecd.VerifyData(dataToVerify, signatureData, HashAlgorithmName.SHA256);

            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Verifies Transaction usining groupRecord public userDhPrivWrapByDeriveDeviceDhPubUserDhPriv stored in the DB.
        /// </summary>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="dataToVerify"></param>
        /// <param fileParent="certificateType"></param>
        /// <param fileParent="signature"></param>
        /// <returns>bool</returns>
        public async Task<bool> VerifyTransaction(
                                      ulong deviceId,
                                      ulong userId,
                                      byte[] dataToVerify,
                                      string signature)
        {
            try
            {
                var deviceStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(deviceId, (byte)AttributeType.NodeStatus);
                var userStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(userId, (byte)AttributeType.NodeStatus);

                if (!deviceStatus.Equals(NodeStatus.Active))
                {
                    _logger.LogInformation($"Device status is not active: {deviceStatus}");
                    return false;
                }

                if (!userStatus.Equals(NodeStatus.Active))
                {
                    _logger.LogInformation($"User status is not active: {userStatus}");
                    return false;
                }

                var key = await _attributesRepository.GetFirstValueByNodeIdAndType(deviceId, (byte)AttributeType.DeviceDsaPub);

                return await VerifyData(dataToVerify, key, signature);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Verifies Transaction using the public userDhPrivWrapByDeriveDeviceDhPubUserDhPriv parameter.
        /// </summary>
        /// <param fileParent="dataToVerify"></param>
        /// <param fileParent="signature"></param>
        /// <param fileParent="verifyKey"></param>
        /// <returns>bool</returns>
        public async Task<bool> VerifyTransaction(
                                      byte[] dataToVerify,
                                      string signature,
                                      string publicKeyJson)
        {
            try
            {
                return await VerifyData(dataToVerify, publicKeyJson, signature);
            }
            catch (Exception ex)
            {

                throw;
            }
        }


        /// <summary>
        /// Updates the Invitation table.
        /// </summary>
        /// <param fileParent="encEmail"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="revoked"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>InvitationDto</returns>
        private async Task<InvitationDto> CreateInvitation(string inviteeEmail,
                                                           ulong userId,
                                                           bool revoked,
                                                           string timestamp)
        {
            try
            {
                // define groupRecord deviceNode record - deviceNode table - rTYP CERTIFICATE_TYPE
                InvitationDto invitationDto = new InvitationDto();
                invitationDto.InviteeEmail = inviteeEmail;
                invitationDto.UserNodeId = userId;
                invitationDto.Revoked = revoked;
                invitationDto.Timestamp = DateTimeOffset.Parse(timestamp);

                // create groupRecord deviceNode record - deviceNode table - rTYP CERTIFICATE_TYPE
                var newInvitation = await _invitationsRepository.AddAsync<InvitationDto, InvitationDto>(invitationDto);

                return newInvitation;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Updates the Permission table.
        /// </summary>
        /// <param fileParent="Flags"></param>
        /// <param fileParent="granteeId"></param>
        /// <param fileParent="granterId"></param>
        /// <param fileParent="nodeKeyWrappedByParentNodeKey"></param>
        /// <param fileParent="revoked"></param>
        /// <param fileParent="nodeId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>PermissionDto</returns>
        private async Task<PermissionDto> CreatePermission(ulong flags,
                                                           ulong granteeId,
                                                           ulong granterId,
                                                           string encNodeKey,
                                                           bool revoked,
                                                           ulong nodeId,
                                                           string timestamp)
        {
            try
            {

                PermissionDto permissionDto = new PermissionDto();
                permissionDto.Flags = flags;
                permissionDto.GranteeId = granteeId;
                permissionDto.UserNodeId = granterId;
                permissionDto.Revoked = revoked;
                permissionDto.NodeAesWrapByDeriveGranteeDhPubGranterDhPriv = encNodeKey;
                permissionDto.NodeId = nodeId;
                permissionDto.Timestamp = DateTimeOffset.Parse(timestamp);

                var newPermission = await _permissionsRepository.AddAsync<PermissionDto, PermissionDto>(permissionDto);

                return newPermission;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Updates the PublicOrgs table.
        /// </summary>
        /// <param fileParent="orgNodeId"></param>
        /// <param fileParent="orgEncName"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>OrgDto</returns>
        private async Task<OrgDto> CreatePublicOrg(ulong orgId, string orgName, ulong userId, string timestamp)
        {
            try
            {
                OrgDto orgDto = new OrgDto();
                orgDto.Id = orgId;
                orgDto.Name = orgName;
                orgDto.UserId = userId;
                orgDto.Timestamp = DateTimeOffset.Parse(timestamp);

                var newPublicOrg = await _publicOrgsRepository.AddAsync<OrgDto, OrgDto>(orgDto);

                return newPublicOrg;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Updates the PublicUsers table.
        /// </summary>
        /// <param fileParent="nodeId"></param>
        /// <param fileParent="orgNodeId"></param>
        /// <param fileParent="deviceNode"></param>
        /// <param fileParent="legalKey"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>PublicUserDto</returns>
        private async Task<PublicUserDto> CreatePublicUser(ulong newUserId, ulong? orgId, ulong? deviceId, ulong creatorId, string timestamp)
        {
            try
            {
                PublicUserDto userDto = new PublicUserDto();
                userDto.UserId = creatorId;
                userDto.Id = newUserId;
                userDto.OrgId = orgId;
                userDto.CertId = deviceId;
                userDto.Timestamp = DateTimeOffset.Parse(timestamp);

                var newPublicUser = await _publicUsersRepository.AddAsync<PublicUserDto, PublicUserDto>(userDto);

                return newPublicUser;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Updates the PublicEmail table.
        /// </summary>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="encEmail"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>EmailDto</returns>
        private async Task<EmailDto> CreatePublicEmail(ulong userId, string email, string timestamp)
        {
            try
            {
                EmailDto emailDto = new EmailDto();
                emailDto.UserId = userId;
                emailDto.EmailAddress = email;
                emailDto.Timestamp = DateTimeOffset.Parse(timestamp);

                var newPublicEmail = await _publicEmailRepository.AddAsync<EmailDto, EmailDto>(emailDto);

                return newPublicEmail;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        public async Task<ActivateDeviceResponse> ActivateDevice(ActivateDeviceRequest request,
                                                                 string timestamp,
                                                                 string blockchainId,
                                                                 string transactionId,
                                                                 ulong originalDeviceId)
        {
            try
            {
                ulong deviceId = HexStringToULong(request.DeviceID);

                ulong userNodeId = await GetUserNodeIdByDeviceNodeId(this.HexStringToULong(request.DeviceID));
                //ulong orgNodeId = await GetOrgNodeIdByUserNodeId(activatedUserNodeId);

                var userDhPubKey = await _attributesRepository.GetFirstValueByNodeIdAndType(userNodeId, (byte)AttributeType.UserDhPub);
                // Update Private DB...               

                // Update Attributes table         

                await CreateAttribute(deviceId,
                                      AttributeType.UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv,
                                      request.UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv,
                                      userNodeId,
                                      timestamp);

                await CreateAttribute(deviceId, AttributeType.UserDhPub, userDhPubKey, userNodeId, timestamp);

                if (request.OrgNodeAesWrapByDeriveUserDhPrivDeviceDhPub != null)
                {
                    var originalDeviceStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(
                        deviceId,
                        (byte)AttributeType.OriginalDevice);

                    if (originalDeviceStatus.Equals(OriginalDeviceStatus.False))
                    {
                        var keyAttribute = await _attributesRepository.GetFirstByNodeIdAndType(
                            deviceId,
                            (byte)AttributeType.OrgNodeAesWrapByDeriveUserDhPrivDeviceDhPub);

                        if (keyAttribute == null)
                        {
                            await CreateAttribute(deviceId,
                                                  AttributeType.OrgNodeAesWrapByDeriveUserDhPrivDeviceDhPub,
                                                  request.OrgNodeAesWrapByDeriveUserDhPrivDeviceDhPub,
                                                  userNodeId,
                                                  timestamp);
                        }
                    }
                }

                // update new device status to active
                var deviceAttribute = await _attributesRepository.GetFirstByNodeIdAndType(deviceId, (byte)AttributeType.NodeStatus);

                deviceAttribute.AttributeValue = NodeStatus.Active;

                await _attributesRepository.UpdateAsync(deviceAttribute);


                // create Audit record - audits table
                await CreateAudit(AuditType.Activate, userNodeId, deviceId, timestamp);

                ActivateDeviceResponse response = new ActivateDeviceResponse();

                response.TYP = BaseRequest.ActivateDevice;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }



        public async Task<ActivateGroupResponse> ActivateGroup(ActivateGroupRequest request,
                                                     string timestamp,
                                                     string blockchainId,
                                                     string transactionId,
                                                     ulong userId)
        {
            try
            {
                ulong activatedGroupNodeId = HexStringToULong(request.GroupID);

                // Update Private DB...               

                // Update Attributes table         

                // update new user status to active
                var activatedGroupAttribute = await _attributesRepository.GetFirstByNodeIdAndType(activatedGroupNodeId, (byte)AttributeType.NodeStatus);

                if (activatedGroupAttribute.AttributeValue.Equals(NodeStatus.Deleted))
                {
                    throw new Exception("Unable to activate Group is deleted!");
                }

                activatedGroupAttribute.AttributeValue = NodeStatus.Active;

                await _attributesRepository.UpdateAsync(activatedGroupAttribute);

                // create Audit record - audits table
                await CreateAudit(AuditType.Activate, userId, activatedGroupNodeId, timestamp);

                ActivateGroupResponse response = new ActivateGroupResponse();

                response.TYP = BaseRequest.ActivateGroup;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<ActivateUserResponse> ActivateUser(ActivateUserRequest request,
                                                             string timestamp,
                                                             string blockchainId,
                                                             string transactionId,
                                                             ulong userId)
        {
            try
            {
                ulong activatedUserNodeId = HexStringToULong(request.UserID);

                var userNode = await _nodesRepository.GetByUlongIdAsync(userId);
                var userParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);
                ulong usersFoldeNodeId = (ulong)userParent.ParentNodeId;


                // Update Private DB...               

                // Update Attributes table         

                // update new user status to active
                var activatedUserAttribute = await _attributesRepository.GetFirstByNodeIdAndType(activatedUserNodeId, (byte)AttributeType.NodeStatus);

                if (activatedUserAttribute.AttributeValue.Equals(NodeStatus.Deleted))
                {
                    throw new Exception("Unable to activate User is deleted!");
                }

                activatedUserAttribute.AttributeValue = NodeStatus.Active;

                await _attributesRepository.UpdateAsync(activatedUserAttribute);

                // Activate original device if inactive
                var activatedDeviceNodeId = await GetOriginalDeviceNodeIdByUserNodeId(activatedUserNodeId);
                var activatedDeviceStatus = await _attributesRepository.GetFirstByNodeIdAndType(activatedDeviceNodeId, (byte)AttributeType.NodeStatus);

                if (activatedDeviceStatus.AttributeValue.Equals(NodeStatus.Pending))
                {
                    activatedDeviceStatus.AttributeValue = NodeStatus.Active;

                    await _attributesRepository.UpdateAsync(activatedDeviceStatus);
                    await CreateAudit(AuditType.Activate, userId, activatedDeviceNodeId, timestamp);
                }

                // grant access to Users folder
                ulong usersFlags = 0;
                usersFlags |= (ulong)PermissionType.Read;

                await CreatePermission(usersFlags,
                                       activatedUserNodeId,
                                       userId,
                                       request.UsersAesWrapByDeriveGranteeDhPubGranterDhPriv,
                                       false,
                                       usersFoldeNodeId,
                                       timestamp);


                // grant access to Groups folder
                ulong orgNodeId = await GetOrgNodeIdByUserNodeId(activatedUserNodeId);
                var groupsFolderNode = await GetGroupsFolderNodeByOrgId(orgNodeId);
                ulong groupsFolderNodeId = groupsFolderNode.Id;

                ulong groupsFlag = 0;
                groupsFlag |= (ulong)PermissionType.Read;
                groupsFlag |= (ulong)PermissionType.Create;

                await CreatePermission(groupsFlag,
                                       activatedUserNodeId,
                                       userId,
                                       request.GroupsAesWrapByDeriveGranteeDhPubGranterDhPriv,
                                       false,
                                       groupsFolderNodeId,
                                       timestamp);

                // create Audit record - audits table
                await CreateAudit(AuditType.Activate, userId, activatedUserNodeId, timestamp);

                ActivateUserResponse response = new ActivateUserResponse();

                response.TYP = BaseRequest.ActivateUser;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<AddFileTagResponse> AddFileTag(AddFileTagRequest request, ulong userId, string timestamp, string transactionId)
        {
            ulong fileId = this.HexStringToULong(request.ID);

            var fileParent = await _parentsRepository.GetCurrentParentByNodeId(fileId);

            string tag = request.tag.ToLower();

            await CreateAttribute(fileId, AttributeType.Tag, tag, userId, timestamp);

            await CreateAudit(AuditType.AddFileTag, userId, fileId, timestamp);

            AddFileTagResponse response = new AddFileTagResponse();

            response.TYP = request.TYP;
            response.tID = transactionId;
            response.ID = request.ID;
            response.encNAM = fileParent.NameEncByParentEncKey;
            response.success = true;

            return response;
        }

        public async Task<AddPermissionsResponse> AddPermissions(AddPermissionsRequest request,
                                                               ulong userId,
                                                               string timestamp,
                                                               string transactionId)
        {
            ulong granteeId = HexStringToULong(request.granteeID);
            ulong nodeId = HexStringToULong(request.nodeID);
            ulong flags = request.flags;

            var permission = await _permissionsRepository.GetByNodeIdAndGranteeId(nodeId, granteeId);
            ulong switchedOnFlags = permission.Flags & ~flags;

            permission.Flags = flags;

            await _permissionsRepository.UpdateAsync(permission);


            AddPermissionsResponse response = new AddPermissionsResponse();

            response.TYP = request.TYP;
            response.tID = transactionId;

            return response;
        }

        /// <summary>
        /// Creates a new file Review by updating the Review table.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>CreateReviewResponse</returns>
        public async Task<AddReviewResponse> AddReview(AddReviewRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {
                ApprovalDto reviewDto = new ApprovalDto();

                reviewDto.Type = request.rTYP;
                reviewDto.Id = HexStringToULong(request.ID);
                reviewDto.UserNodeId = HexStringToULong(request.rID);
                reviewDto.VersionId = HexStringToULong(request.vID);
                reviewDto.Comment = request.encCom;
                reviewDto.Timestamp = DateTimeOffset.Parse(timestamp);

                var review = await _approvalsRepository.AddAsync<ApprovalDto, ApprovalDto>(reviewDto);

                var newAuditRecord = await CreateAudit(AuditType.AddReview, reviewDto.UserNodeId, reviewDto.Id, timestamp);

                AddReviewResponse response = new AddReviewResponse();

                response.ID = request.ID;
                response.success = true;
                response.encCom = request.encCom;
                response.TYP = request.TYP;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<AddUserToGroupResponse> AddUserToGroup(AddUserToGroupRequest request,
                                                                 ulong userId,
                                                                 string timestamp,
                                                                 string transactionId)
        {

            ulong groupMemberId = HexStringToULong(request.uID);
            ulong groupId = HexStringToULong(request.ID);

            // Update Private DB...

            ulong permissions = 0;
            permissions |= (ulong)PermissionType.Read;
            permissions |= (ulong)PermissionType.Create;
            permissions |= (ulong)PermissionType.Delete;
            permissions |= (ulong)PermissionType.Update;
            permissions |= (ulong)PermissionType.Review;
            permissions |= (ulong)PermissionType.Approve;
            permissions |= (ulong)PermissionType.Release;

            await CreatePermission(permissions,
                                   groupMemberId,
                                   userId,
                                   request.encUnwrapKEY,
                                   false,
                                   groupId,
                                   timestamp);

            var newAuditRecord = await CreateAudit(AuditType.AddUserToGroup, userId, groupMemberId, timestamp);

            AddUserToGroupResponse response = new AddUserToGroupResponse();

            response.TYP = request.TYP;
            response.tID = transactionId;
            response.ID = request.ID;
            response.success = true;

            return response;
        }

        public async Task<ApprovalResponse> Approval(ApprovalRequest request, string timestamp, string transactionId)
        {
            ApprovalResponse response = new ApprovalResponse();
            response.TYP = request.TYP;
            response.tID = transactionId;
            response.ID = request.ID;
            response.encCom = request.encCom;
            response.success = true;

            ApprovalDto approvalDto = new ApprovalDto();
            approvalDto.Timestamp = DateTimeOffset.Parse(timestamp);

            approvalDto.UserNodeId = HexStringToULong(request.aID);
            approvalDto.Id = HexStringToULong(request.ID);
            approvalDto.VersionId = HexStringToULong(request.vID);
            approvalDto.Comment = request.encCom;

            if (request.aTYP.Equals("approve", StringComparison.OrdinalIgnoreCase))
            {
                approvalDto.Type = (byte)ApprovalType.Approval;
                await CreateAudit(AuditType.Approval, approvalDto.UserNodeId, approvalDto.Id, timestamp);
            }
            else
            if (request.aTYP.Equals("release", StringComparison.OrdinalIgnoreCase))
            {
                approvalDto.Type = (byte)ApprovalType.Release;
                await CreateAudit(AuditType.Release, approvalDto.UserNodeId, approvalDto.Id, timestamp);
            }

            var approval = await _approvalsRepository.AddAsync<ApprovalDto, ApprovalDto>(approvalDto);

            return response;
        }

        /// <summary>
        /// Copy file/folder.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>ListFilesResponse</returns>
        public async Task<ListFilesResponse> Copy(CopyRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {

                ulong nodeId = HexStringToULong(request.ID);
                ulong parentId = HexStringToULong(request.pID);

                string encKey = request.encKEY;

                await CopyNodes(nodeId, parentId, 0, request.newIDs, encKey, userId);

                var response = await ListFolderContents(parentId, userId, BaseRequest.Copy, transactionId);

                var newAuditRecord = await CreateAudit(AuditType.Create, userId, nodeId, timestamp);

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Copy Nodes
        /// </summary>
        /// <param fileParent="copiedNodeId"></param>
        /// <param fileParent="newParentId"></param>
        /// <param fileParent="currentNewIdIndex"></param>
        /// <param fileParent="newIds"></param>
        /// <param fileParent="rootEncKey"></param>
        /// <returns></returns>
        private async Task CopyNodes(ulong copiedNodeId, ulong newParentId, int currentNewIdIndex, List<string> newIds, string rootEncKey, ulong userId)
        {
            try
            {

                var copiedNode = await _nodesRepository.GetByUlongIdAsync(copiedNodeId);

                ulong copiedNodeNewId = HexStringToULong(newIds[currentNewIdIndex]);

                string copiedNodeEncKey = null;

                if (currentNewIdIndex == 0)
                {
                    copiedNodeEncKey = rootEncKey;
                }
                else
                {
                    copiedNodeEncKey = copiedNode.CurrentNodeAesWrapByParentNodeAes;
                }

                if (copiedNode.Type == (byte)NodeType.Folder)
                {
                    var folderParent = await _parentsRepository.GetAsync(copiedNode.CurrentParentId);

                    var newFolderParent = await CreateParent(folderParent.NameEncByParentEncKey, newParentId, copiedNodeNewId,
                        copiedNodeEncKey, copiedNode.UserNodeId, copiedNode.Timestamp.ToString());

                    var newFolderNode = await CreateNode(copiedNodeNewId, NodeType.Folder, newFolderParent.Id,
                        null, userId, copiedNodeEncKey, copiedNode.UserNodeId, copiedNode.Timestamp.ToString());




                }
                else if (copiedNode.Type == (byte)NodeType.File)
                {
                    var fileParent = await _parentsRepository.GetAsync(copiedNode.CurrentParentId);

                    var newFileParent = await CreateParent(fileParent.NameEncByParentEncKey, newParentId, copiedNodeNewId,
                        copiedNodeEncKey, copiedNode.UserNodeId, copiedNode.Timestamp.ToString());

                    var newFileCurrentVersionId = HexStringToULong(newIds[currentNewIdIndex + 1]);

                    var newFileNode = await CreateNode(copiedNodeNewId, NodeType.File, newFileParent.Id, copiedNode.CurrentOwnerId, newFileCurrentVersionId,
                        copiedNodeEncKey, copiedNode.UserNodeId, copiedNode.Timestamp.ToString());

                    var copiedVersions = await _versionsRepository.GetByNodeId(copiedNodeId);

                    foreach (var copiedVersion in copiedVersions)
                    {

                        currentNewIdIndex++;

                        ulong newVersionId = HexStringToULong(newIds[currentNewIdIndex]);

                        var newVersion = await CreateVersion(newVersionId, copiedVersion.Size,
                            copiedNodeId, copiedNode.UserNodeId, copiedVersion.Timestamp.ToString());

                        // Download version
                        var shardsPacket = Servers.Instance.DownloadFileShards(copiedVersion.Id.ToString());

                        // Upload a copy of the version with new nodeId
                        Servers.Instance.UploadFileShards(shardsPacket, newIds[currentNewIdIndex]);

                    }
                    return;
                }

                var nodeChildren = await _parentsRepository.GetChildrenByParentNodeId(copiedNodeId);

                foreach (var child in nodeChildren)
                {
                    currentNewIdIndex++;
                    var childNode = await _nodesRepository.GetByUlongIdAsync(child.NodeId);
                    await CopyNodes(childNode.Id, copiedNodeNewId, currentNewIdIndex, newIds, null, userId);
                }
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Counts a files/folders children (is used by copy to detarmine how many new NodeID ids need).
        /// </summary>
        /// <param fileParent="nodeId"></param>
        /// <returns>int</returns>
        private async Task<int> CountNumberOfNodes(Node node)
        {
            try
            {
                var nodeChildren = await _parentsRepository.GetChildrenByParentNodeId(node.Id);

                int count = 1;

                if (node.Type == (byte)NodeType.File)
                {
                    var versions = await _versionsRepository.GetByNodeId(node.Id);
                    count += versions.Count;
                }

                foreach (var child in nodeChildren)
                {
                    var childNode = await _nodesRepository.GetByUlongIdAsync(child.NodeId);
                    count += await CountNumberOfNodes(childNode);
                }

                return count;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        /// <summary>
        /// Updates the Audit table.
        /// </summary>
        /// <param fileParent="type"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="nodeId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>AuditDto</returns>
        private async Task<AuditDto> CreateAudit(AuditType type, ulong userId, ulong? nodeId, string timestamp)
        {
            try
            {
                // Insert details to create Audit Record here
                AuditDto auditDto = new AuditDto();
                auditDto.Type = (byte)type;
                auditDto.UserNodeId = userId;
                auditDto.NodeId = nodeId;
                auditDto.Timestamp = DateTimeOffset.Parse(timestamp);

                var newAuditRecord = await _auditsRepository.AddAsync<AuditDto, AuditDto>(auditDto);

                return newAuditRecord;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<CreateDeviceResponse> CreateDevice(CreateDeviceRequest request,
                                                             string timestamp,
                                                             string blockchainId,
                                                             string transactionId)
        {
            try
            {

                ulong userId = HexStringToULong(request.UserID);
                ulong deviceId = HexStringToULong(request.DeviceID);

                var userNode = await _nodesRepository.GetByUlongIdAsync(userId);

                // Update Private DB...

                // create Device fileNode
                var newDevParent = await CreateParent(request.DeviceName,
                                                    userId,
                                                    deviceId,
                                                    userNode.CurrentNodeAesWrapByParentNodeAes,
                                                    userId,
                                                    timestamp);

                var newDevNode = await CreateNode(deviceId,
                                                   NodeType.Device,
                                                   newDevParent.Id,
                                                   null,
                                                   userId,
                                                   request.DeviceNodeAesWrapByUserNodeAes,
                                                   userId,
                                                   timestamp);

                await CreateAttribute(deviceId, AttributeType.NodeStatus, NodeStatus.Pending, userId, timestamp);

                await CreateAttribute(userId, AttributeType.Code, request.Code, userId, timestamp);

                await CreateAttribute(deviceId, AttributeType.OriginalDevice, OriginalDeviceStatus.False, userId, timestamp);

                // create Audit record - audits table
                await CreateAudit(AuditType.Create, userId, deviceId, timestamp);

                CreateDeviceResponse response = new CreateDeviceResponse();

                response.TYP = BaseRequest.CreateDevice;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Creates a new File.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>UploadResponse</returns>
        public async Task<UploadResponse> CreateFile(CreateFileRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {
                ulong parentId = HexStringToULong(request.pID);

                UploadResponse response = new UploadResponse();

                response.VersionId = request.verID;
                response.TYP = BaseRequest.CreateFile;
                response.tID = transactionId;

                bool haveCreatePermission = await HavePermission(parentId, PermissionType.Create, userId);

                if (!haveCreatePermission)
                {
                    response.fileManagerResponse = await ListFolderContents(parentId, userId, BaseRequest.CreateFile, transactionId);
                    response.Success = false;
                    return response;
                }

                var parentNode = await _nodesRepository.GetByUlongIdAsync(parentId);


                ulong fileId = HexStringToULong(request.ID);
                string fileName = request.encNAM;
                string fileEncodedKey = request.encKEY;

                ulong versionId = HexStringToULong(request.verID);
                long size = request.size;

                var newFileParent = await CreateParent(fileName, parentId, fileId, parentNode.CurrentNodeAesWrapByParentNodeAes, userId, timestamp);
                var newFileNode = await CreateNode(fileId, NodeType.File, newFileParent.Id, versionId, userId, fileEncodedKey, userId, timestamp);

                var newVersion = await CreateVersion(versionId, size, fileId, userId, timestamp);

                await CreateAudit(AuditType.Create, userId, fileId, timestamp);

                // Add file to all permission lists          
                ulong flags = (ulong)PermissionType.Create | (ulong)PermissionType.Read |
                              (ulong)PermissionType.Update | (ulong)PermissionType.Delete |
                              (ulong)PermissionType.Review | (ulong)PermissionType.Approve |
                              (ulong)PermissionType.Release | (ulong)PermissionType.Share;

                await CreatePermission(flags,
                                       userId,
                                       userId,
                                       fileEncodedKey,
                                       false,
                                       fileId,
                                       timestamp);

                response.fileManagerResponse = await ListFolderContents(parentId, userId, BaseRequest.CreateFile, transactionId);
                response.Success = true;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Creates a Folder.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>ListFilesResponse</returns>
        public async Task<ListFilesResponse> CreateFolder(CreateFolderRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {

                ulong folderId = HexStringToULong(request.ID);
                string encFolderName = request.encNAM;
                ulong parentId = HexStringToULong(request.pID);

                bool haveCreatePermission = await HavePermission(parentId, PermissionType.Create, userId);

                if (!haveCreatePermission)
                {
                    return await ListFolderContents(parentId, userId, BaseRequest.CreateFolder, transactionId);
                }

                var parrentNode = await _nodesRepository.GetByUlongIdAsync(parentId);

                string folderEncNodeKey = request.encKEY;

                var newFolderParent = await CreateParent(encFolderName, parentId, folderId, parrentNode.CurrentNodeAesWrapByParentNodeAes, userId, timestamp);

                var newFolderNode = await CreateNode(folderId, NodeType.Folder, newFolderParent.Id,
                                                null, userId, folderEncNodeKey, userId, timestamp);

                // create audit record - audits table
                var newAuditRecord = await CreateAudit(AuditType.Create, userId, folderId, timestamp);

                // Add folder to all permission lists

                ulong flags = (ulong)PermissionType.Create | (ulong)PermissionType.Read |
                              (ulong)PermissionType.Update | (ulong)PermissionType.Delete |
                              (ulong)PermissionType.Review | (ulong)PermissionType.Approve |
                              (ulong)PermissionType.Release | (ulong)PermissionType.Share;

                await CreatePermission(flags,
                                       userId,
                                       userId,
                                       folderEncNodeKey,
                                       false,
                                       folderId,
                                       timestamp);

                var response = await ListFolderContents(parentId, userId, BaseRequest.CreateFolder, transactionId);

                return response;

            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }
        public async Task<CreateGroupResponse> CreateGroup(CreateGroupRequest request, ulong userId, string blockchainId, string timestamp, string transactionId)
        {
            try
            {

                CreateGroupResponse response = new CreateGroupResponse();

                //_logger.LogInformation($"1 - blockchain ID: {blockchainId}, user ID: {ULongToHexString(userId)}");

                var userNode = await _nodesRepository.GetByUlongIdAsync(userId);

                //_logger.LogInformation("2 - _nodesRepository.GetByUlongIdAsync(userId) succeeded!");

                var userParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);

                //_logger.LogInformation("3 - _parentsRepository.GetAsync(userNode.CurrentParentId) succeeded!");

                var groupsNode = await GetGroupsNodeByChainNodeId(blockchainId);

                var groupsNodeId = groupsNode.Id;

                bool haveCreatePermission = true;// await HavePermission(groupsNodeId, PermissionType.Create, userId);
                if (haveCreatePermission)
                {
                    //_logger.LogInformation($"4 - GetGroupsNodeByChainNodeId Groups ID: {ULongToHexString(parentNodeId)} succeeded!");

                    var encGroupName = request.encNAM;
                    var groupNodeId = HexStringToULong(request.ID);

                    var newGroupParent = await CreateParent(encGroupName,
                                                            groupsNodeId,
                                                            groupNodeId,
                                                            null,
                                                            userId,
                                                            timestamp);

                    //_logger.LogInformation("5 - CreateParent succeeded!");

                    var newGroupNode = await CreateNode(groupNodeId,
                                                        NodeType.Group,
                                                        newGroupParent.Id,
                                                        null,
                                                        userId,
                                                        null,
                                                        userId,
                                                        timestamp);

                    //_logger.LogInformation("6 - CreateNode succeeded!");

                    ulong permissions = (ulong)PermissionType.Create | (ulong)PermissionType.Read |
                                        (ulong)PermissionType.Update | (ulong)PermissionType.Delete |
                                        (ulong)PermissionType.Review | (ulong)PermissionType.Approve |
                                        (ulong)PermissionType.Release | (ulong)PermissionType.Share;

                    await CreatePermission(permissions,
                                           userId,
                                           userId,
                                           request.NodeAESWrapByDeriveGranteeDhPubGranterDhPriv,
                                           false,
                                           groupNodeId,
                                           timestamp);

                    //_logger.LogInformation("7 - CreatePermission succeeded!");

                    await CreateAttribute(groupNodeId, AttributeType.NodeStatus, NodeStatus.Active, userId, timestamp);

                    await CreateAttribute(groupNodeId, AttributeType.GroupDhPub, request.GroupDhPub, userId, timestamp);
                    await CreateAttribute(groupNodeId, AttributeType.EncGroupDhPriv, request.EncGroupDhPriv, userId, timestamp);

                    //_logger.LogInformation("8 - CreateAttributes succeeded!");

                    // create audit record - audits table
                    var newAuditRecord = await CreateAudit(AuditType.Create, userId, groupNodeId, timestamp);

                    response.userPermissions = permissions;
                    response.success = true;
                }
                else
                {
                    response.success = false;
                }

                //_logger.LogInformation("7 - CreateAudit succeeded!");

                response.TYP = request.TYP;
                response.tID = transactionId;

                response.ID = request.ID;
                response.encNAM = request.encNAM;

                response.userID = ULongToHexString(userId);
                response.userEncName = userParent.NameEncByParentEncKey;


                response.groups = await GetGroupsList(blockchainId, false);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"CreateGroup Exception caught: {ex.InnerException}");
                throw;
            }
        }


        /// <summary>
        /// Creates and Organisation.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="orgNodeId"></param>
        /// <param fileParent="creatorId"></param>    
        /// <param fileParent="blockchainId"></param>
        /// <returns>GenericResponse</returns>
        public async Task<CreateOrgResponse> CreateOrg(CreateOrgRequest request,
                                                       string timestamp,
                                                       string blockchainId,
                                                       string transactionId)
        {
            try
            {

                ulong orgId = HexStringToULong(request.OrgID);
                ulong usersFolderId = HexStringToULong(request.UsersID);
                ulong userId = HexStringToULong(request.UserID);
                ulong deviceId = HexStringToULong(request.DeviceID);
                ulong chainId = HexStringToULong(blockchainId);
                ulong groupsFolderId = HexStringToULong(request.GroupsID);

                // Update Private DB...

                // create Org node
                var newOrgParent = await CreateParent(request.OrgName, null, orgId, null, userId, timestamp);

                var newOrgNode = await CreateNode(orgId,
                                                  NodeType.Org,
                                                  newOrgParent.Id,
                                                  null,
                                                  userId,
                                                  request.OrgNodeAesWrapByDeriveUserDhPubDeviceDhPriv,
                                                  userId,
                                                  timestamp);


                // Create Blockchain node
                var newBlockchainParent = await CreateParent(request.ChainName,
                                              orgId,
                                              chainId,
                                              request.OrgNodeAesWrapByDeriveUserDhPubDeviceDhPriv,
                                              userId,
                                              timestamp);

                var newBlockchainNode = await CreateNode(chainId,
                                                  NodeType.Blockchain,
                                                  newBlockchainParent.Id,
                                                  null,
                                                  userId,
                                                  request.ChainNodeAesWrapByOrgNodeAes,
                                                  userId,
                                                  timestamp);


                // create Users folder node
                var newUsersFolderParent = await CreateParent(request.UsersName,
                                                              chainId,
                                                              usersFolderId,
                                                              request.ChainNodeAesWrapByOrgNodeAes,
                                                              userId,
                                                              timestamp);

                var newUsersFolderNode = await CreateNode(usersFolderId,
                                                  NodeType.UsersFolder,
                                                  newUsersFolderParent.Id,
                                                  null,
                                                  userId,
                                                  request.UsersNodeAesWrapByChainNodeAes,
                                                  userId,
                                                  timestamp);

                // create Groups folder node
                var newGroupsFolderParent = await CreateParent(request.GroupsName,
                                                              chainId,
                                                              groupsFolderId,
                                                              request.ChainNodeAesWrapByOrgNodeAes,
                                                              userId,
                                                              timestamp);

                var newGroupsFolderNode = await CreateNode(groupsFolderId,
                                                  NodeType.GroupsFolder,
                                                  newGroupsFolderParent.Id,
                                                  null,
                                                  userId,
                                                  request.GroupsNodeAesWrapByChainNodeAes,
                                                  userId,
                                                  timestamp);

                // create User node record

                var newUserParent = await CreateParent(request.UserName, usersFolderId,
                                                       userId,
                                                       request.UsersNodeAesWrapByChainNodeAes,
                                                       userId,
                                                       timestamp);

                var newUserNode = await CreateNode(userId,
                                                   NodeType.User,
                                                   newUserParent.Id,
                                                   null,
                                                   userId,
                                                   request.UserNodeAesWrapByUsersNodeAes,
                                                   userId,
                                                   timestamp);

                // create Device node
                var newDevParent = await CreateParent(request.DeviceName,
                                                    userId,
                                                    deviceId,
                                                    request.UserNodeAesWrapByUsersNodeAes,
                                                    userId,
                                                    timestamp);

                var newDevNode = await CreateNode(deviceId,
                                                   NodeType.Device,
                                                   newDevParent.Id,
                                                   null,
                                                   userId,
                                                   request.DeviceNodeAesWrapByUserNodeAes,
                                                   userId,
                                                   timestamp);


                // Update Attributes table
                await CreateAttribute(userId, AttributeType.UserType, UserType.Internal, userId, timestamp);
                await CreateAttribute(userId, AttributeType.UserDhPub, request.UserDhPub, userId, timestamp);

                await CreateAttribute(deviceId, AttributeType.DeviceDsaPub, request.DeviceDsaPub, userId, timestamp);
                await CreateAttribute(deviceId, AttributeType.DeviceDhPub, request.DeviceDhPub, userId, timestamp);
                await CreateAttribute(deviceId,
                                      AttributeType.UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv,
                                      request.UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv,
                                      userId,
                                      timestamp);
                await CreateAttribute(deviceId, AttributeType.OriginalDevice, OriginalDeviceStatus.True, userId, timestamp);

                await CreateAttribute(deviceId, AttributeType.NodeStatus, NodeStatus.Active, userId, timestamp);
                await CreateAttribute(userId, AttributeType.NodeStatus, NodeStatus.Active, userId, timestamp);

                // create Audit record - audits table
                await CreateAudit(AuditType.Create, userId, orgId, timestamp);
                await CreateAudit(AuditType.Create, userId, userId, timestamp);
                await CreateAudit(AuditType.Create, userId, userId, timestamp);

                // Upadate Public DB...

                var newPublicUser = await CreatePublicUser(userId, orgId, deviceId, userId, timestamp);
                var newPublicEmail = await CreatePublicEmail(userId, request.UserName, timestamp);
                var newPublicOrg = await CreatePublicOrg(orgId, request.OrgName, userId, timestamp);
                var newBlockchain = await CreatePublicBlockchain(HexStringToULong(blockchainId), orgId, request.OrgName);

                CreateOrgResponse response = new CreateOrgResponse();

                response.oneTimeCode = request.Code;
                response.TYP = BaseRequest.CreateOrg;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Creates a new User.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="orgNodeId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>GenericResponse</returns>
        public async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ulong userId, ulong? orgId, ulong deviceId, string timestamp)
        {
            try
            {
                ulong usersFolderId = await GetUsersNodeIdByDeviceNodeId(deviceId);
                string newUserEncName = request.encNAM;
                string newUserEncEmail = request.encEmail;
                ulong newUserId = HexStringToULong(request.ID);

                var userNode = await _nodesRepository.GetByUlongIdAsync(userId);
                var usersFolderParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);

                bool haveCreatePermission = true;// await HavePermission(usersFolderId, PermissionType.Create, userId);

                if (haveCreatePermission)
                {
                    // Update Private DB...

                    // create groupRecord fileNode record
                    var newUserParent = await CreateParent(newUserEncName,
                                                    usersFolderParent.ParentNodeId,
                                                    newUserId,
                                                    request.CurrentNodeAesWrapByParentNodeAes,
                                                    userId,
                                                    timestamp);

                    var newUserNode = await CreateNode(newUserId,
                                                       NodeType.User,
                                                       usersFolderParent.Id,
                                                       null,
                                                       null,
                                                       request.CurrentNodeAesWrapByParentNodeAes,
                                                       userId,
                                                       timestamp);


                    await CreateAttribute(newUserId, AttributeType.NodeStatus, NodeStatus.Pending, userId, timestamp);
                    await CreateAttribute(newUserId, AttributeType.UserType, UserType.Internal, userId, timestamp);

                    // grant access to Users folder
                    ulong flags = (ulong)PermissionType.Create | (ulong)PermissionType.Read |
                                  (ulong)PermissionType.Update | (ulong)PermissionType.Delete |
                                  (ulong)PermissionType.Review | (ulong)PermissionType.Approve |
                                  (ulong)PermissionType.Release;

                    // grant access to User folder 
                    await CreatePermission(flags,
                                          newUserId,
                                          userId,
                                          request.CurrentNodeAesWrapByParentNodeAes,
                                          false,
                                          newUserId,
                                          timestamp);


                    // create audit record - audits table
                    var newAuditRecord = await CreateAudit(AuditType.Create, newUserId, userId, timestamp);

                    // Upadate Public DB...
                    var newPublicUser = await CreatePublicUser(userId, orgId, deviceId, userId, timestamp);
                    var newPublicEmail = await CreatePublicEmail(newUserId, newUserEncEmail, timestamp);
                }

                CreateUserResponse response = new CreateUserResponse();
                response.oneTimeCode = "123456";

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="userId"></param>
        /// <param name="timestamp"></param>
        /// <param name="blockchainId"></param>
        /// <param name="transactionId"></param>
        /// <param name="requestingDeviceId"></param>
        /// <returns></returns>
        public async Task<ListUsersResponse> CreateUserAndDevice(CreateUserAndDeviceRequest request,
                                                                           ulong userId,
                                                                           string timestamp,
                                                                           string blockchainId,
                                                                           string transactionId,
                                                                           ulong requestingDeviceId)
        {
            try
            {
                ulong usersFolderId = await GetUsersNodeIdByDeviceNodeId(requestingDeviceId);
                ulong newUserId = HexStringToULong(request.UserID);

                // Update Private DB...

                var usersFolderNode = await _nodesRepository.GetByUlongIdAsync(usersFolderId);
                var usersFolderParent = await _parentsRepository.GetAsync(usersFolderNode.CurrentParentId);

                var orgId = usersFolderParent.ParentNodeId;

                bool haveCreatePermission = true;// await HavePermission(usersFolderId, PermissionType.Create, userId);

                if (haveCreatePermission)
                {

                    // create User node record

                    var newUserParent = await CreateParent(request.UserName, usersFolderId,
                                                       newUserId,
                                                       usersFolderNode.CurrentNodeAesWrapByParentNodeAes,
                                                       userId,
                                                       timestamp);

                    var newUserNode = await CreateNode(newUserId,
                                                       NodeType.User,
                                                       newUserParent.Id,
                                                       null,
                                                       newUserId,
                                                       request.UserNodeAesWrapByUsersNodeAes,
                                                       userId,
                                                       timestamp);


                    ulong newDeviceId = HexStringToULong(request.DeviceID);

                    // create Device Node
                    var newDevParent = await CreateParent(request.DeviceName,
                                                        newUserId,
                                                        newDeviceId,
                                                        null,
                                                        userId,
                                                        timestamp);

                    var newDevNode = await CreateNode(newDeviceId,
                                                       NodeType.Device,
                                                       newDevParent.Id,
                                                       null,
                                                       userId,
                                                       request.DeviceNodeAesWrapByUserNodeAes,
                                                       userId,
                                                       timestamp);

                    await CreateAttribute(newUserId, AttributeType.Code, request.Code, userId, timestamp);
                    await CreateAttribute(newUserId, AttributeType.NodeStatus, NodeStatus.Pending, userId, timestamp);
                    await CreateAttribute(newUserId, AttributeType.UserType, UserType.Internal, userId, timestamp);

                    await CreateAttribute(newDeviceId, AttributeType.NodeStatus, NodeStatus.Pending, userId, timestamp);
                    await CreateAttribute(newDeviceId, AttributeType.OriginalDevice, OriginalDeviceStatus.True, userId, timestamp);

                    // grant access to Users folder
                    ulong flags = (ulong)PermissionType.Create | (ulong)PermissionType.Read |
                                  (ulong)PermissionType.Update | (ulong)PermissionType.Delete |
                                  (ulong)PermissionType.Review | (ulong)PermissionType.Approve |
                                  (ulong)PermissionType.Release;

                    // grant access to User folder 
                    await CreatePermission(flags,
                                          newUserId,
                                          userId,
                                          request.UserNodeAesWrapByUsersNodeAes,
                                          false,
                                          newUserId,
                                          timestamp);


                    // create Audit record - audits table                
                    await CreateAudit(AuditType.Create, userId, newUserId, timestamp);
                    await CreateAudit(AuditType.Create, userId, newDeviceId, timestamp);

                    // Upadate Public DB...

                    var newPublicUser = await CreatePublicUser(newUserId, orgId, null, userId, timestamp);
                    var newPublicEmail = await CreatePublicEmail(newUserId, request.UserName, timestamp);
                }

                return await ListUsersResponseCreation(request.TYP, false, blockchainId, transactionId);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        /// <summary>
        /// Updates the Version Table. 
        /// </summary>
        /// <param fileParent="versionId"></param>
        /// <param fileParent="size"></param>
        /// <param fileParent="nodeId"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>VersionDto</returns>
        private async Task<VersionDto> CreateVersion(ulong versionId,
                                                     long size,
                                                     ulong nodeId,
                                                     ulong userNodeId,
                                                     string timestamp)
        {
            try
            {
                VersionDto versionDto = new VersionDto();
                versionDto.Id = versionId;
                versionDto.Size = size;
                versionDto.NodeId = nodeId;
                versionDto.UserNodeId = userNodeId;
                versionDto.Timestamp = DateTimeOffset.Parse(timestamp);

                var newVersion = await _versionsRepository.AddAsync<VersionDto, VersionDto>(versionDto);

                return newVersion;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }



        /// <summary>
        /// Creates a new file Version.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <returns>UploadResponse</returns>
        public async Task<UploadResponse> CreateVersion(CreateVersionRequest request, ulong userId, string timestamp, string transactionId)
        {
            _logger.LogInformation("CreateVersion in ServerService");
            try
            {

                ulong fileId = HexStringToULong(request.ID);
                ulong versionId = HexStringToULong(request.verID);
                long size = request.size;

                var fileNode = await _nodesRepository.GetByUlongIdAsync(fileId);

                var currentParentId = fileNode.CurrentParentId;

                var parent = await _parentsRepository.GetAsync(currentParentId);

                var parentId = parent.ParentNodeId;

                var parentNode = await _nodesRepository.GetByUlongIdAsync(parentId);

                UploadResponse response = new UploadResponse();
                response.VersionId = request.verID;
                response.TYP = BaseRequest.CreateVersion;
                response.tID = transactionId;

                bool haveUpdatePermission = await HavePermission(fileId, PermissionType.Update, userId);
                if (!haveUpdatePermission)
                {
                    response.fileManagerResponse = await ListFolderContents(parentId, userId, BaseRequest.CreateVersion, transactionId);
                    response.Success = false;
                    return response;
                }

                var newVersion = await CreateVersion(versionId, size, fileId, userId, timestamp);

                fileNode.CurrentVersionId = versionId;
                await _nodesRepository.UpdateAsync(fileNode);

                await CreateAudit(AuditType.Update, userId, fileId, timestamp);

                response.fileManagerResponse = await ListFolderContents(parentId, userId, BaseRequest.CreateVersion, transactionId);
                response.Success = true;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        public async Task<DeactivateDeviceResponse> DeactivateDevice(DeactivateDeviceRequest request, ulong userId, string timestamp, string blockchainId, string transactionId)
        {
            try
            {
                ulong deviceId = HexStringToULong(request.DeviceID);

                var deviceAttribute = await _attributesRepository.GetFirstByNodeIdAndType(deviceId, (byte)AttributeType.NodeStatus);

                deviceAttribute.AttributeValue = NodeStatus.Inactive;

                await _attributesRepository.UpdateAsync(deviceAttribute);

                await CreateAudit(AuditType.Deactivate, userId, deviceId, timestamp);

                DeactivateDeviceResponse response = new DeactivateDeviceResponse();

                response.TYP = BaseRequest.DeactivateDevice;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught DeactivateDevice: {ex}");
                throw;
            }
        }



        public async Task<DeactivateGroupResponse> DeactivateGroup(DeactivateGroupRequest request, ulong userId, string timestamp, string blockchainId, string transactionId)
        {
            try
            {
                ulong deactivatedGroupId = HexStringToULong(request.GroupID);

                bool haveDeletePermission = true; //await HavePermission(deactivatedGroupId, PermissionType.Delete, userId);
                if (!haveDeletePermission)
                {
                    var userAttribute = await _attributesRepository.GetFirstByNodeIdAndType(deactivatedGroupId, (byte)AttributeType.NodeStatus);

                    userAttribute.AttributeValue = NodeStatus.Inactive;

                    await _attributesRepository.UpdateAsync(userAttribute);

                    await CreateAudit(AuditType.Deactivate, userId, deactivatedGroupId, timestamp);
                }

                DeactivateGroupResponse response = new DeactivateGroupResponse();

                response.TYP = BaseRequest.DeactivateGroup;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught DeactivateGroup: {ex}");
                throw;
            }
        }

        public async Task<DeactivateUserResponse> DeactivateUser(DeactivateUserRequest request, ulong userId, string timestamp, string blockchainId, string transactionId)
        {
            try
            {
                ulong deactivatedUserId = HexStringToULong(request.UserID);

                bool haveDeletePermission = true;// await HavePermission(deactivatedUserId, PermissionType.Delete, userId);
                if (!haveDeletePermission)
                {

                    var userAttribute = await _attributesRepository.GetFirstByNodeIdAndType(deactivatedUserId, (byte)AttributeType.NodeStatus);

                    userAttribute.AttributeValue = NodeStatus.Inactive;

                    await _attributesRepository.UpdateAsync(userAttribute);

                    await CreateAudit(AuditType.Deactivate, userId, deactivatedUserId, timestamp);
                }

                DeactivateUserResponse response = new DeactivateUserResponse();

                response.TYP = BaseRequest.DeactivateUser;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught DeactivateUser: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Deletes file/folder.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>ListFilesResponse</returns>
        public async Task<ListFilesResponse> Delete(DeleteRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {

                ulong deletedNodeId = HexStringToULong(request.ID);

                var deletedNode = await _nodesRepository.GetByUlongIdAsync(deletedNodeId);

                var currentParent = await _parentsRepository.GetAsync(deletedNode.CurrentParentId);

                bool haveDeletePermission = await HavePermission(deletedNodeId, PermissionType.Delete, userId);
                if (!haveDeletePermission)
                {
                    return await ListFolderContents(currentParent.ParentNodeId, userId, BaseRequest.Delete, transactionId);
                }

                currentParent.CurrentParent = false;
                await _parentsRepository.UpdateAsync(currentParent);

                var newParent = await CreateParent(
                              currentParent.NameEncByParentEncKey,
                              null,
                              currentParent.NodeId,
                              currentParent.NodeKeyWrappedByParentNodeKey,
                              userId,
                              timestamp);

                deletedNode.CurrentParentId = newParent.Id;
                await _nodesRepository.UpdateAsync(deletedNode);

                var newAuditRecord = await CreateAudit(AuditType.Delete, userId, deletedNodeId, timestamp);

                var response = await ListFolderContents(currentParent.ParentNodeId, userId, BaseRequest.Delete, transactionId);

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<DeleteGroupResponse> DeleteGroup(DeleteGroupRequest request, ulong userId, string timestamp, string blockchainId, string transactionId)
        {
            try
            {
                ulong deletedGroupId = HexStringToULong(request.GroupID);

                bool haveDeletePermission = true;// await HavePermission(deletedGroupId, PermissionType.Delete, userId);
                if (haveDeletePermission)
                {

                    var groupAttribute = await _attributesRepository.GetFirstByNodeIdAndType(deletedGroupId, (byte)AttributeType.NodeStatus);

                    groupAttribute.AttributeValue = NodeStatus.Deleted;

                    await _attributesRepository.UpdateAsync(groupAttribute);

                    await CreateAudit(AuditType.Delete, userId, deletedGroupId, timestamp);
                }

                DeleteGroupResponse response = new DeleteGroupResponse();

                response.TYP = BaseRequest.DeleteGroup;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught DeleteGroup: {ex}");
                throw;
            }
        }

        public async Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ulong userId, string timestamp, string blockchainId, string transactionId)
        {
            try
            {
                ulong deletedUserId = HexStringToULong(request.UserID);

                var deletedUserNode = await _nodesRepository.GetByUlongIdAsync(deletedUserId);

                var userParent = await _parentsRepository.GetAsync(deletedUserNode.CurrentParentId);

                bool haveDeletePermission = true;// await HavePermission(userParent.ParentNodeId, PermissionType.Delete, userId);
                if (haveDeletePermission)
                {
                    var userAttribute = await _attributesRepository.GetFirstByNodeIdAndType(deletedUserId, (byte)AttributeType.NodeStatus);

                    userAttribute.AttributeValue = NodeStatus.Deleted;

                    await _attributesRepository.UpdateAsync(userAttribute);

                    await CreateAudit(AuditType.Delete, userId, deletedUserId, timestamp);
                }

                DeleteUserResponse response = new DeleteUserResponse();

                response.TYP = BaseRequest.DeleteUser;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught DeleteUser: {ex}");
                throw;
            }
        }

        public async Task<GetFileActionsResponse> GetFileActions(GetFileActionsRequest request, string transactionId)
        {
            ulong fileId = HexStringToULong(request.ID);

            var fileNode = await _nodesRepository.GetByUlongIdAsync(fileId);

            GetFileActionsResponse response = new GetFileActionsResponse();
            response.tID = transactionId;
            response.TYP = request.TYP;

            //var actions = await _auditsRepository.GetByNodeId(fileId);
            var actions = await _auditsRepository.GetReviewsAndApprovalsByNodeId(fileId);

            foreach (var action in actions)
            {
                var userParent = await _parentsRepository.GetCurrentParentByNodeId(action.UserNodeId);
                var userEncName = userParent.NameEncByParentEncKey;

                FileAction fileAction = new FileAction(ULongToHexString(action.UserNodeId), action.Timestamp.ToString(), userEncName, "");

                response.actions.Add(fileAction);
            }

            return response;
        }

        public async Task<GetAttributeResponse> GetAttribute(GetAttributeRequest request, string transactionId)
        {
            try
            {
                GetAttributeResponse response = new GetAttributeResponse();
                response.tID = transactionId;
                response.TYP = request.TYP;

                ulong nodeId = HexStringToULong(request.ID);

                var attribute = await _attributesRepository.GetFirstByNodeIdAndType(nodeId, request.AttrType);

                response.AttrValue = attribute.AttributeValue;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetAttribute Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Get file information.
        /// </summary>
        /// <param fileParent="request"></param>      
        /// <returns>ListFilesResponse</returns>
        public async Task<GetFileInfoResponse> GetFileInfo(GetFileInfoRequest request, ulong userId, string transactionId)
        {
            try
            {
                ulong fileId = HexStringToULong(request.ID);

                var sharedPermission = await _permissionsRepository.GetSharedByNodeIdAndGranteeId(fileId, userId);

                var fileNode = await _nodesRepository.GetByUlongIdAsync(fileId);
                var fileParent = await _parentsRepository.GetAsync(fileNode.CurrentParentId);

                GetFileInfoResponse response = new GetFileInfoResponse();

                response.tID = transactionId;
                response.TYP = BaseRequest.GetFileInfo;

                response.FileInfo.ID = request.ID;
                response.FileInfo.TYP = Enum.GetName(typeof(NodeType), fileNode.Type);

                // if not a delagated file
                if (sharedPermission == null)
                {
                    response.FileInfo.encNAM = fileParent.NameEncByParentEncKey;
                }
                else
                {
                    var attribute = await _attributesRepository.GetFirstByNodeIdAndTypeAndUserNodeId(fileId, (int)AttributeType.NameEncByGranteeNodeAes, userId);
                    var grantNameEncByNodeAes = attribute.AttributeValue;
                    response.FileInfo.encNAM = grantNameEncByNodeAes;
                }

                response.FileInfo.hasChild = false;

                var fileOwnerNode = await _nodesRepository.GetByUlongIdAsync(fileNode.CurrentOwnerId);
                var fileOnwerParent = await _parentsRepository.GetAsync(fileOwnerNode.CurrentParentId);

                response.FileInfo.encOwner = fileOnwerParent.NameEncByParentEncKey;
                response.FileInfo.encOwnerID = ULongToHexString(fileOnwerParent.NodeId);

                response.FileInfo.cTS = fileNode.Timestamp.ToString(cultureInfo);

                var tagAttributes = await _attributesRepository.GetListByNodeIdAndType(fileId, (byte)AttributeType.Tag);

                foreach (var tagAttribute in tagAttributes)
                {
                    response.FileInfo.tags.Add(tagAttribute.AttributeValue);
                }

                if (fileNode.Type == (byte)NodeType.File)
                {
                    var currentVersion = await _versionsRepository.GetByUlongIdAsync(fileNode.CurrentVersionId);

                    response.FileInfo.size = currentVersion.Size;
                    response.FileInfo.mTS = currentVersion.Timestamp.ToString(cultureInfo);

                    var currentVersionUserNode = await _nodesRepository.GetByUlongIdAsync(currentVersion.UserNodeId);
                    var currentVersionUserParent = await _parentsRepository.GetAsync(currentVersionUserNode.CurrentParentId);

                    response.FileInfo.encMUser = currentVersionUserParent.NameEncByParentEncKey;
                    response.FileInfo.encMUserID = ULongToHexString(currentVersionUserParent.NodeId);

                    List<Models.Private.Version> versions = await _versionsRepository.GetByNodeId(fileId);
                    response.FileInfo.verNUM = versions.Count;

                    response.FileInfo.relNUM = 1;

                    response.FileInfo.status = GetFileInfoResponse.STATUS_TYPE_DRAFT;
                }
                else
                {
                    response.FileInfo.size = 0;
                    response.FileInfo.mTS = fileNode.Timestamp.ToString(cultureInfo);
                    response.FileInfo.encMUser = fileOnwerParent.NameEncByParentEncKey;
                    response.FileInfo.encMUserID = ULongToHexString(fileOnwerParent.NodeId);

                    var folderChildren = await _parentsRepository.GetChildrenByParentNodeId(fileId);
                    if (folderChildren.Count > 0)
                    {
                        response.FileInfo.hasChild = true;
                    }

                }

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Gets a files details (size, date created, etc).
        /// </summary>
        /// <param fileParent="file"></param>       
        /// <param fileParent="isDelagated"></param>
        /// <returns>FileDetails</returns>
        private async Task<FileDetails> GetFileNodeDetails(Node file, bool isShared, ulong userId)
        {
            var fileParent = await _parentsRepository.GetAsync(file.CurrentParentId);

            string type = Enum.GetName(typeof(NodeType), file.Type);
            long size;
            string modified;

            bool hasChild;
            string fileNameEncByParentEncKey = fileParent.NameEncByParentEncKey;
            if (isShared)
            {
                var attribute = await _attributesRepository.GetFirstByNodeIdAndTypeAndUserNodeId(file.Id, (int)AttributeType.NameEncByGranteeNodeAes, userId);
                var grantNameEncByNodeAes = attribute.AttributeValue;
                fileNameEncByParentEncKey = grantNameEncByNodeAes;
            }

            if (file.Type == (byte)NodeType.File)
            {
                hasChild = false;

                var currentVersion = await _versionsRepository.GetByUlongIdAsync(file.CurrentVersionId);
                size = currentVersion.Size;

                List<Models.Private.Version> versions = await _versionsRepository.GetByNodeId(file.Id);

                modified = currentVersion.Timestamp.ToString(cultureInfo);

                var userParent = await _parentsRepository.GetCurrentParentByNodeId(currentVersion.UserNodeId);

                return new FileDetails(ULongToHexString(file.Id),
                        type,
                        fileNameEncByParentEncKey,
                        hasChild,
                        modified,
                        size,
                        ListFilesResponse.STATUS_TYPE_DRAFT,
                        userParent.NameEncByParentEncKey,
                        ULongToHexString(userParent.NodeId));
            }
            else
            if (file.Type == (byte)NodeType.Folder)
            {
                size = 0;

                modified = file.Timestamp.ToString(cultureInfo);

                var userParent = await _parentsRepository.GetCurrentParentByNodeId(file.UserNodeId);

                var folderChildren = await _parentsRepository.GetChildrenByParentNodeId(file.Id);
                if (folderChildren.Count > 0)
                {
                    hasChild = true;
                }
                else
                {
                    hasChild = false;
                }

                return new FileDetails(ULongToHexString(file.Id),
                         type,
                         fileNameEncByParentEncKey,
                         hasChild,
                         modified,
                         size,
                         "",
                         userParent.NameEncByParentEncKey,
                         ULongToHexString(userParent.NodeId));
            }
            else
            {
                return null;
            }

        }

        public async Task<GetGroupResponse> GetGroup(GetGroupRequest request, string transactionId)
        {
            var groupNode = await _nodesRepository.GetByUlongIdAsync(HexStringToULong(request.ID));
            var groupParent = await _parentsRepository.GetAsync(groupNode.CurrentParentId);
            var groupStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(groupNode.Id, (byte)AttributeType.NodeStatus);

            GetGroupResponse response = new GetGroupResponse();

            response.TYP = request.TYP;
            response.tID = transactionId;

            response.ID = request.ID;
            response.NameEncByParentEncKey = groupParent.NameEncByParentEncKey;
            response.Status = groupStatus;
            response.Type = "";

            var groupPermissions = await _permissionsRepository.GetPermissionsByNodeId(groupNode.Id);

            foreach (var permission in groupPermissions)
            {
                ulong userId = permission.GranteeId;

                var userNode = await _nodesRepository.GetByUlongIdAsync(userId);
                var userParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);

                UserDetailsObject user = new UserDetailsObject();

                user.ID = ULongToHexString(userId);
                user.NameEncByParentEncKey = userParent.NameEncByParentEncKey;

                response.Users.Add(user);
            }

            return response;
        }

        public async Task<List<GroupObject>> GetGroupsList(string blockchainId, bool showDeleted)
        {
            List<GroupObject> groupsList = new List<GroupObject>();

            var groupsNode = await GetGroupsNodeByChainNodeId(blockchainId);

            var groupsFolderChildren = await _parentsRepository.GetChildrenByParentNodeId(groupsNode.Id);

            foreach (var groupChild in groupsFolderChildren)
            {
                var groupNode = await _nodesRepository.GetByUlongIdAsync(groupChild.NodeId);
                var groupStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(groupNode.Id, (byte)AttributeType.NodeStatus);

                if (!showDeleted)
                {
                    if (groupStatus.Equals(NodeStatus.Deleted))
                    {
                        continue;
                    }
                }

                GroupObject group = new GroupObject();

                group.ID = ULongToHexString(groupNode.Id);
                group.NameEncByParentEncKey = groupChild.NameEncByParentEncKey;
                group.Status = groupStatus;

                var groupPermissions = await _permissionsRepository.GetPermissionsByNodeId(groupNode.Id);

                foreach (var permission in groupPermissions)
                {
                    ulong userId = permission.GranteeId;

                    var userNode = await _nodesRepository.GetByUlongIdAsync(userId);
                    var userParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);

                    UserDetailsObject user = new UserDetailsObject();

                    user.ID = ULongToHexString(userId);
                    user.NameEncByParentEncKey = userParent.NameEncByParentEncKey;

                    group.Users.Add(user);
                }

                groupsList.Add(group);
            }

            return groupsList;
        }

        private async Task<Node> GetGroupsNodeByChainNodeId(string blockchainId)
        {
            try
            {

                var blockchainChildren = await _parentsRepository.GetChildrenByParentNodeId(HexStringToULong(blockchainId));

                foreach (var blockchainChild in blockchainChildren)
                {
                    var childNode = await _nodesRepository.GetByUlongIdAsync(blockchainChild.NodeId);

                    if (childNode.Type == (byte)NodeType.GroupsFolder)
                    {
                        return childNode;
                    }

                }

                throw new Exception("No Groups folder found!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetGroupsNodeByChainNodeId Exception caught: {ex}");
                throw;
            }
        }

        public async Task<ulong> GetNodeIdByCode(string code)
        {
            try
            {
                var codeAttibute = await _attributesRepository.GetByAttributeValue(code);
                return codeAttibute.NodeId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetNodeIdByCode Exception caught: {ex}");
                throw;
            }
        }

        public async Task<GetNodeKeyResponse> GetNodeKey(GetNodeKeyRequest request, ulong userId, string transactionId)
        {
            GetNodeKeyResponse response = new GetNodeKeyResponse();
            response.tID = transactionId;
            response.TYP = BaseRequest.GetNodeKey;

            var node = await _nodesRepository.GetByUlongIdAsync(HexStringToULong(request.ID));

            //check if file is a shared file from another user 
            var permission = await _permissionsRepository.GetSharedByNodeIdAndGranteeId(node.Id, userId);

            // if is not a delegated/shared file then
            // return the CurrentNodeAesWrapByParentNodeAes otherwise return the
            // NodeAesWrapByDeriveGranteeDhPubGranterDhPriv from the permissions table
            if (permission == null)
            {
                response.CurrentNodeAesWrapByParentNodeAes = node.CurrentNodeAesWrapByParentNodeAes;
            }
            else
            {
                if (await HaveInheritedPermissions(node.Id, userId))
                {
                    response.CurrentNodeAesWrapByParentNodeAes = node.CurrentNodeAesWrapByParentNodeAes;
                }
                else
                {
                    response.CurrentNodeAesWrapByParentNodeAes = permission.NodeAesWrapByDeriveGranteeDhPubGranterDhPriv;
                }
            }

            return response;
        }

        /// <summary>
        /// Counts a files/folders children (is used by copy to detarmine how many new NodeID ids need).
        /// </summary>
        /// <param fileParent="request"></param>
        /// <returns>GetNumberOfNodesResponse</returns>
        public async Task<GetNumberOfNodesResponse> GetNumberOfNodes(GetNumberOfNodesRequest request)
        {
            try
            {
                GetNumberOfNodesResponse response = new GetNumberOfNodesResponse();

                var node = await _nodesRepository.GetByUlongIdAsync(HexStringToULong(request.ID));

                response.NodeCount = await CountNumberOfNodes(node);

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<ulong> GetOriginalDeviceNodeIdByUserNodeId(ulong userId)
        {
            try
            {
                var userChildren = await _parentsRepository.GetChildrenByParentNodeId(userId);

                foreach (var child in userChildren)
                {
                    var childNode = await _nodesRepository.GetByUlongIdAsync(child.NodeId);

                    if (childNode.Type == (byte)NodeType.Device)
                    {
                        var attribute = await _attributesRepository.GetFirstByNodeIdAndType(childNode.Id, (byte)AttributeType.OriginalDevice);
                        if (attribute != null)
                        {
                            if (attribute.AttributeValue.Equals(OriginalDeviceStatus.True))
                            {
                                return childNode.Id;
                            }
                        }
                    }
                }

                throw new Exception("Node devices found!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetOriginalDeviceFromUserId Exception caught: {ex}");
                throw;
            }

        }

        public async Task<ulong> GetOrgNodeIdByUserNodeId(ulong userNodeId)
        {
            try
            {
                var userNode = await _nodesRepository.GetByUlongIdAsync(userNodeId);
                var userParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);
                var usersFolderNode = await _nodesRepository.GetByUlongIdAsync(userParent.ParentNodeId);
                var usersFolderParent = await _parentsRepository.GetAsync(usersFolderNode.CurrentParentId);
                var chainNode = await _nodesRepository.GetByUlongIdAsync(usersFolderParent.ParentNodeId);
                var chainParent = await _parentsRepository.GetAsync(chainNode.CurrentParentId);

                return (ulong)chainParent.ParentNodeId;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<GetPermissionTypesResponse> GetPermissionTypes(GetPermissionTypesRequest request, string transactionId)
        {
            GetPermissionTypesResponse response = new GetPermissionTypesResponse();

            response.PermissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Read), (ulong)PermissionType.Read));
            response.PermissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Create), (ulong)PermissionType.Create));
            response.PermissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Delete), (ulong)PermissionType.Delete));
            response.PermissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Update), (ulong)PermissionType.Update));
            response.PermissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Review), (ulong)PermissionType.Review));
            response.PermissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Approve), (ulong)PermissionType.Approve));
            response.PermissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Release), (ulong)PermissionType.Release));
            response.PermissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Share), (ulong)PermissionType.Share));

            response.tID = transactionId;
            response.TYP = request.TYP;

            return response;
        }

        /// <summary>
        /// Gets a file review. 
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <returns>OpenFileResponse</returns>
        public async Task<OpenFileResponse> GetReview(GetReviewRequest request)
        {
            try
            {

                var review = await _approvalsRepository.GetByUlongIdAsync(HexStringToULong(request.ID));

                OpenFileResponse response = new OpenFileResponse();
                response.ShardsPacket = Servers.Instance.DownloadFileShards(request.ID);

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        public async Task<GetUserResponse> GetUser(GetUserRequest request, string transactionId)
        {
            GetUserResponse response = new GetUserResponse();
            response.tID = transactionId;
            response.TYP = request.TYP;

            var userNode = await _nodesRepository.GetByUlongIdAsync(HexStringToULong(request.userID));
            var userParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);
            var userStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(userNode.Id, (byte)AttributeType.NodeStatus);
            var userAudit = await _auditsRepository.GetMostRecentByNodeId(userNode.Id);

            response.ID = ULongToHexString(userNode.Id);
            response.NameEncByParentEncKey = userParent.NameEncByParentEncKey;
            response.LastModified = userAudit.Timestamp.ToString(cultureInfo);

            response.Classification = new List<string> { "Class1", "Class2" };

            var userTypeAttributeValue = await _attributesRepository.GetFirstValueByNodeIdAndType(userNode.Id, (byte)AttributeType.UserType);

            response.UserType = userTypeAttributeValue;

            var groupPermissions = await _permissionsRepository.GetGroupsByGranteeId(userNode.Id);

            response.NoOfGroupsIn = groupPermissions.Count;

            foreach (var groupPermission in groupPermissions)
            {
                var groupStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(groupPermission.NodeId, (byte)AttributeType.NodeStatus);

                if (groupStatus.Equals(NodeStatus.Deleted))
                {
                    continue;
                }

                var groupNode = await _nodesRepository.GetByUlongIdAsync(groupPermission.NodeId);
                var groupParent = await _parentsRepository.GetAsync(groupNode.CurrentParentId);

                GroupDetailsObject group = new GroupDetailsObject();

                group.ID = ULongToHexString(groupNode.Id);
                group.NameEncByParentEncKey = groupParent.NameEncByParentEncKey;

                response.Groups.Add(group);
            }

            response.Status = userStatus;

            return response;
        }

        /// <summary>
        /// Gets file Audit.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <returns>GetAuditResponse</returns>
        public async Task<GetUserActionsResponse> GetUserActions(GetUserActionsRequest request, string transactionId)
        {
            try
            {
                GetUserActionsResponse response = new GetUserActionsResponse();
                response.tID = transactionId;
                response.TYP = request.TYP;

                ulong userId = HexStringToULong(request.ID);
                List<Audit> auditList = await _auditsRepository.GetUserActions(userId);
                //_logger.LogInformation($"GetUserActions auditList {auditList}");
                //_logger.LogInformation($"GetUserActions auditList size {auditList.Count()}");
                foreach (var audit in auditList)
                {
                    var node = await _nodesRepository.GetByUlongIdAsync(audit.NodeId);

                    if (node == null)
                    {
                        continue;
                    }

                    UserActionInfo userActionInfo = new UserActionInfo();

                    userActionInfo.ActionType = Enum.GetName(typeof(AuditType), audit.Type);

                    var nodeParent = await _parentsRepository.GetAsync(node.CurrentParentId);

                    userActionInfo.NodeID = ULongToHexString(audit.NodeId);
                    userActionInfo.NodeEncName = nodeParent.NameEncByParentEncKey;

                    userActionInfo.NodeType = Enum.GetName(typeof(NodeType), node.Type);

                    userActionInfo.Timestamp = audit.Timestamp.ToString(cultureInfo);

                    response.Actions.Add(userActionInfo);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetAudit Exception caught: {ex}");
                throw;
            }
        }


        private async Task<Node> GetUsersNodeByChainNodeId(string blockchainId)
        {
            try
            {

                var blockchainChildren = await _parentsRepository.GetChildrenByParentNodeId(HexStringToULong(blockchainId));

                foreach (var blockchainChild in blockchainChildren)
                {
                    var childNode = await _nodesRepository.GetByUlongIdAsync(blockchainChild.NodeId);

                    if (childNode.Type == (byte)NodeType.UsersFolder)
                    {
                        return childNode;
                    }

                }

                throw new Exception("No Groups folder found!");
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<ulong> GetUserNodeIdByDeviceNodeId(ulong deviceId)
        {
            try
            {
                _logger.LogInformation($"GetUserNodeIdByDeviceNodeId: {deviceId}");
                var deviceNode = await _nodesRepository.GetByUlongIdAsync(deviceId);
                var deviceParent = await _parentsRepository.GetAsync(deviceNode.CurrentParentId);
                var userNode = await _nodesRepository.GetByUlongIdAsync(deviceParent.ParentNodeId);
                _logger.LogInformation($"UserID: {userNode.Id}");

                return userNode.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetUserNodeIdByDeviceNodeId Exception caught: {ex}");
                throw;
            }
        }

        public async Task<ulong> GetUsersNodeIdByDeviceNodeId(ulong deviceId)
        {
            try
            {
                var deviceNode = await _nodesRepository.GetByUlongIdAsync(deviceId);
                var deviceParent = await _parentsRepository.GetAsync(deviceNode.CurrentParentId);
                var userNode = await _nodesRepository.GetByUlongIdAsync(deviceParent.ParentNodeId);
                var userParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);
                var usersFolderNode = await _nodesRepository.GetByUlongIdAsync(userParent.ParentNodeId);

                return usersFolderNode.Id;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<GetVersionListResponse> GetVersionList(GetVersionListRequest request, string transactionId)
        {
            try
            {
                GetVersionListResponse response = new GetVersionListResponse();

                response.tID = transactionId;
                response.TYP = BaseRequest.GetVersionList;

                string fileId = request.ID;

                var fileNode = await _nodesRepository.GetByUlongIdAsync(HexStringToULong(fileId));

                var fileParent = await _parentsRepository.GetAsync(fileNode.CurrentParentId);

                response.FileVersions.ID = fileId;
                response.FileVersions.TYP = Enum.GetName(typeof(NodeType), fileNode.Type);
                response.FileVersions.encNAM = fileParent.NameEncByParentEncKey;

                List<Models.Private.Version> versions = await _versionsRepository.GetByNodeId(HexStringToULong(fileId));

                int verNum = versions.Count;
                foreach (var version in versions)
                {
                    FileVersion fileVersion = new FileVersion();

                    fileVersion.verID = ULongToHexString(version.Id);
                    fileVersion.mTS = version.Timestamp.ToString(cultureInfo);

                    var userNode = await _nodesRepository.GetByUlongIdAsync(version.UserNodeId);
                    var userParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);

                    fileVersion.encMUser = userParent.NameEncByParentEncKey;
                    fileVersion.encMUserID = ULongToHexString(userParent.NodeId);
                    fileVersion.size = version.Size;
                    fileVersion.verNUM = verNum;
                    fileVersion.relNUM = 1;
                    fileVersion.status = GetVersionListResponse.STATUS_TYPE_DRAFT;

                    response.FileVersions.verLIST.Add(fileVersion);

                    verNum--;
                }

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        private async Task updatePermissionsAudit(ulong previousFlags,
                                            ulong newFlags,
                                            PermissionType permissionType,
                                            AuditType grantedPermission,
                                            AuditType removePermission,
                                            ulong userId,
                                            ulong granteeId,
                                            ulong nodeId,
                                            string timestamp)
        {
            if ((previousFlags & (ulong)permissionType) != 0)
            {
                if ((newFlags & (ulong)permissionType) == 0)
                {
                    await CreateAudit(removePermission, userId, granteeId, timestamp);
                    await CreateAudit(removePermission, userId, nodeId, timestamp);
                }
            }
            else
            {
                if ((newFlags & (ulong)permissionType) != 0)
                {
                    await CreateAudit(grantedPermission, userId, granteeId, timestamp);
                    await CreateAudit(grantedPermission, userId, nodeId, timestamp);
                }
            }
        }

        /// <summary>
        /// Add file/folder to an access rTYP list (authors, approvers, etc) by updating the Permissions table.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>GenericResponse</returns>
        public async Task<GrantAccessResponse> GrantAccess(GrantAccessRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {
                GrantAccessResponse response = new GrantAccessResponse();
                response.TYP = BaseRequest.GrantAccess;
                response.tID = transactionId;

                ulong granteeNodeId = HexStringToULong(request.GranteeNodeID);
                ulong nodeId = HexStringToULong(request.NodeID);

                var node = await _nodesRepository.GetByUlongIdAsync(nodeId);


                bool haveSharePermission = await HavePermission(nodeId, PermissionType.Share, userId);
                if (haveSharePermission)
                {
                    // make sure that the permissions of the onwer of the file can't changed by another user
                    // and also the owner can't switch off his/her Share permission
                    if (granteeNodeId == node.UserNodeId)
                    {
                        // if is the owner he/she can switch off permissions except Share
                        // otherwise prevent another user with shre permission to remove
                        // permissions from the owner of the file/folder 
                        if (granteeNodeId == userId)
                        {
                            if ((request.Flags & (ulong)PermissionType.Share) == 0)
                            {
                                request.Flags |= (ulong)PermissionType.Share;
                            }
                        }
                        else
                        {
                            return response;
                        }
                    }

                    // if granter different from the owner, then prervent the granter giving permissions to a user 
                    // different from the permission given to the granter from the owner
                    if (node.UserNodeId != userId)
                    {
                        var sharedPermission = await _permissionsRepository.GetSharedByNodeIdAndGranteeId(nodeId, granteeNodeId);
                        if (sharedPermission != null)
                        {
                            ulong newFlags = 0;
                            foreach (ulong flag in Enum.GetValues(typeof(PermissionType)))
                            {
                                if ((request.Flags & flag) != 0 && (sharedPermission.Flags & flag) != 0)
                                {
                                    newFlags |= flag;
                                }
                            }

                            if (newFlags == 0)
                            {
                                return response;
                            }
                            else
                            {
                                request.Flags = newFlags;
                            }
                        }
                    }

                    Permission permission;
                    if (userId == node.UserNodeId)
                    {
                        permission = await _permissionsRepository.GetByNodeIdAndUserNodeIdAndGranteedNodeId(nodeId, userId, granteeNodeId);
                        if (permission == null)
                        {
                            permission = await _permissionsRepository.GetSharedByNodeIdAndGranteeId(nodeId, granteeNodeId);
                        }
                    }
                    else
                    {
                        permission = await _permissionsRepository.GetSharedByNodeIdAndGranteeId(nodeId, granteeNodeId);
                    }

                    if (permission == null)
                    {

                        await CreatePermission(request.Flags,
                                                granteeNodeId,
                                                userId,
                                                request.NodeAesWrapByGranteeNodeAes,
                                                false,
                                                nodeId,
                                                timestamp);

                        await CreateAttribute(nodeId,
                                                AttributeType.NameEncByGranteeNodeAes,
                                                request.NameEncByGranteeNodeAes,
                                                granteeNodeId,
                                                timestamp);


                        if ((request.Flags & (ulong)PermissionType.Read) != 0)
                        {
                            await CreateAudit(AuditType.GrantedRead, userId, granteeNodeId, timestamp);
                            await CreateAudit(AuditType.GrantedRead, userId, nodeId, timestamp);
                        }

                        if ((request.Flags & (ulong)PermissionType.Create) != 0)
                        {
                            await CreateAudit(AuditType.GrantedCreate, userId, granteeNodeId, timestamp);
                            await CreateAudit(AuditType.GrantedCreate, userId, nodeId, timestamp);
                        }

                        if ((request.Flags & (ulong)PermissionType.Update) != 0)
                        {
                            await CreateAudit(AuditType.GrantedUpdate, userId, granteeNodeId, timestamp);
                            await CreateAudit(AuditType.GrantedUpdate, userId, nodeId, timestamp);
                        }

                        if ((request.Flags & (ulong)PermissionType.Delete) != 0)
                        {
                            await CreateAudit(AuditType.GrantedDelete, userId, granteeNodeId, timestamp);
                            await CreateAudit(AuditType.GrantedDelete, userId, nodeId, timestamp);
                        }

                        if ((request.Flags & (ulong)PermissionType.Review) != 0)
                        {
                            await CreateAudit(AuditType.GrantedReview, userId, granteeNodeId, timestamp);
                            await CreateAudit(AuditType.GrantedReview, userId, nodeId, timestamp);
                        }

                        if ((request.Flags & (ulong)PermissionType.Approve) != 0)
                        {
                            await CreateAudit(AuditType.GrantedApprove, granteeNodeId, nodeId, timestamp);
                            await CreateAudit(AuditType.GrantedApprove, userId, nodeId, timestamp);
                        }

                        if ((request.Flags & (ulong)PermissionType.Release) != 0)
                        {
                            await CreateAudit(AuditType.GrantedRelease, userId, granteeNodeId, timestamp);
                            await CreateAudit(AuditType.GrantedRelease, userId, nodeId, timestamp);
                        }

                        if ((request.Flags & (ulong)PermissionType.Share) != 0)
                        {
                            await CreateAudit(AuditType.GrantedShare, userId, granteeNodeId, timestamp);
                            await CreateAudit(AuditType.GrantedShare, userId, nodeId, timestamp);
                        }

                        response.success = true;
                    }
                    else
                    {

                        ulong newFlags = request.Flags;

                        ulong previousFlags = permission.Flags;

                        if (newFlags != previousFlags)
                        {
                            permission.Flags = newFlags;

                            await _permissionsRepository.UpdateAsync(permission);

                            await updatePermissionsAudit(previousFlags,
                                                    newFlags,
                                                    PermissionType.Read,
                                                    AuditType.GrantedRead,
                                                    AuditType.RevokedRead,
                                                    userId,
                                                    granteeNodeId,
                                                    nodeId,
                                                    timestamp);

                            await updatePermissionsAudit(previousFlags,
                                                    newFlags,
                                                    PermissionType.Create,
                                                    AuditType.GrantedCreate,
                                                    AuditType.RevokedCreate,
                                                    userId,
                                                    granteeNodeId,
                                                    nodeId,
                                                    timestamp);

                            await updatePermissionsAudit(previousFlags,
                                                        newFlags,
                                                        PermissionType.Update,
                                                        AuditType.GrantedUpdate,
                                                        AuditType.RevokedUpdate,
                                                        userId,
                                                        granteeNodeId,
                                                        nodeId,
                                                        timestamp);

                            await updatePermissionsAudit(previousFlags,
                                                        newFlags,
                                                        PermissionType.Delete,
                                                        AuditType.GrantedDelete,
                                                        AuditType.RevokedDelete,
                                                        userId,
                                                        granteeNodeId,
                                                        nodeId,
                                                        timestamp);

                            await updatePermissionsAudit(previousFlags,
                                                    newFlags,
                                                    PermissionType.Review,
                                                    AuditType.GrantedReview,
                                                    AuditType.RevokedReview,
                                                    userId,
                                                    granteeNodeId,
                                                    nodeId,
                                                    timestamp);

                            await updatePermissionsAudit(previousFlags,
                                                    newFlags,
                                                    PermissionType.Approve,
                                                    AuditType.GrantedApprove,
                                                    AuditType.RevokedApprove,
                                                    userId,
                                                    granteeNodeId,
                                                    nodeId,
                                                    timestamp);

                            await updatePermissionsAudit(previousFlags,
                                                    newFlags,
                                                    PermissionType.Release,
                                                    AuditType.GrantedRelease,
                                                    AuditType.RevokedRelease,
                                                    userId,
                                                    granteeNodeId,
                                                    nodeId,
                                                    timestamp);

                            await updatePermissionsAudit(previousFlags,
                                                        newFlags,
                                                        PermissionType.Share,
                                                        AuditType.GrantedShare,
                                                        AuditType.RevokedShare,
                                                        userId,
                                                        granteeNodeId,
                                                        nodeId,
                                                        timestamp);

                            response.success = true;

                        }
                    }
                }


                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        // checks for an inherited permision recursively
        private async Task<bool> HaveInheritedPermission(ulong? nodeId, ulong userId, PermissionType permissionType)
        {
            var node = await _nodesRepository.GetByUlongIdAsync(nodeId);

            var parent = await _parentsRepository.GetAsync(node.CurrentParentId);

            if (parent == null)
            {
                return false;
            }

            var parentNode = await _nodesRepository.GetByUlongIdAsync(parent.ParentNodeId);
            if (parentNode.Type != (ulong)NodeType.Folder)
            {
                return false;
            }

            var parentPermissions = await _permissionsRepository.GetByNodeIdAndGranteeId(parentNode.Id, userId);

            if (parentPermissions != null)
            {
                if ((parentPermissions.Flags & (ulong)permissionType) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return await HaveInheritedPermissions(parentNode.Id, userId);
        }

        // checks for any inherited permisions recursively
        private async Task<bool> HaveInheritedPermissions(ulong? nodeId, ulong userId)
        {
            var node = await _nodesRepository.GetByUlongIdAsync(nodeId);

            var parent = await _parentsRepository.GetAsync(node.CurrentParentId);

            if (parent == null)
            {
                return false;
            }

            var parentNode = await _nodesRepository.GetByUlongIdAsync(parent.ParentNodeId);
            if (parentNode.Type != (ulong)NodeType.Folder)
            {
                return false;
            }

            var parentPermissions = await _permissionsRepository.GetByNodeIdAndGranteeId(parentNode.Id, userId);

            if (parentPermissions != null)
            {
                if (parentPermissions.Flags == (ulong)PermissionType.None)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return await HaveInheritedPermissions(parentNode.Id, userId);
        }

        // checks permissions recursively
        private async Task<bool> HavePermission(ulong? nodeId,
                                                PermissionType permissionType,
                                                ulong userId)
        {
            var node = await _nodesRepository.GetByUlongIdAsync(nodeId);

            if (!(node.Type == (byte)NodeType.File ||
                  node.Type == (byte)NodeType.Folder ||
                  node.Type == (byte)NodeType.User ||
                  node.Type == (byte)NodeType.UsersFolder ||
                  node.Type == (byte)NodeType.Group ||
                  node.Type == (byte)NodeType.GroupsFolder))
            {
                return false;
            }

            var permissions = await _permissionsRepository.GetByNodeIdAndGranteeId(nodeId, userId);

            if (permissions != null)
            {
                if ((permissions.Flags & (ulong)permissionType) == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            var parent = await _parentsRepository.GetAsync(node.CurrentParentId);

            if (parent == null)
            {
                return false;
            }

            return await HavePermission(parent.ParentNodeId, permissionType, userId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<HasUserPermissionResponse> HasUserPermission(HasUserPermissionRequest request, ulong userId, string transactionId)
        {
            HasUserPermissionResponse response = new HasUserPermissionResponse();
            response.TYP = request.TYP;
            response.tID = transactionId;

            ulong nodeId = HexStringToULong(request.NodeID);

            if (request.TYP.Equals(BaseRequest.CanCreate, StringComparison.OrdinalIgnoreCase))
            {
                response.HasUserPermission = await HavePermission(nodeId, PermissionType.Create, userId);
            }
            else if (request.TYP.Equals(BaseRequest.CanDelete, StringComparison.OrdinalIgnoreCase))
            {
                response.HasUserPermission = await HavePermission(nodeId, PermissionType.Delete, userId);
            }
            else if (request.TYP.Equals(BaseRequest.CanUpdate, StringComparison.OrdinalIgnoreCase))
            {
                response.HasUserPermission = await HavePermission(nodeId, PermissionType.Update, userId);
            }
            else if (request.TYP.Equals(BaseRequest.CanShare, StringComparison.OrdinalIgnoreCase))
            {
                response.HasUserPermission = await HavePermission(nodeId, PermissionType.Share, userId);
            }

            return response;
        }

        /// <summary>
        /// Invites a User (updates the Invitation table).
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>GenericResponse</returns>
        public async Task<InviteUserResponse> InviteUser(InviteUserRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {

                InvitationDto invitationDto = new InvitationDto();
                invitationDto.InviteeEmail = request.email;
                invitationDto.Revoked = false;
                invitationDto.UserNodeId = userId;

                var invitation = await _invitationsRepository.AddAsync<InvitationDto, InvitationDto>(invitationDto);

                var newAuditRecord = await CreateAudit(AuditType.Read, userId, userId, timestamp);

                InviteUserResponse response = new InviteUserResponse();
                response.TYP = BaseRequest.InviteUser;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        public async Task<ListDevicesResponse> ListDevices(ListDevicesRequest request, ulong userId, ulong orgId, string timestamp, string transactionId)
        {
            _logger.LogInformation("ListDevices");
            ListDevicesResponse response = new ListDevicesResponse();
            response.TYP = BaseRequest.ListDevices;
            response.tID = transactionId;
            ulong userNodeId = HexStringToULong(request.UserID);
            _logger.LogInformation($"User NodeId: {request.UserID}");
            var userChildren = await _parentsRepository.GetChildrenByParentNodeId(userNodeId);
            _logger.LogInformation($"userChildren.Count: {userChildren.Count}");

            var userNode = await _nodesRepository.GetByUlongIdAsync(userNodeId);

            response.UserAESKey = userNode.CurrentNodeAesWrapByParentNodeAes;

            foreach (var child in userChildren)
            {
                var childNode = await _nodesRepository.GetByUlongIdAsync(child.NodeId);

                if (childNode.Type == (byte)NodeType.Device)
                {
                    DeviceObject deviceObject = new DeviceObject();

                    var deviceNode = childNode;

                    var deviceDhPub = await _attributesRepository.GetFirstByNodeIdAndType(deviceNode.Id, (byte)AttributeType.DeviceDhPub);

                    var deviceStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(deviceNode.Id, (byte)AttributeType.NodeStatus);

                    var deviceParent = await _parentsRepository.GetAsync(deviceNode.CurrentParentId);

                    var deviceAudit = await _auditsRepository.GetMostRecentByNodeId(deviceNode.Id);

                    deviceObject.DeviceID = ULongToHexString(deviceNode.Id);
                    deviceObject.DeviceName = deviceParent.NameEncByParentEncKey;

                    if (deviceDhPub != null)
                    {
                        deviceObject.DeviceDhPub = deviceDhPub.AttributeValue;
                    }
                    else
                    {
                        deviceObject.DeviceDhPub = null;
                    }

                    deviceObject.Status = deviceStatus;
                    deviceObject.LastModified = deviceAudit.Timestamp.ToString(cultureInfo);

                    response.Devices.Add(deviceObject);
                }
            }

            return response;
        }



        /// <summary>
        /// ListFiles a Folders contents.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <returns>ListFilesResponse</returns>
        public async Task<ListFilesResponse> ListFiles(ListFilesRequest request, ulong userId, string transactionId)
        {
            try
            {
                ulong folderId = HexStringToULong(request.ID);
                return await ListFolderContents(folderId, userId, BaseRequest.ListFiles, transactionId);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        private async Task<bool> SharedNodeNotDeleted(Node sharedFileNode)
        {
            if (!(sharedFileNode.Type == (byte)NodeType.File || sharedFileNode.Type == (byte)NodeType.Folder))
            {
                return true;
            }

            var sharedFileParent = await _parentsRepository.GetAsync(sharedFileNode.CurrentParentId);
            if (sharedFileParent.ParentNodeId == null)
            {
                return false;
            }
            else
            {
                var parentNode = await _nodesRepository.GetByUlongIdAsync(sharedFileParent.ParentNodeId);
                if (parentNode == null)
                {
                    return false;
                }
                return await SharedNodeNotDeleted(parentNode);
            }
        }

        /// <summary>
        /// ListFiles a folders contents.
        /// </summary>
        /// <param fileParent="folderId"></param>
        /// <returns>ListFilesResponse</returns>
        private async Task<ListFilesResponse> ListFolderContents(ulong? folderId, ulong userId, string requestType, string transactionId)
        {
            try
            {
                var folderNode = await _nodesRepository.GetByUlongIdAsync(folderId);
                if (folderNode.Type == (byte)NodeType.User)
                {
                    if (folderId != userId)
                    {
                        folderId = userId;
                    }
                }

                ListFilesResponse response = new ListFilesResponse();

                string folderEncNodeKey = folderNode.CurrentNodeAesWrapByParentNodeAes;

                var folderChildren = await _parentsRepository.GetChildrenByParentNodeId(folderId);

                var permissions = await _permissionsRepository.GetByNodeIdAndGranteeId(folderId, userId);

                response.TYP = requestType;
                response.tID = transactionId;
                response.encKEY = folderEncNodeKey;
                if (permissions != null)
                {
                    response.flags = permissions.Flags;
                }

                foreach (var child in folderChildren)
                {
                    var childNode = await _nodesRepository.GetByUlongIdAsync(child.NodeId);
                    // temp fix - so only returns files and folders nodes.
                    if (childNode.Type == (byte)NodeType.Folder || childNode.Type == (byte)NodeType.File)
                    {
                        response.nodes.Add(await GetFileNodeDetails(childNode, false, userId));
                    }
                }

                // check if it is the root folder
                // and add shared files if present
                if (folderId == userId)
                {
                    var sharedFiles = await _permissionsRepository.GetSharedFilesByUserId(userId);

                    foreach (var sharedFile in sharedFiles)
                    {
                        var sharedFileNode = await _nodesRepository.GetByUlongIdAsync(sharedFile.NodeId);

                        // check if shared file is deleted                       
                        if (await SharedNodeNotDeleted(sharedFileNode))
                        {
                            // chreck for any inheritetd permissions so you will not display the
                            // file/folder twice
                            if (!await HaveInheritedPermissions(sharedFileNode.Id, userId))
                            {
                                response.nodes.Add(await GetFileNodeDetails(sharedFileNode, true, userId));
                            }
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<ListGroupsResponse> ListGroups(ListGroupsRequest request, string blockchainId, string timestamp, string transactionId)
        {
            ListGroupsResponse response = new ListGroupsResponse();

            response.TYP = request.TYP;
            response.tID = transactionId;

            response.Groups = await GetGroupsList(blockchainId, request.Deleted);

            return response;
        }

        public async Task<ListNoAccessResponse> ListNoAccess(ListNoAccessRequest request,
                                                 ulong userId,
                                                 string blockchainId,
                                                 string timestamp,
                                                 string transactionId)
        {
            var groupsFolder = await GetGroupsNodeByChainNodeId(blockchainId);
            var usersFolder = await GetUsersNodeByChainNodeId(blockchainId);

            ulong nodeId = HexStringToULong(request.ID);

            ListNoAccessResponse response = new ListNoAccessResponse();

            response.TYP = request.TYP;
            response.tID = transactionId;

            response.ID = request.ID;
            response.encGroupsFolderKEY = groupsFolder.CurrentNodeAesWrapByParentNodeAes;
            response.encUsersFolderKEY = usersFolder.CurrentNodeAesWrapByParentNodeAes;

            var permissions = await _permissionsRepository.GetPermissionsByNodeId(nodeId);

            var blockchainChildren = await _parentsRepository.GetChildrenByParentNodeId(HexStringToULong(blockchainId));

            foreach (var blockchainChild in blockchainChildren)
            {
                var childNode = await _nodesRepository.GetByUlongIdAsync(blockchainChild.NodeId);

                if (childNode.Type == (byte)NodeType.UsersFolder)
                {
                    var usersFolderChildren = await _parentsRepository.GetChildrenByParentNodeId(childNode.Id);

                    foreach (var usersFolderChild in usersFolderChildren)
                    {
                        var userNode = await _nodesRepository.GetByUlongIdAsync(usersFolderChild.NodeId);
                        var userStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(userNode.Id, (byte)AttributeType.NodeStatus);

                        if (userStatus.Equals(NodeStatus.Deleted))
                        {
                            continue;
                        }

                        bool found = false;
                        foreach (var permission in permissions)
                        {
                            if (userNode.Id == permission.GranteeId)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            NoAccessUser noAccessUser = new NoAccessUser();

                            var userParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);
                            var userDhPub = await _attributesRepository.GetFirstValueByNodeIdAndType(userNode.Id, (byte)AttributeType.UserDhPub);
                            var userType = await _attributesRepository.GetFirstValueByNodeIdAndType(userNode.Id, (byte)AttributeType.UserType);

                            noAccessUser.ID = ULongToHexString(userNode.Id);
                            noAccessUser.encNAM = userParent.NameEncByParentEncKey;
                            noAccessUser.dhPublicKEY = userDhPub;
                            noAccessUser.Type = userType;

                            response.uNoAccess.Add(noAccessUser);
                        }
                    }
                }
                else if (childNode.Type == (byte)NodeType.UsersFolder)
                {
                    var groupsFolderChildren = await _parentsRepository.GetChildrenByParentNodeId(childNode.Id);

                    foreach (var groupChild in groupsFolderChildren)
                    {
                        var groupNode = await _nodesRepository.GetByUlongIdAsync(groupChild.NodeId);
                        var groupStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(groupNode.Id, (byte)AttributeType.NodeStatus);

                        if (groupStatus.Equals(NodeStatus.Deleted))
                        {
                            continue;
                        }

                        bool found = false;
                        foreach (var permission in permissions)
                        {
                            if (groupNode.Id == permission.GranteeId)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            NoAccessGroup noAccessGroup = new NoAccessGroup();

                            var groupParent = await _parentsRepository.GetAsync(groupNode.CurrentParentId);
                            var groupDhPub = await _attributesRepository.GetFirstValueByNodeIdAndType(groupNode.Id, (byte)AttributeType.UserDhPub);

                            noAccessGroup.ID = ULongToHexString(groupNode.Id);
                            noAccessGroup.encNAM = groupParent.NameEncByParentEncKey;
                            noAccessGroup.dhPublicKEY = groupDhPub;

                            response.gNoAccess.Add(noAccessGroup);
                        }
                    }

                }
            }

            return response;
        }

        public async Task<ListUsersResponse> ListUsers(ListUsersRequest request, string blockchainId, string timestamp, string transactionId)
        {
            return await ListUsersResponseCreation(request.TYP, request.Deleted, blockchainId, transactionId);
        }

        private async Task<ListUsersResponse> ListUsersResponseCreation(string requestType, bool showDeleted, string blockchainId, string transactionId)
        {
            ListUsersResponse response = new ListUsersResponse();

            response.TYP = requestType;
            response.tID = transactionId;

            var blockchainChildren = await _parentsRepository.GetChildrenByParentNodeId(HexStringToULong(blockchainId));

            foreach (var blockchainChild in blockchainChildren)
            {
                var childNode = await _nodesRepository.GetByUlongIdAsync(blockchainChild.NodeId);

                if (childNode.Type == (byte)NodeType.UsersFolder)
                {
                    var usersFolderChildren = await _parentsRepository.GetChildrenByParentNodeId(childNode.Id);

                    foreach (var usersFolderChild in usersFolderChildren)
                    {
                        var userNode = await _nodesRepository.GetByUlongIdAsync(usersFolderChild.NodeId);
                        var userStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(userNode.Id, (byte)AttributeType.NodeStatus);
                        var userAudit = await _auditsRepository.GetMostRecentByNodeId(userNode.Id);

                        if (!showDeleted)
                        {
                            if (userStatus.Equals(NodeStatus.Deleted))
                            {
                                continue;
                            }
                        }

                        UserObject user = new UserObject();

                        user.ID = ULongToHexString(userNode.Id);
                        user.NameEncByParentEncKey = usersFolderChild.NameEncByParentEncKey;
                        user.LastModified = userAudit.Timestamp.ToString(cultureInfo);

                        var userTypeAttributeValue = await _attributesRepository.GetFirstValueByNodeIdAndType(userNode.Id, (byte)AttributeType.UserType);

                        user.UserType = userTypeAttributeValue;

                        var groupPermissions = await _permissionsRepository.GetGroupsByGranteeId(userNode.Id);

                        user.NoOfGroupsIn = groupPermissions.Count;

                        foreach (var groupPermission in groupPermissions)
                        {
                            var groupStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(groupPermission.NodeId, (byte)AttributeType.NodeStatus);

                            if (groupStatus.Equals(NodeStatus.Deleted))
                            {
                                continue;
                            }

                            var groupNode = await _nodesRepository.GetByUlongIdAsync(groupPermission.NodeId);
                            var groupParent = await _parentsRepository.GetAsync(groupNode.CurrentParentId);

                            GroupDetailsObject group = new GroupDetailsObject();

                            group.ID = ULongToHexString(groupNode.Id);
                            group.NameEncByParentEncKey = groupParent.NameEncByParentEncKey;

                            user.Groups.Add(group);
                        }

                        user.Status = userStatus;

                        response.Users.Add(user);
                    }

                    break;
                }
            }

            return response;
        }

        public async Task<LoginResponse> Login(ulong deviceId, string timestamp, string transactionId)
        {

            ulong userNodeId = await GetUserNodeIdByDeviceNodeId(deviceId);
            ulong orgNodeId = await GetOrgNodeIdByUserNodeId(userNodeId);

            var userDhPub = await _attributesRepository.GetFirstValueByNodeIdAndType(userNodeId, (byte)AttributeType.UserDhPub);
            var userDhPrivWrapByDeriveDeviceDhPubUserDhPriv = await _attributesRepository.GetFirstValueByNodeIdAndType(deviceId, (byte)AttributeType.UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv);

            var userNode = await _nodesRepository.GetByUlongIdAsync(userNodeId);
            var userParent = await _parentsRepository.GetAsync(userNode.CurrentParentId);
            var usersNode = await _nodesRepository.GetByUlongIdAsync(userParent.ParentNodeId);
            var usersParent = await _parentsRepository.GetAsync(usersNode.CurrentParentId);


            var groupsFolderNode = await GetGroupsFolderNodeByOrgId(orgNodeId);

            var orgNode = await _nodesRepository.GetByUlongIdAsync(orgNodeId);

            var publicBlockchain = await _publicBlockchainsRepository.GetByOrgNodeIdAsync(orgNodeId);
            var blockchainId = this.ULongToHexString(publicBlockchain.Id);

            var originalDeviceStatus = await _attributesRepository.GetFirstValueByNodeIdAndType(
                     deviceId,
                     (byte)AttributeType.OriginalDevice);

            var loginAudit = await _auditsRepository.GetLatestByUserIdAndType(userNodeId, AuditType.Login);

            await CreateAudit(AuditType.Login, userNodeId, orgNodeId, timestamp);

            LoginResponse response = new LoginResponse();

            if (loginAudit != null)
            {
                response.LastLoginTime = loginAudit.Timestamp.ToString(cultureInfo);
            }

            response.tID = transactionId;
            response.TYP = BaseRequest.Login;

            response.UserID = ULongToHexString(userNodeId);
            response.UserDhPub = userDhPub;
            response.UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv = userDhPrivWrapByDeriveDeviceDhPubUserDhPriv;
            response.UserNameEncByParentEncKey = userParent.NameEncByParentEncKey;

            if (originalDeviceStatus.Equals(OriginalDeviceStatus.True))
            {
                response.OrgNodeAesWrapByDeriveUserDhPubDeviceDhPriv = orgNode.CurrentNodeAesWrapByParentNodeAes;
                response.OrgNodeAesWrapByDeriveUserDhPrivDeviceDhPub = null;
            }
            else
            {
                var orgNodeAesWrapByDeriveUserDhPrivDeviceDhPub =
                    await _attributesRepository.GetFirstValueByNodeIdAndType(deviceId,
                    (byte)AttributeType.OrgNodeAesWrapByDeriveUserDhPrivDeviceDhPub);

                response.OrgNodeAesWrapByDeriveUserDhPubDeviceDhPriv = null;
                response.OrgNodeAesWrapByDeriveUserDhPrivDeviceDhPub = orgNodeAesWrapByDeriveUserDhPrivDeviceDhPub;
            }

            response.UsersNodeAesWrapByChainNodeAes = usersNode.CurrentNodeAesWrapByParentNodeAes;
            response.ChainNodeAesWrapByOrgNodeAes = usersParent.NodeKeyWrappedByParentNodeKey;

            // if a user has a permission on the Users folder from a granter then use this key
            // instead of usersNode.CurrentNodeAesWrapByParentNodeAes
            // and get also GranterDhPub
            var userPermissionInUsers = await _permissionsRepository.GetByNodeIdAndGranteeId(usersNode.Id, userNodeId);
            if (userPermissionInUsers != null)
            {
                response.UsersAesWrapByDeriveGranteeDhPubGranterDhPriv =
                    userPermissionInUsers.NodeAesWrapByDeriveGranteeDhPubGranterDhPriv;

                ulong granterNodeId = userPermissionInUsers.UserNodeId;

                var granterDhPub =
                    await _attributesRepository.GetFirstValueByNodeIdAndType(granterNodeId,
                                                                            (byte)AttributeType.UserDhPub);

                response.GranterDhPub = granterDhPub;
            }
            else
            {
                response.UsersAesWrapByDeriveGranteeDhPubGranterDhPriv = null;
                response.GranterDhPub = null;
            }

            response.UserNodeAesWrapByUsersNodeAes = userNode.CurrentNodeAesWrapByParentNodeAes;
            // check if the group folder node is null
            if (groupsFolderNode != null)
            {
                response.GroupsNodeAesWrapByChainNodeAes = groupsFolderNode.CurrentNodeAesWrapByParentNodeAes;

                var userPermissionInGroups = await _permissionsRepository.GetByNodeIdAndGranteeId(groupsFolderNode.Id, userNodeId);
                if (userPermissionInGroups != null)
                {
                    response.GroupsAesWrapByDeriveGranteeDhPubGranterDhPriv =
                        userPermissionInGroups.NodeAesWrapByDeriveGranteeDhPubGranterDhPriv;
                }
            }
            else
            {
                response.GroupsNodeAesWrapByChainNodeAes = null;
            }

            response.BlockchainID = blockchainId;

            return response;
        }




        /// <summary>
        /// Moves a file/folder. 
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>ListFilesResponse</returns>
        public async Task<ListFilesResponse> Move(MoveRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {

                ulong movedNodeId = HexStringToULong(request.ID);
                ulong newParentId = HexStringToULong(request.pID);
                string encKey = request.encKEY;
                string newEncFileName = request.encNAM;

                var movedNode = await _nodesRepository.GetByUlongIdAsync(movedNodeId);
                var currentParent = await _parentsRepository.GetAsync(movedNode.CurrentParentId);

                currentParent.CurrentParent = false;
                await _parentsRepository.UpdateAsync(currentParent);

                var newParentNode = await _nodesRepository.GetByUlongIdAsync(newParentId);
                var newParent = await CreateParent(newEncFileName, newParentNode.Id, movedNode.Id, encKey, userId, timestamp);

                movedNode.CurrentParentId = newParent.Id;
                movedNode.CurrentNodeAesWrapByParentNodeAes = encKey;

                await _nodesRepository.UpdateAsync(movedNode);

                var newAuditRecord = await CreateAudit(AuditType.Move, userId, movedNodeId, timestamp);

                var response = await ListFolderContents(newParentId, userId, BaseRequest.Move, transactionId);

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        /// <summary>
        /// Opens a File.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns><OpenFileResponse/returns>
        public async Task<OpenFileResponse> OpenFile(OpenFileRequest request, ulong userId, string timestamp)
        {
            try
            {

                var fileNode = await _nodesRepository.GetByUlongIdAsync(HexStringToULong(request.ID));

                OpenFileResponse response = new OpenFileResponse();
                response.ShardsPacket = Servers.Instance.DownloadFileShards(ULongToHexString(fileNode.CurrentVersionId));

                var newAuditRecord = await CreateAudit(AuditType.Read, userId, fileNode.CurrentVersionId, timestamp);

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Opens a previous Version of a file.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>OpenFileResponse</returns>
        public async Task<OpenFileResponse> OpenFilePreviousVersion(OpenFilePreviousVersionRequest request, ulong userId, string timestamp)
        {
            try
            {

                OpenFileResponse response = new OpenFileResponse();
                response.ShardsPacket = Servers.Instance.DownloadFileShards(request.ID);

                var newAuditRecord = await CreateAudit(AuditType.Read, userId, HexStringToULong(request.ID), timestamp);

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }



        public async Task<RegisterDeviceResponse> RegisterDevice(RegisterDeviceRequest request,
                                                                 ulong deviceId,
                                                                 string timestamp,
                                                                 string transactionId)
        {
            try
            {

                ulong userNodeId = await GetUserNodeIdByDeviceNodeId(deviceId);

                // Update Private DB...               

                // Update Attributes table         

                await CreateAttribute(deviceId, AttributeType.DeviceDsaPub, request.DeviceDsaPub, userNodeId, timestamp);
                await CreateAttribute(deviceId, AttributeType.DeviceDhPub, request.DeviceDhPub, userNodeId, timestamp);

                // update new device status to inactive
                var deviceAttribute = await _attributesRepository.GetFirstByNodeIdAndType(deviceId, (byte)AttributeType.NodeStatus);

                deviceAttribute.AttributeValue = NodeStatus.Inactive;

                await _attributesRepository.UpdateAsync(deviceAttribute);

                // create Audit record - audits table               
                await CreateAudit(AuditType.Register, userNodeId, deviceId, timestamp);

                ////////////////////////
                // Delete Code...
                ////////////////////////
                var codeAttibute = await _attributesRepository.GetByAttributeValue(request.Code);
                await _attributesRepository.DeleteByUlongIdAsync(codeAttibute.Id);

                RegisterDeviceResponse response = new RegisterDeviceResponse();

                response.TYP = BaseRequest.RegisterDevice;
                response.UserID = ULongToHexString(userNodeId);
                response.DeviceID = ULongToHexString(deviceId);
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="timestamp"></param>
        /// <param name="blockchainId"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public async Task<RegisterOrgResponse> RegisterOrg(RegisterOrgRequest request,
                                                         string timestamp,
                                                         string blockchainId,
                                                         string transactionId)
        {
            try
            {
                ulong orgId = HexStringToULong(request.OrgID);
                ulong usersFolderId = HexStringToULong(request.UsersID);
                ulong userId = HexStringToULong(request.UserID);
                ulong deviceId = HexStringToULong(request.DeviceID);
                ulong chainId = HexStringToULong(blockchainId);
                ulong groupsFolderId = HexStringToULong(request.GroupsID);

                // Update Private DB...

                // create Org node
                var newOrgParent = await CreateParent(request.OrgName, null, orgId, null, userId, timestamp);

                var newOrgNode = await CreateNode(orgId,
                                                  NodeType.Org,
                                                  newOrgParent.Id,
                                                  null,
                                                  userId,
                                                  request.OrgNodeAesWrapByDeriveUserDhPubDeviceDhPriv,
                                                  userId,
                                                  timestamp);


                // Create Blockchain node
                var newBlockchainParent = await CreateParent(request.ChainName,
                                              orgId,
                                              chainId,
                                              request.OrgNodeAesWrapByDeriveUserDhPubDeviceDhPriv,
                                              userId,
                                              timestamp);

                var newBlockchainNode = await CreateNode(chainId,
                                                  NodeType.Blockchain,
                                                  newBlockchainParent.Id,
                                                  null,
                                                  userId,
                                                  request.ChainNodeAesWrapByOrgNodeAes,
                                                  userId,
                                                  timestamp);


                // create Users folder node
                var newUsersFolderParent = await CreateParent(request.UsersName,
                                                              chainId,
                                                              usersFolderId,
                                                              request.ChainNodeAesWrapByOrgNodeAes,
                                                              userId,
                                                              timestamp);

                var newUsersFolderNode = await CreateNode(usersFolderId,
                                                  NodeType.UsersFolder,
                                                  newUsersFolderParent.Id,
                                                  null,
                                                  userId,
                                                  request.UsersNodeAesWrapByChainNodeAes,
                                                  userId,
                                                  timestamp);

                // create Groups folder node
                var newGroupsFolderParent = await CreateParent(request.GroupsName,
                                                              chainId,
                                                              groupsFolderId,
                                                              request.ChainNodeAesWrapByOrgNodeAes,
                                                              userId,
                                                              timestamp);

                var newGroupsFolderNode = await CreateNode(groupsFolderId,
                                                  NodeType.GroupsFolder,
                                                  newGroupsFolderParent.Id,
                                                  null,
                                                  userId,
                                                  request.GroupsNodeAesWrapByChainNodeAes,
                                                  userId,
                                                  timestamp);

                // create User node record

                var newUserParent = await CreateParent(request.UserName, usersFolderId,
                                                       userId,
                                                       request.UsersNodeAesWrapByChainNodeAes,
                                                       userId,
                                                       timestamp);

                var newUserNode = await CreateNode(userId,
                                                   NodeType.User,
                                                   newUserParent.Id,
                                                   null,
                                                   userId,
                                                   request.UserNodeAesWrapByUsersNodeAes,
                                                   userId,
                                                   timestamp);

                // create Device node
                var newDevParent = await CreateParent(request.DeviceName,
                                                    userId,
                                                    deviceId,
                                                    request.UserNodeAesWrapByUsersNodeAes,
                                                    userId,
                                                    timestamp);

                var newDevNode = await CreateNode(deviceId,
                                                   NodeType.Device,
                                                   newDevParent.Id,
                                                   null,
                                                   userId,
                                                   request.DeviceNodeAesWrapByUserNodeAes,
                                                   userId,
                                                   timestamp);

                // Update Attributes table         
                await CreateAttribute(userId, AttributeType.UserDhPub, request.UserDhPub, userId, timestamp);
                await CreateAttribute(userId, AttributeType.UserType, UserType.Internal, userId, timestamp);

                await CreateAttribute(deviceId, AttributeType.DeviceDsaPub, request.DeviceDsaPub, userId, timestamp);
                await CreateAttribute(deviceId, AttributeType.DeviceDhPub, request.DeviceDhPub, userId, timestamp);
                await CreateAttribute(deviceId,
                                      AttributeType.UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv,
                                      request.UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv,
                                      userId,
                                      timestamp);
                await CreateAttribute(deviceId, AttributeType.OriginalDevice, OriginalDeviceStatus.True, userId, timestamp);

                await CreateAttribute(deviceId, AttributeType.NodeStatus, NodeStatus.Active, userId, timestamp);
                await CreateAttribute(userId, AttributeType.NodeStatus, NodeStatus.Active, userId, timestamp);

                // grant access to Users folder
                ulong flags = (ulong)PermissionType.Create | (ulong)PermissionType.Read |
                              (ulong)PermissionType.Update | (ulong)PermissionType.Delete |
                              (ulong)PermissionType.Review | (ulong)PermissionType.Approve |
                              (ulong)PermissionType.Release | (ulong)PermissionType.Share;

                await CreatePermission(flags,
                                       userId,
                                       userId,
                                       request.UsersAesWrapByDeriveGranteeDhPubGranterDhPriv,
                                       false,
                                       usersFolderId,
                                       timestamp);

                // grant access to User folder 
                await CreatePermission(flags,
                                      userId,
                                      userId,
                                      request.UserNodeAesWrapByUsersNodeAes,
                                      false,
                                      userId,
                                      timestamp);

                // grant access to Groups folder
                await CreatePermission(flags,
                        userId,
                        userId,
                        request.GroupsAesWrapByDeriveGranteeDhPubGranterDhPriv,
                        false,
                        groupsFolderId,
                        timestamp);

                // create Audit record - audits table
                await CreateAudit(AuditType.Register, userId, orgId, timestamp);
                await CreateAudit(AuditType.Create, userId, userId, timestamp);
                await CreateAudit(AuditType.Create, userId, deviceId, timestamp);
                await CreateAudit(AuditType.Create, userId, chainId, timestamp);

                // Upadate Public DB...

                var newPublicUser = await CreatePublicUser(userId, orgId, deviceId, userId, timestamp);
                var newPublicEmail = await CreatePublicEmail(userId, request.UserName, timestamp);
                var newPublicOrg = await CreatePublicOrg(orgId, request.OrgName, userId, timestamp);
                var newBlockchain = await CreatePublicBlockchain(chainId, orgId, request.OrgName);

                RegisterOrgResponse response = new RegisterOrgResponse();

                response.TYP = BaseRequest.RegisterOrg;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<RegisterUserAndDeviceResponse> RegisterUserAndDevice(RegisterUserAndDeviceRequest request,
                                                                               ulong userId,
                                                                               ulong deviceId,
                                                                               string timestamp,
                                                                               string blockchainId,
                                                                               string transactionId)
        {
            try
            {

                //ulong activatedUserNodeId = await GetUserNodeIdByDeviceNodeId(deviceId);
                ulong orgNodeId = await GetOrgNodeIdByUserNodeId(userId);


                // Update Private DB...               

                // Update Attributes table
                await CreateAttribute(userId, AttributeType.UserType, UserType.Internal, userId, timestamp);

                await CreateAttribute(userId, AttributeType.UserDhPub, request.UserDhPub, userId, timestamp);

                await CreateAttribute(deviceId, AttributeType.DeviceDsaPub, request.DeviceDsaPub, userId, timestamp);
                await CreateAttribute(deviceId, AttributeType.DeviceDhPub, request.DeviceDhPub, userId, timestamp);
                await CreateAttribute(deviceId,
                                      AttributeType.UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv,
                                      request.UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv,
                                      userId,
                                      timestamp);

                // update  user status to Inactive from Pending
                var activatedUserAttribute = await _attributesRepository.GetFirstByNodeIdAndType(userId, (byte)AttributeType.NodeStatus);

                activatedUserAttribute.AttributeValue = NodeStatus.Inactive;

                await _attributesRepository.UpdateAsync(activatedUserAttribute);

                // create Audit record - audits table               
                await CreateAudit(AuditType.Register, userId, orgNodeId, timestamp);
                await CreateAudit(AuditType.Register, userId, userId, timestamp);

                ////////////////////////
                // Delete Code...
                ////////////////////////
                var codeAttibute = await _attributesRepository.GetByAttributeValue(request.Code);
                await _attributesRepository.DeleteByUlongIdAsync(codeAttibute.Id);

                RegisterUserAndDeviceResponse response = new RegisterUserAndDeviceResponse();

                response.TYP = BaseRequest.RegisterUserAndDevice;
                response.OrgID = ULongToHexString(orgNodeId);
                response.UserID = ULongToHexString(userId);
                response.DeviceID = ULongToHexString(deviceId);
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RegisterUserAndDevice Exception caught: {ex}");
                throw;
            }
        }

        public async Task<RemoveFileTagResponse> RemoveFileTag(RemoveFileTagRequest request, ulong userId, string timestamp, string transactionId)
        {
            ulong fileId = this.HexStringToULong(request.ID);

            var fileParent = await _parentsRepository.GetCurrentParentByNodeId(fileId);

            var attribute = await _attributesRepository.GetByNodeIdAndAttributeValue(fileId, request.tag.ToLower());

            RemoveFileTagResponse response = new RemoveFileTagResponse();
            response.TYP = request.TYP;
            response.tID = transactionId;
            response.ID = request.ID;

            if (attribute != null)
            {
                await _attributesRepository.DeleteByUlongIdAsync(attribute.Id);
                await CreateAudit(AuditType.RemoveFileTag, userId, fileId, timestamp);
                response.encNAM = fileParent.NameEncByParentEncKey;
                response.success = true;
            }
            else
            {
                response.encNAM = fileParent.NameEncByParentEncKey;
                response.success = false;
            }

            return response;
        }

        public async Task<RemovePermissionResponse> RemovePermission(RemovePermissionsRequest request,
                                                                       ulong userId,
                                                                       string timestamp,
                                                                       string transactionId)
        {
            ulong granteeId = HexStringToULong(request.granteeID);
            ulong nodeId = HexStringToULong(request.nodeID);
            ulong flags = request.flags;

            var permission = await _permissionsRepository.GetByNodeIdAndGranteeId(nodeId, granteeId);
            ulong switchedOffFlags = permission.Flags & ~flags;

            permission.Flags = flags;

            await _permissionsRepository.UpdateAsync(permission);



            RemovePermissionResponse response = new RemovePermissionResponse();

            response.TYP = request.TYP;
            response.tID = transactionId;

            return response;
        }

        public async Task<RemoveUserFromGroupResponse> RemoveUserFromGroup(RemoveUserFromGroupRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {
                ulong groupMemberId = HexStringToULong(request.uID);
                ulong groupId = HexStringToULong(request.ID);

                var permission = await _permissionsRepository.GetByNodeIdAndGranteeId(groupId, groupMemberId);

                permission.Revoked = true;

                await _permissionsRepository.UpdateAsync(permission);

                var newAuditRecord = await CreateAudit(AuditType.RemoveUserFromGroup, userId, groupMemberId, timestamp);

                RemoveUserFromGroupResponse response = new RemoveUserFromGroupResponse();
                response.TYP = BaseRequest.RemoveUserFromGroup;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }


        /// <summary>
        /// Renames a file/folder.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>ListFilesResponse</returns>
        public async Task<ListFilesResponse> Rename(RenameRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {
                ulong renamedNodeId = HexStringToULong(request.ID);
                string newFileName = request.encNAM;

                var renamedNode = await _nodesRepository.GetByUlongIdAsync(renamedNodeId);
                var currentParent = await _parentsRepository.GetAsync(renamedNode.CurrentParentId);
                var parentNode = await _nodesRepository.GetByUlongIdAsync(currentParent.ParentNodeId);

                currentParent.CurrentParent = false;
                await _parentsRepository.UpdateAsync(currentParent);

                var newParent = await CreateParent(newFileName, currentParent.ParentNodeId, renamedNodeId, renamedNode.CurrentNodeAesWrapByParentNodeAes, userId, timestamp);

                renamedNode.CurrentParentId = newParent.Id;

                await _nodesRepository.UpdateAsync(renamedNode);

                var newAuditRecord = await CreateAudit(AuditType.Rename, userId, renamedNodeId, timestamp);

                var response = await ListFolderContents(parentNode.Id, userId, BaseRequest.Rename, transactionId);

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Remove file/folder from an access rTYP list (authors, approvers, etc) by updating the Permissions table.
        /// </summary>
        /// <param fileParent="request"></param>
        /// <param fileParent="creatorId"></param>
        /// <param fileParent="timestamp"></param>
        /// <returns>GenericResponse</returns>
        public async Task<RevokeAccessResponse> RevokeAccess(RevokeAccessRequest request, ulong userId, string timestamp, string transactionId)
        {
            try
            {

                var permission = await _permissionsRepository.GetByNodeIdAndGranteeId(HexStringToULong(request.nodeID), HexStringToULong(request.granteeID));

                permission.Revoked = true;

                await _permissionsRepository.UpdateAsync(permission);

                RevokeAccessResponse response = new RevokeAccessResponse();
                response.TYP = BaseRequest.RevokeAccess;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<RevokeDeviceResponse> RevokeDevice(RevokeDeviceRequest request, ulong userId, string timestamp, string blockchainId, string transactionId)
        {
            ulong deviceId = HexStringToULong(request.DeviceID);

            var deviceAttribute = await _attributesRepository.GetFirstByNodeIdAndType(deviceId, (byte)AttributeType.NodeStatus);

            deviceAttribute.AttributeValue = NodeStatus.Revoked;

            /*
             * 
             * De we need to remove the keys????
             * 
             */
            await _attributesRepository.UpdateAsync(deviceAttribute);

            await CreateAudit(AuditType.Revoke, userId, deviceId, timestamp);

            RevokeDeviceResponse response = new RevokeDeviceResponse();

            response.TYP = BaseRequest.RevokeDevice;
            response.tID = transactionId;

            return response;
        }

        public async Task SendEmail()
        {
            await SendGridTest();
        }

        private async Task SendGridTest()
        {
            var apiKey = _configuration.GetValue<string>("SendGrid:SENDGRID_API_KEY");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("mattbodden@googlemail.com", "Matt Bodden");
            var subject = "Sending with SendGrid is Hard with hidden values";
            var to = new EmailAddress("dimitri_zervas@hotmail.com", "Dimitrios Zervas");
            var plainTextContent = "and easy to do anywhere, even with C#";
            var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }

        public async Task<SendNewUserAndDeviceCodeResponse> SendNewUserAndDeviceCode(SendNewUserAndDeviceCodeRequest request,
                                                                        string timestamp,
                                                                        string blockchainId,
                                                                        string transactionId)
        {
            try
            {
                ulong deviceId = HexStringToULong(request.DeviceID);

                SendNewUserAndDeviceCodeResponse response = new SendNewUserAndDeviceCodeResponse();

                response.TYP = BaseRequest.SendNewUserAndDeviceCode;
                response.tID = transactionId;

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError($"Exception caught: {ex}");
                throw;
            }
        }

        public async Task<SharedWithResponse> SharedWith(SharedWithRequest request,
                                                         ulong userId,
                                                         string blockchainId,
                                                         string timestamp,
                                                         string transactionId)
        {
            var groupsFolder = await GetGroupsNodeByChainNodeId(blockchainId);
            var usersFolder = await GetUsersNodeByChainNodeId(blockchainId);

            ulong nodeId = HexStringToULong(request.ID);

            SharedWithResponse response = new SharedWithResponse();

            response.TYP = request.TYP;
            response.tID = transactionId;

            response.ID = request.ID;
            response.encGroupsFolderKEY = groupsFolder.CurrentNodeAesWrapByParentNodeAes;
            response.encUsersFolderKEY = usersFolder.CurrentNodeAesWrapByParentNodeAes;

            var permissions = await _permissionsRepository.GetPermissionsByNodeId(nodeId);

            foreach (var permission in permissions)
            {
                var granteeNode = await _nodesRepository.GetByUlongIdAsync(permission.GranteeId);
                var granteeParent = await _parentsRepository.GetAsync(granteeNode.CurrentParentId);

                WithAccess withAccess = new WithAccess();

                withAccess.ID = ULongToHexString(granteeNode.Id);
                withAccess.encNAM = granteeParent.NameEncByParentEncKey;

                if ((permission.Flags & (ulong)PermissionType.Create) != 0)
                {
                    withAccess.permissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Create), (ulong)PermissionType.Create));
                }
                if ((permission.Flags & (ulong)PermissionType.Read) != 0)
                {
                    withAccess.permissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Read), (ulong)PermissionType.Read));
                }
                if ((permission.Flags & (ulong)PermissionType.Update) != 0)
                {
                    withAccess.permissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Update), (ulong)PermissionType.Update));
                }
                if ((permission.Flags & (ulong)PermissionType.Delete) != 0)
                {
                    withAccess.permissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Delete), (ulong)PermissionType.Delete));
                }
                if ((permission.Flags & (ulong)PermissionType.Review) != 0)
                {
                    withAccess.permissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Review), (ulong)PermissionType.Review));
                }
                if ((permission.Flags & (ulong)PermissionType.Approve) != 0)
                {
                    withAccess.permissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Approve), (ulong)PermissionType.Approve));
                }
                if ((permission.Flags & (ulong)PermissionType.Release) != 0)
                {
                    withAccess.permissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Release), (ulong)PermissionType.Release));
                }
                if ((permission.Flags & (ulong)PermissionType.Share) != 0)
                {
                    withAccess.permissionTypes.Add(new PermissionTypeObject(nameof(PermissionType.Share), (ulong)PermissionType.Share));
                }

                // get grantee's inherited permissions
                if (await HaveInheritedPermission(nodeId, granteeNode.Id, PermissionType.Create))
                {
                    withAccess.inheritedPermissions.Add(new PermissionTypeObject(nameof(PermissionType.Create), (ulong)PermissionType.Create));
                }
                if (await HaveInheritedPermission(nodeId, granteeNode.Id, PermissionType.Read))
                {
                    withAccess.inheritedPermissions.Add(new PermissionTypeObject(nameof(PermissionType.Read), (ulong)PermissionType.Read));
                }
                if (await HaveInheritedPermission(nodeId, granteeNode.Id, PermissionType.Update))
                {
                    withAccess.inheritedPermissions.Add(new PermissionTypeObject(nameof(PermissionType.Update), (ulong)PermissionType.Update));
                }
                if (await HaveInheritedPermission(nodeId, granteeNode.Id, PermissionType.Delete))
                {
                    withAccess.inheritedPermissions.Add(new PermissionTypeObject(nameof(PermissionType.Delete), (ulong)PermissionType.Delete));
                }
                if (await HaveInheritedPermission(nodeId, granteeNode.Id, PermissionType.Review))
                {
                    withAccess.inheritedPermissions.Add(new PermissionTypeObject(nameof(PermissionType.Review), (ulong)PermissionType.Review));
                }
                if (await HaveInheritedPermission(nodeId, granteeNode.Id, PermissionType.Approve))
                {
                    withAccess.inheritedPermissions.Add(new PermissionTypeObject(nameof(PermissionType.Approve), (ulong)PermissionType.Approve));
                }
                if (await HaveInheritedPermission(nodeId, granteeNode.Id, PermissionType.Release))
                {
                    withAccess.inheritedPermissions.Add(new PermissionTypeObject(nameof(PermissionType.Release), (ulong)PermissionType.Release));
                }
                if (await HaveInheritedPermission(nodeId, granteeNode.Id, PermissionType.Share))
                {
                    withAccess.inheritedPermissions.Add(new PermissionTypeObject(nameof(PermissionType.Share), (ulong)PermissionType.Share));
                }

                if (granteeNode.Type == (byte)NodeType.User)
                {
                    response.uWithAccess.Add(withAccess);
                }
                else if (granteeNode.Type == (byte)NodeType.Group)
                {
                    response.gWithAccess.Add(withAccess);
                }
            }

            return response;
        }
    }
}
