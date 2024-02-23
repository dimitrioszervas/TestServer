using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models.Private
{
    public enum AttributeType : byte
    {
        UserDhPub,
        DeviceDsaPub,
        DeviceDhPub,
        UserDhPrivWrapByDeriveDeviceDhPubUserDhPriv,
        NodeStatus,
        Code,
        UserType,
        Tag,
        GroupDhPub,
        EncGroupDhPriv,
        OriginalDevice,
        OrgNodeAesWrapByDeriveUserDhPrivDeviceDhPub,
        NameEncByGranteeNodeAes
    }

    public static class NodeStatus
    {
        public const string Inactive = "Inactive";
        public const string Active = "Active";
        public const string Revoked = "Revoked";
        public const string Deleted = "Deleted";
        public const string Pending = "Pending";
    }

    public static class UserType
    {
        public const string Internal = "Internal";
        public const string Customer = "Customer";
        public const string Supplier = "Supplier";
        public const string Special = "Special";
    }

    public static class OriginalDeviceStatus
    {
        public const string True = "True";
        public const string False = "False";
    }

    public class Attribute : BaseTable
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong Id { get; set; }
        public byte AttributeType { get; set; }
        public string AttributeValue { get; set; }
        public ulong NodeId { get; set; }
    }
}
