using AutoMapper;
using TestServer.Dtos.Private;
using TestServer.Dtos.Public;
using TestServer.Dtos.Server;
using TestServer.Models.Private;
using TestServer.Models.Public;
using TestServer.Server;

namespace TestServer.Configurations
{
    public class MapperConfig : Profile
    {
        public MapperConfig()
        {
            // Private DB
            CreateMap<Approval, ApprovalDto>().ReverseMap();
            CreateMap<Models.Private.Attribute, AttributeDto>().ReverseMap();
            CreateMap<Audit, AuditDto>().ReverseMap();
            CreateMap<Node, NodeDto>().ReverseMap();
            CreateMap<Parent, ParentDto>().ReverseMap();
            CreateMap<Models.Private.Version, VersionDto>().ReverseMap();
            CreateMap<Invitation, InvitationDto>().ReverseMap();
            CreateMap<Owner, OwnerDto>().ReverseMap();
            CreateMap<Permission, PermissionDto>().ReverseMap();
            CreateMap<Group, GroupDto>().ReverseMap();
            CreateMap<GroupMember, GroupMemberDto>().ReverseMap();
            CreateMap<Json, JsonDto>().ReverseMap();
            CreateMap<Models.Private.Seal, Dtos.Private.SealDto>().ReverseMap();

            // Public DB
            CreateMap<PublicUser, PublicUserDto>().ReverseMap();
            CreateMap<Org, OrgDto>().ReverseMap();
            CreateMap<Models.Public.Blockchain, BlockchainDto>().ReverseMap();
            CreateMap<Domain, DomainDto>().ReverseMap();
            CreateMap<Email, EmailDto>().ReverseMap();
            CreateMap<Invite, InviteDto>().ReverseMap();
            CreateMap<Models.Public.Seal, Dtos.Public.SealDto>().ReverseMap();
            CreateMap<SealTransaction, SealTransactionDto>().ReverseMap();

            // Server
            CreateMap<ShardsPacket, ShardsPacketDto>().ReverseMap();
        }
    }
}
