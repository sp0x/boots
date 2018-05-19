using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Netlyt.Service.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Endpoint = table.Column<string>(nullable: true),
                    AppId = table.Column<string>(nullable: false),
                    AppSecret = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Discriminator = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonutFunction",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(nullable: true),
                    IsAggregate = table.Column<bool>(nullable: false),
                    Parameters = table.Column<string>(nullable: true),
                    Body = table.Column<string>(nullable: true),
                    Projection = table.Column<string>(nullable: true),
                    GroupValue = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonutFunction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldExtras",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Unique = table.Column<bool>(nullable: false),
                    Nullable = table.Column<bool>(nullable: false),
                    FieldId = table.Column<long>(nullable: false),
                    IsFake = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldExtras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiPermissionsSet",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Type = table.Column<string>(nullable: true),
                    ApiAuthId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiPermissionsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiPermissionsSet_ApiKeys_ApiAuthId",
                        column: x => x.ApiAuthId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ApiKeyId = table.Column<long>(nullable: true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    RoleId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiPermission",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Value = table.Column<string>(nullable: true),
                    ApiPermissionsSetId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiPermission_ApiPermissionsSet_ApiPermissionsSetId",
                        column: x => x.ApiPermissionsSetId,
                        principalTable: "ApiPermissionsSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    OrganizationId = table.Column<long>(nullable: true),
                    RoleId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApiUsers",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    ApiId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiUsers", x => new { x.ApiId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ApiUsers_ApiKeys_ApiId",
                        column: x => x.ApiId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApiUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    UserId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    RoleId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    OwnerId = table.Column<string>(nullable: true),
                    FeatureScript = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    DataEncoding = table.Column<int>(nullable: false),
                    APIKeyId = table.Column<long>(nullable: false),
                    PublicKeyId = table.Column<long>(nullable: true),
                    DataFormatType = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true),
                    Collection = table.Column<string>(nullable: true),
                    DataIndexColumn = table.Column<string>(nullable: true),
                    DataTimestampColumn = table.Column<string>(nullable: true),
                    FeaturesCollection = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Integrations_ApiKeys_APIKeyId",
                        column: x => x.APIKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Integrations_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Integrations_ApiKeys_PublicKeyId",
                        column: x => x.PublicKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rules",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    RuleName = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    OwnerId = table.Column<string>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rules_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AggregateKeys",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(nullable: true),
                    OperationId = table.Column<long>(nullable: false),
                    Arguments = table.Column<string>(nullable: true),
                    DataIntegrationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregateKeys_Integrations_DataIntegrationId",
                        column: x => x.DataIntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AggregateKeys_DonutFunction_OperationId",
                        column: x => x.OperationId,
                        principalTable: "DonutFunction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fields",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    ExtrasId = table.Column<long>(nullable: true),
                    DataEncoding = table.Column<int>(nullable: false),
                    IntegrationId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fields_FieldExtras_ExtrasId",
                        column: x => x.ExtrasId,
                        principalTable: "FieldExtras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fields_Integrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationExtra",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    DataIntegrationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationExtra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationExtra_Integrations_DataIntegrationId",
                        column: x => x.DataIntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldExtra",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    FieldId = table.Column<long>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    FieldExtrasId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldExtra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldExtra_FieldExtras_FieldExtrasId",
                        column: x => x.FieldExtrasId,
                        principalTable: "FieldExtras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldExtra_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    UserId = table.Column<string>(nullable: true),
                    DonutScriptId = table.Column<long>(nullable: true),
                    ModelName = table.Column<string>(nullable: true),
                    ClassifierType = table.Column<string>(nullable: true),
                    CurrentModel = table.Column<string>(nullable: true),
                    Callback = table.Column<string>(nullable: true),
                    TrainingParams = table.Column<string>(nullable: true),
                    HyperParams = table.Column<string>(nullable: true),
                    TargetAttribute = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Models_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonutScripts",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    AssemblyPath = table.Column<string>(nullable: true),
                    DonutScriptContent = table.Column<string>(nullable: true),
                    ModelId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonutScripts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonutScripts_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureGenerationTasks",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ModelId = table.Column<long>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureGenerationTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureGenerationTasks_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelIntegration",
                columns: table => new
                {
                    ModelId = table.Column<long>(nullable: false),
                    IntegrationId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelIntegration", x => new { x.ModelId, x.IntegrationId });
                    table.ForeignKey(
                        name: "FK_ModelIntegration_Integrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModelIntegration_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelRule",
                columns: table => new
                {
                    ModelId = table.Column<long>(nullable: false),
                    RuleId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelRule", x => new { x.ModelId, x.RuleId });
                    table.ForeignKey(
                        name: "FK_ModelRule_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModelRule_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelTrainingPerformance",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ModelId = table.Column<long>(nullable: false),
                    TrainedTs = table.Column<DateTime>(nullable: false),
                    Accuracy = table.Column<double>(nullable: false),
                    FeatureImportance = table.Column<string>(nullable: true),
                    ReportUrl = table.Column<string>(type: "varchar", maxLength: 255, nullable: true),
                    TestResultsUrl = table.Column<string>(type: "varchar", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelTrainingPerformance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelTrainingPerformance_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingTasks",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ModelId = table.Column<long>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingTasks_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateKeys_DataIntegrationId",
                table: "AggregateKeys",
                column: "DataIntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateKeys_OperationId",
                table: "AggregateKeys",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiPermission_ApiPermissionsSetId",
                table: "ApiPermission",
                column: "ApiPermissionsSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiPermissionsSet_ApiAuthId",
                table: "ApiPermissionsSet",
                column: "ApiAuthId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsers_UserId",
                table: "ApiUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RoleId",
                table: "AspNetUsers",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DonutScripts_ModelId",
                table: "DonutScripts",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureGenerationTasks_ModelId",
                table: "FeatureGenerationTasks",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldExtra_FieldExtrasId",
                table: "FieldExtra",
                column: "FieldExtrasId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldExtra_FieldId",
                table: "FieldExtra",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_ExtrasId",
                table: "Fields",
                column: "ExtrasId");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_IntegrationId",
                table: "Fields",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExtra_DataIntegrationId",
                table: "IntegrationExtra",
                column: "DataIntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_APIKeyId",
                table: "Integrations",
                column: "APIKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OwnerId",
                table: "Integrations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_PublicKeyId",
                table: "Integrations",
                column: "PublicKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelIntegration_IntegrationId",
                table: "ModelIntegration",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelRule_RuleId",
                table: "ModelRule",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_DonutScriptId",
                table: "Models",
                column: "DonutScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_UserId",
                table: "Models",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelTrainingPerformance_ModelId",
                table: "ModelTrainingPerformance",
                column: "ModelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_ApiKeyId",
                table: "Organizations",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_OwnerId",
                table: "Rules",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTasks_ModelId",
                table: "TrainingTasks",
                column: "ModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Models_DonutScripts_DonutScriptId",
                table: "Models",
                column: "DonutScriptId",
                principalTable: "DonutScripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_ApiKeys_ApiKeyId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Models_AspNetUsers_UserId",
                table: "Models");

            migrationBuilder.DropForeignKey(
                name: "FK_DonutScripts_Models_ModelId",
                table: "DonutScripts");

            migrationBuilder.DropTable(
                name: "AggregateKeys");

            migrationBuilder.DropTable(
                name: "ApiPermission");

            migrationBuilder.DropTable(
                name: "ApiUsers");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "FeatureGenerationTasks");

            migrationBuilder.DropTable(
                name: "FieldExtra");

            migrationBuilder.DropTable(
                name: "IntegrationExtra");

            migrationBuilder.DropTable(
                name: "ModelIntegration");

            migrationBuilder.DropTable(
                name: "ModelRule");

            migrationBuilder.DropTable(
                name: "ModelTrainingPerformance");

            migrationBuilder.DropTable(
                name: "TrainingTasks");

            migrationBuilder.DropTable(
                name: "DonutFunction");

            migrationBuilder.DropTable(
                name: "ApiPermissionsSet");

            migrationBuilder.DropTable(
                name: "Fields");

            migrationBuilder.DropTable(
                name: "Rules");

            migrationBuilder.DropTable(
                name: "FieldExtras");

            migrationBuilder.DropTable(
                name: "Integrations");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Models");

            migrationBuilder.DropTable(
                name: "DonutScripts");
        }
    }
}
