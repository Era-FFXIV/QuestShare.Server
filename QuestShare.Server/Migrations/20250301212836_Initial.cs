using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestShare.Server.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bans",
                columns: table => new
                {
                    BanId = table.Column<string>(type: "text", nullable: false),
                    BanIp = table.Column<IPAddress>(type: "inet", nullable: false),
                    BanCharacterId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    BanReason = table.Column<string>(type: "text", nullable: false),
                    BanIssuer = table.Column<string>(type: "text", nullable: false),
                    BanDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BanExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bans", x => x.BanId);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    KnownShareCodes = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp"),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp"),
                    SessionMemberClientSessionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "SessionMembers",
                columns: table => new
                {
                    ClientSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp"),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionMembers", x => x.ClientSessionId);
                    table.ForeignKey(
                        name: "FK_SessionMembers_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerCharacterId = table.Column<string>(type: "text", nullable: false),
                    OwnerClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShareCode = table.Column<string>(type: "text", nullable: false),
                    ReservedConnectionId = table.Column<string>(type: "text", nullable: false),
                    SharedQuestId = table.Column<int>(type: "integer", nullable: false),
                    SharedQuestStep = table.Column<byte>(type: "smallint", nullable: false),
                    PartyMembers = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SkipPartyCheck = table.Column<bool>(type: "boolean", nullable: false),
                    AllowJoins = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp"),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp"),
                    SessionMemberClientSessionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Sessions_Clients_OwnerClientId",
                        column: x => x.OwnerClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sessions_SessionMembers_SessionMemberClientSessionId",
                        column: x => x.SessionMemberClientSessionId,
                        principalTable: "SessionMembers",
                        principalColumn: "ClientSessionId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_SessionMemberClientSessionId",
                table: "Clients",
                column: "SessionMemberClientSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionMembers_ClientId",
                table: "SessionMembers",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionMembers_SessionId",
                table: "SessionMembers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_OwnerClientId",
                table: "Sessions",
                column: "OwnerClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SessionMemberClientSessionId",
                table: "Sessions",
                column: "SessionMemberClientSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_SessionMembers_SessionMemberClientSessionId",
                table: "Clients",
                column: "SessionMemberClientSessionId",
                principalTable: "SessionMembers",
                principalColumn: "ClientSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionMembers_Sessions_SessionId",
                table: "SessionMembers",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_SessionMembers_SessionMemberClientSessionId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_SessionMembers_SessionMemberClientSessionId",
                table: "Sessions");

            migrationBuilder.DropTable(
                name: "Bans");

            migrationBuilder.DropTable(
                name: "SessionMembers");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
