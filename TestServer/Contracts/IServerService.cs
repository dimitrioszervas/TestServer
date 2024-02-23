using TestServer.Server.Requests;
using TestServer.Server.Responses;
using Humanizer;

namespace TestServer.Contracts
{
    public interface IServerService
    {
        ulong HexStringToULong(string hexString);
        string ULongToHexString(ulong? num);

        Task<bool> VerifyTransaction(ulong deviceId, ulong userId, byte[] dataToVerify, string signature);
        Task<bool> VerifyTransaction(byte[] dataToVerify, string signature, string publicKey);
        Task<ActivateDeviceResponse> ActivateDevice(ActivateDeviceRequest request, string timestamp, string blockchainId, string transactionId, ulong originalDeviceId);
        Task<ActivateGroupResponse> ActivateGroup(ActivateGroupRequest request, string timestamp, string blockchainId, string transactionId, ulong userId);
        Task<ActivateUserResponse> ActivateUser(ActivateUserRequest request, string timestamp, string blockchainId, string transactionId, ulong userId);
        Task<AddFileTagResponse> AddFileTag(AddFileTagRequest request, ulong userId, string timestamp, string transactionId);
        Task<AddPermissionsResponse> AddPermissions(AddPermissionsRequest request, ulong userId, string timestamp, string transactionId);
        Task<AddReviewResponse> AddReview(AddReviewRequest request, ulong userId, string timestamp, string transactionId);
        Task<AddUserToGroupResponse> AddUserToGroup(AddUserToGroupRequest request, ulong userId, string timestamp, string transactionId);
        Task<ApprovalResponse> Approval(ApprovalRequest request, string timestamp, string transactionId);
        Task<ListFilesResponse> Copy(CopyRequest request, ulong userId, string timestamp, string transactionId);
        Task<CreateDeviceResponse> CreateDevice(CreateDeviceRequest request, string timestamp, string blockchainId, string transactionId);
        Task<UploadResponse> CreateFile(CreateFileRequest request, ulong userId, string timestamp, string transactionId);
        Task<ListFilesResponse> CreateFolder(CreateFolderRequest request, ulong userId, string timestamp, string transactionId);
        Task<CreateGroupResponse> CreateGroup(CreateGroupRequest request, ulong userId, string blockchainId, string timestamp, string transactionId);
        Task<CreateOrgResponse> CreateOrg(CreateOrgRequest request, string timestamp, string blockchainId, string transactionId);
        Task<CreateUserResponse> CreateUser(CreateUserRequest request, ulong userId, ulong? orgId, ulong certId, string timestamp);
        Task<ListUsersResponse> CreateUserAndDevice(CreateUserAndDeviceRequest request, ulong userId, string timestamp, string blockchainId, string transactionId, ulong currentDeviceId);
        Task<UploadResponse> CreateVersion(CreateVersionRequest request, ulong userId, string timestamp, string transactionId);
        Task<DeactivateDeviceResponse> DeactivateDevice(DeactivateDeviceRequest request, ulong userId, string timestamp, string blockchainId, string transactionId);
        Task<DeactivateGroupResponse> DeactivateGroup(DeactivateGroupRequest request, ulong userId, string timestamp, string blockchainId, string transactionId);
        Task<DeactivateUserResponse> DeactivateUser(DeactivateUserRequest request, ulong userId, string timestamp, string blockchainId, string transactionId);
        Task<ListFilesResponse> Delete(DeleteRequest request, ulong userId, string timestamp, string transactionId);
        Task<DeleteGroupResponse> DeleteGroup(DeleteGroupRequest request, ulong userId, string timestamp, string blockchainId, string transactionId);
        Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ulong userId, string timestamp, string blockchainId, string transactionId);
        Task<RegisterOrgResponse> RegisterOrg(RegisterOrgRequest request, string timestamp, string blockchainId, string transactionId);
        Task<GetFileActionsResponse> GetFileActions(GetFileActionsRequest request, string transactionId);
        Task<GetAttributeResponse> GetAttribute(GetAttributeRequest request, string transactionId);
        Task<GetFileInfoResponse> GetFileInfo(GetFileInfoRequest request, ulong userId, string transactionId);
        Task<GetGroupResponse> GetGroup(GetGroupRequest request, string transactionId);
        Task<ulong> GetNodeIdByCode(string code);
        Task<GetNodeKeyResponse> GetNodeKey(GetNodeKeyRequest request, ulong userId, string transactioId);
        Task<GetNumberOfNodesResponse> GetNumberOfNodes(GetNumberOfNodesRequest request);
        Task<ulong> GetOriginalDeviceNodeIdByUserNodeId(ulong userId);
        Task<ulong> GetOrgNodeIdByUserNodeId(ulong userNodeId);
        Task<GetPermissionTypesResponse> GetPermissionTypes(GetPermissionTypesRequest request, string transactionId);
        Task<OpenFileResponse> GetReview(GetReviewRequest request);
        Task<GetUserResponse> GetUser(GetUserRequest request, string transactionId);
        Task<GetUserActionsResponse> GetUserActions(GetUserActionsRequest request, string transactionId);
        Task<ulong> GetUserNodeIdByDeviceNodeId(ulong deviceId);
        Task<GetVersionListResponse> GetVersionList(GetVersionListRequest request, string transactionId);
        Task<GrantAccessResponse> GrantAccess(GrantAccessRequest request, ulong userId, string timestamp, string transactionId);
        Task<HasUserPermissionResponse> HasUserPermission(HasUserPermissionRequest request, ulong userId, string transactionId);
        Task<InviteUserResponse> InviteUser(InviteUserRequest request, ulong userId, string timestamp, string transactionId);
        Task<ListDevicesResponse> ListDevices(ListDevicesRequest request, ulong userId, ulong orgId, string timestamp, string transactionId);
        Task<ListFilesResponse> ListFiles(ListFilesRequest request, ulong userId, string transactionId);
        Task<ListGroupsResponse> ListGroups(ListGroupsRequest request, string blockchainId, string timestamp, string transactionId);
        Task<ListNoAccessResponse> ListNoAccess(ListNoAccessRequest request, ulong userId, string blockchainId, string timestamp, string transactionId);
        Task<ListUsersResponse> ListUsers(ListUsersRequest request, string blockchainId, string timestamp, string transactionId);
        Task<LoginResponse> Login(ulong deviceId, string timestamp, string transactionId);
        Task<ListFilesResponse> Move(MoveRequest request, ulong userId, string timestamp, string transactionId);
        Task<OpenFileResponse> OpenFile(OpenFileRequest request, ulong userId, string timestamp);
        Task<OpenFileResponse> OpenFilePreviousVersion(OpenFilePreviousVersionRequest request, ulong userId, string timestamp);
        Task<RegisterDeviceResponse> RegisterDevice(RegisterDeviceRequest request, ulong deviceId, string timestamp, string transactionId);
        Task<RegisterUserAndDeviceResponse> RegisterUserAndDevice(RegisterUserAndDeviceRequest request, ulong userId, ulong deviceId, string timestamp, string blockchainId, string transactionId);
        Task<RemoveFileTagResponse> RemoveFileTag(RemoveFileTagRequest request, ulong userId, string timestamp, string transactionId);
        Task<RemovePermissionResponse> RemovePermission(RemovePermissionsRequest request, ulong userId, string timestamp, string transactionId);
        Task<RemoveUserFromGroupResponse> RemoveUserFromGroup(RemoveUserFromGroupRequest request, ulong userId, string timestamp, string transactionId);
        Task<ListFilesResponse> Rename(RenameRequest request, ulong userId, string timestamp, string transactionId);
        Task<RevokeAccessResponse> RevokeAccess(RevokeAccessRequest request, ulong userId, string timestamp, string transactionId);
        Task<RevokeDeviceResponse> RevokeDevice(RevokeDeviceRequest request, ulong userId, string timestamp, string blockchainId, string transactionId);
        Task SendEmail();
        Task<SendNewUserAndDeviceCodeResponse> SendNewUserAndDeviceCode(SendNewUserAndDeviceCodeRequest request, string timestamp, string blockchainId, string transactionId);
        Task<SharedWithResponse> SharedWith(SharedWithRequest request, ulong userId, string blockchainId, string timestamp, string transactionId);
    }
}
