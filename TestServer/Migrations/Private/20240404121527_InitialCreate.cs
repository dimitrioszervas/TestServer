using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestServer.Migrations.Private
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attributes",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeType = table.Column<byte>(type: "tinyint", nullable: false),
                    AttributeValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Audits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    NodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InviteeEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Revoked = table.Column<bool>(type: "bit", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jsons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Attributes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jsons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    CurrentParentId = table.Column<int>(type: "int", nullable: true),
                    CurrentVersionId = table.Column<decimal>(type: "decimal(20,0)", nullable: true),
                    CurrentOwnerId = table.Column<decimal>(type: "decimal(20,0)", nullable: true),
                    CurrentNodeAesWrapByParentNodeAes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Owners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    EncNodeKey = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameEncByParentEncKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: true),
                    NodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    NodeKeyWrappedByParentNodeKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentParent = table.Column<bool>(type: "bit", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    PrivateBlockHash = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PublicBlockHash = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    wrapKEY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    encUnwrapKEY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Flags = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    GranteeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    NodeAesWrapByDeriveGranteeDhPubGranterDhPriv = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Revoked = table.Column<bool>(type: "bit", nullable: false),
                    NodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Versions",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    NodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Versions_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: true),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Nodes_UserId",
                        column: x => x.UserId,
                        principalTable: "Nodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Approvals",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VersionId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    UserNodeId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Approvals_Versions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "Versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_VersionId",
                table: "Approvals",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId",
                table: "GroupMembers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_NodeId",
                table: "Groups",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_NodeId",
                table: "Permissions",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Versions_NodeId",
                table: "Versions",
                column: "NodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Approvals");

            migrationBuilder.DropTable(
                name: "Attributes");

            migrationBuilder.DropTable(
                name: "Audits");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "Jsons");

            migrationBuilder.DropTable(
                name: "Owners");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Seals");

            migrationBuilder.DropTable(
                name: "Versions");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Nodes");
        }
    }
}
