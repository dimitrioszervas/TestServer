﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TestServer.Models;

#nullable disable

namespace TestServer.Migrations.Private
{
    [DbContext(typeof(EndocloudDbContext))]
    [Migration("20240404121527_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("TestServer.Models.Private.Approval", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("Comment")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("VersionId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("VersionId");

                    b.ToTable("Approvals");
                });

            modelBuilder.Entity("TestServer.Models.Private.Attribute", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(20,0)");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<decimal>("Id"));

                    b.Property<byte>("AttributeType")
                        .HasColumnType("tinyint");

                    b.Property<string>("AttributeValue")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("NodeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Attributes");
                });

            modelBuilder.Entity("TestServer.Models.Private.Audit", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("NodeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Audits");
                });

            modelBuilder.Entity("TestServer.Models.Private.Group", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Deleted")
                        .HasColumnType("bit");

                    b.Property<decimal>("NodeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("encUnwrapKEY")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("wrapKEY")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("NodeId");

                    b.ToTable("Groups");
                });

            modelBuilder.Entity("TestServer.Models.Private.GroupMember", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("GroupId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal?>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GroupId");

                    b.HasIndex("UserId");

                    b.ToTable("GroupMembers");
                });

            modelBuilder.Entity("TestServer.Models.Private.Invitation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("InviteeEmail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Revoked")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Invitations");
                });

            modelBuilder.Entity("TestServer.Models.Private.Json", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Attributes")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Jsons");
                });

            modelBuilder.Entity("TestServer.Models.Private.Node", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("CurrentNodeAesWrapByParentNodeAes")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("CurrentOwnerId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<int?>("CurrentParentId")
                        .HasColumnType("int");

                    b.Property<decimal?>("CurrentVersionId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Nodes");
                });

            modelBuilder.Entity("TestServer.Models.Private.Owner", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<byte[]>("EncNodeKey")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<decimal>("NodeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Owners");
                });

            modelBuilder.Entity("TestServer.Models.Private.Parent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("CurrentParent")
                        .HasColumnType("bit");

                    b.Property<string>("NameEncByParentEncKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("NodeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("NodeKeyWrappedByParentNodeKey")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("ParentNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Parents");
                });

            modelBuilder.Entity("TestServer.Models.Private.Permission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Flags")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("GranteeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("NodeAesWrapByDeriveGranteeDhPubGranterDhPriv")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("NodeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<bool>("Revoked")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("NodeId");

                    b.ToTable("Permissions");
                });

            modelBuilder.Entity("TestServer.Models.Private.Seal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<byte[]>("PrivateBlockHash")
                        .HasColumnType("varbinary(max)");

                    b.Property<byte[]>("PublicBlockHash")
                        .HasColumnType("varbinary(max)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Seals");
                });

            modelBuilder.Entity("TestServer.Models.Private.Version", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("NodeId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<long>("Size")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserNodeId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("NodeId");

                    b.ToTable("Versions");
                });

            modelBuilder.Entity("TestServer.Models.Private.Approval", b =>
                {
                    b.HasOne("TestServer.Models.Private.Version", "Version")
                        .WithMany()
                        .HasForeignKey("VersionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Version");
                });

            modelBuilder.Entity("TestServer.Models.Private.Group", b =>
                {
                    b.HasOne("TestServer.Models.Private.Node", "Node")
                        .WithMany()
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Node");
                });

            modelBuilder.Entity("TestServer.Models.Private.GroupMember", b =>
                {
                    b.HasOne("TestServer.Models.Private.Group", "groupRecord")
                        .WithMany()
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TestServer.Models.Private.Node", "user")
                        .WithMany()
                        .HasForeignKey("UserId");

                    b.Navigation("groupRecord");

                    b.Navigation("user");
                });

            modelBuilder.Entity("TestServer.Models.Private.Permission", b =>
                {
                    b.HasOne("TestServer.Models.Private.Node", "Node")
                        .WithMany()
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Node");
                });

            modelBuilder.Entity("TestServer.Models.Private.Version", b =>
                {
                    b.HasOne("TestServer.Models.Private.Node", "Node")
                        .WithMany()
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Node");
                });
#pragma warning restore 612, 618
        }
    }
}