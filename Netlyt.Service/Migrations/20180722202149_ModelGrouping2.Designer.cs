﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Netlyt.Service.Data;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Netlyt.Service.Migrations
{
    [DbContext(typeof(ManagementDbContext))]
    [Migration("20180722202149_ModelGrouping2")]
    partial class ModelGrouping2
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.1.1-rtm-30846")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Donut.AggregateKey", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Arguments");

                    b.Property<long?>("DataIntegrationId");

                    b.Property<string>("Name");

                    b.Property<long?>("OperationId");

                    b.HasKey("Id");

                    b.HasIndex("DataIntegrationId");

                    b.HasIndex("OperationId")
                        .IsUnique();

                    b.ToTable("AggregateKeys");
                });

            modelBuilder.Entity("Donut.Data.DataIntegration", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("APIKeyId");

                    b.Property<string>("Collection");

                    b.Property<int>("DataEncoding");

                    b.Property<string>("DataFormatType");

                    b.Property<string>("DataIndexColumn");

                    b.Property<string>("DataTimestampColumn");

                    b.Property<string>("FeatureScript");

                    b.Property<string>("FeaturesCollection");

                    b.Property<string>("Name");

                    b.Property<string>("OwnerId");

                    b.Property<long?>("PublicKeyId");

                    b.Property<string>("Source");

                    b.HasKey("Id");

                    b.HasIndex("APIKeyId");

                    b.HasIndex("OwnerId");

                    b.HasIndex("PublicKeyId");

                    b.ToTable("Integrations");
                });

            modelBuilder.Entity("Donut.Data.ModelTarget", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ColumnId");

                    b.Property<bool>("IsRegression");

                    b.Property<long>("ModelId");

                    b.Property<string>("Scoring");

                    b.Property<string>("Type");

                    b.HasKey("Id");

                    b.HasIndex("ColumnId");

                    b.HasIndex("ModelId");

                    b.ToTable("ModelTargets");
                });

            modelBuilder.Entity("Donut.Data.TargetConstraint", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("AfterId");

                    b.Property<long?>("BeforeId");

                    b.Property<string>("Key");

                    b.Property<long?>("ModelTargetId");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.HasIndex("AfterId");

                    b.HasIndex("BeforeId");

                    b.HasIndex("ModelTargetId");

                    b.ToTable("TargetConstraint");
                });

            modelBuilder.Entity("Donut.Data.TimeConstraint", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Days");

                    b.Property<int>("Hours");

                    b.Property<long>("Months");

                    b.Property<int>("Seconds");

                    b.Property<long>("Years");

                    b.HasKey("Id");

                    b.ToTable("TimeConstraint");
                });

            modelBuilder.Entity("Donut.DonutFunction", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Body");

                    b.Property<string>("GroupValue");

                    b.Property<bool>("IsAggregate");

                    b.Property<string>("Name");

                    b.Property<string>("Projection");

                    b.Property<int>("Type");

                    b.Property<string>("_Parameters")
                        .HasColumnName("Parameters");

                    b.HasKey("Id");

                    b.ToTable("DonutFunction");
                });

            modelBuilder.Entity("Donut.DonutScriptInfo", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AssemblyPath");

                    b.Property<string>("DonutScriptContent");

                    b.Property<long>("ModelId");

                    b.HasKey("Id");

                    b.HasIndex("ModelId");

                    b.ToTable("DonutScripts");
                });

            modelBuilder.Entity("Donut.Models.FeatureGenerationTask", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ModelId");

                    b.Property<int>("Status");

                    b.HasKey("Id");

                    b.HasIndex("ModelId");

                    b.ToTable("FeatureGenerationTasks");
                });

            modelBuilder.Entity("Donut.Models.Model", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Callback");

                    b.Property<DateTime>("CreatedOn");

                    b.Property<string>("CurrentModel");

                    b.Property<long?>("DonutScriptId");

                    b.Property<string>("Grouping");

                    b.Property<string>("HyperParams");

                    b.Property<string>("ModelName");

                    b.Property<string>("TrainingParams");

                    b.Property<bool>("UseFeatures");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("DonutScriptId");

                    b.HasIndex("UserId");

                    b.ToTable("Models");
                });

            modelBuilder.Entity("Donut.Models.ModelIntegration", b =>
                {
                    b.Property<long>("ModelId");

                    b.Property<long>("IntegrationId");

                    b.HasKey("ModelId", "IntegrationId");

                    b.HasIndex("IntegrationId");

                    b.ToTable("ModelIntegration");
                });

            modelBuilder.Entity("Donut.Models.ModelRule", b =>
                {
                    b.Property<long>("ModelId");

                    b.Property<long>("RuleId");

                    b.HasKey("ModelId", "RuleId");

                    b.HasIndex("RuleId");

                    b.ToTable("ModelRule");
                });

            modelBuilder.Entity("Donut.Models.ModelTrainingPerformance", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<double>("Accuracy");

                    b.Property<string>("AdvancedReport")
                        .HasColumnType("text");

                    b.Property<string>("FeatureImportance")
                        .HasColumnType("text");

                    b.Property<string>("LastRequestIP")
                        .HasColumnType("VARCHAR")
                        .HasMaxLength(255);

                    b.Property<DateTime>("LastRequestTs");

                    b.Property<long>("ModelId");

                    b.Property<string>("MonthlyUsage")
                        .HasColumnType("text");

                    b.Property<string>("ReportUrl")
                        .HasColumnType("VARCHAR")
                        .HasMaxLength(255);

                    b.Property<string>("TargetName");

                    b.Property<long?>("TaskId");

                    b.Property<string>("TestResultsUrl")
                        .HasColumnType("VARCHAR")
                        .HasMaxLength(255);

                    b.Property<DateTime>("TrainedTs");

                    b.Property<string>("WeeklyUsage")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ModelId")
                        .IsUnique();

                    b.HasIndex("TaskId")
                        .IsUnique();

                    b.ToTable("ModelTrainingPerformance");
                });

            modelBuilder.Entity("Donut.Models.Rule", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("IsActive");

                    b.Property<string>("OwnerId");

                    b.Property<string>("RuleName");

                    b.Property<string>("Type");

                    b.HasKey("Id");

                    b.HasIndex("OwnerId");

                    b.ToTable("Rules");
                });

            modelBuilder.Entity("Donut.Models.TrainingScript", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DonutScript");

                    b.Property<string>("PythonScript");

                    b.Property<long>("TrainingTaskId");

                    b.HasKey("Id");

                    b.HasIndex("TrainingTaskId")
                        .IsUnique();

                    b.ToTable("TrainingScript");
                });

            modelBuilder.Entity("Donut.Models.TrainingTask", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedOn");

                    b.Property<long>("ModelId");

                    b.Property<string>("Scoring");

                    b.Property<int>("Status");

                    b.Property<long?>("TargetId");

                    b.Property<int>("TrainingTargetId");

                    b.Property<string>("TypeInfo");

                    b.Property<DateTime>("UpdatedOn");

                    b.HasKey("Id");

                    b.HasIndex("ModelId");

                    b.HasIndex("TargetId");

                    b.ToTable("TrainingTasks");
                });

            modelBuilder.Entity("Donut.Source.FieldDefinition", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("DataEncoding");

                    b.Property<long?>("ExtrasId");

                    b.Property<long>("IntegrationId");

                    b.Property<string>("Name");

                    b.Property<string>("TargetType");

                    b.Property<string>("Type");

                    b.HasKey("Id");

                    b.HasIndex("ExtrasId");

                    b.HasIndex("IntegrationId");

                    b.ToTable("Fields");
                });

            modelBuilder.Entity("Donut.Source.FieldExtra", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("FieldExtrasId");

                    b.Property<long?>("FieldId");

                    b.Property<string>("Key");

                    b.Property<int>("Type");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("FieldExtrasId");

                    b.HasIndex("FieldId");

                    b.ToTable("FieldExtra");
                });

            modelBuilder.Entity("Donut.Source.FieldExtras", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("FieldId");

                    b.Property<bool>("IsFake");

                    b.Property<bool>("Nullable");

                    b.Property<bool>("Unique");

                    b.HasKey("Id");

                    b.ToTable("FieldExtras");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Discriminator")
                        .IsRequired();

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex");

                    b.ToTable("AspNetRoles");

                    b.HasDiscriminator<string>("Discriminator").HasValue("IdentityRole");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("RoleId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("Netlyt.Interfaces.ApiUser", b =>
                {
                    b.Property<long>("ApiId");

                    b.Property<string>("UserId");

                    b.HasKey("ApiId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("ApiUsers");
                });

            modelBuilder.Entity("Netlyt.Interfaces.IntegrationExtra", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("DataIntegrationId");

                    b.Property<string>("Key");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("DataIntegrationId");

                    b.ToTable("IntegrationExtra");
                });

            modelBuilder.Entity("Netlyt.Interfaces.Models.ApiAuth", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AppId")
                        .IsRequired();

                    b.Property<string>("AppSecret");

                    b.Property<string>("Endpoint");

                    b.Property<string>("Type");

                    b.HasKey("Id");

                    b.ToTable("ApiKeys");
                });

            modelBuilder.Entity("Netlyt.Interfaces.Models.ApiPermission", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ApiPermissionsSetId");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("ApiPermissionsSetId");

                    b.ToTable("ApiPermission");
                });

            modelBuilder.Entity("Netlyt.Interfaces.Models.ApiPermissionsSet", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ApiAuthId");

                    b.Property<string>("Type");

                    b.HasKey("Id");

                    b.HasIndex("ApiAuthId");

                    b.ToTable("ApiPermissionsSet");
                });

            modelBuilder.Entity("Netlyt.Interfaces.Models.Organization", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ApiKeyId");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.HasIndex("ApiKeyId");

                    b.ToTable("Organizations");
                });

            modelBuilder.Entity("Netlyt.Interfaces.Models.User", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<string>("FirstName");

                    b.Property<string>("LastName");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<long?>("OrganizationId");

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<string>("RoleId");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex");

                    b.HasIndex("OrganizationId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("Netlyt.Interfaces.Models.UserRole", b =>
                {
                    b.HasBaseType("Microsoft.AspNetCore.Identity.IdentityRole");


                    b.ToTable("UserRole");

                    b.HasDiscriminator().HasValue("UserRole");
                });

            modelBuilder.Entity("Donut.AggregateKey", b =>
                {
                    b.HasOne("Donut.Data.DataIntegration")
                        .WithMany("AggregateKeys")
                        .HasForeignKey("DataIntegrationId");

                    b.HasOne("Donut.DonutFunction", "Operation")
                        .WithOne("AggregateKey")
                        .HasForeignKey("Donut.AggregateKey", "OperationId");
                });

            modelBuilder.Entity("Donut.Data.DataIntegration", b =>
                {
                    b.HasOne("Netlyt.Interfaces.Models.ApiAuth", "APIKey")
                        .WithMany()
                        .HasForeignKey("APIKeyId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Netlyt.Interfaces.Models.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId");

                    b.HasOne("Netlyt.Interfaces.Models.ApiAuth", "PublicKey")
                        .WithMany()
                        .HasForeignKey("PublicKeyId");
                });

            modelBuilder.Entity("Donut.Data.ModelTarget", b =>
                {
                    b.HasOne("Donut.Source.FieldDefinition", "Column")
                        .WithMany()
                        .HasForeignKey("ColumnId");

                    b.HasOne("Donut.Models.Model", "Model")
                        .WithMany("Targets")
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Donut.Data.TargetConstraint", b =>
                {
                    b.HasOne("Donut.Data.TimeConstraint", "After")
                        .WithMany()
                        .HasForeignKey("AfterId");

                    b.HasOne("Donut.Data.TimeConstraint", "Before")
                        .WithMany()
                        .HasForeignKey("BeforeId");

                    b.HasOne("Donut.Data.ModelTarget")
                        .WithMany("Constraints")
                        .HasForeignKey("ModelTargetId");
                });

            modelBuilder.Entity("Donut.DonutScriptInfo", b =>
                {
                    b.HasOne("Donut.Models.Model", "Model")
                        .WithMany()
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Donut.Models.FeatureGenerationTask", b =>
                {
                    b.HasOne("Donut.Models.Model", "Model")
                        .WithMany("FeatureGenerationTasks")
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Donut.Models.Model", b =>
                {
                    b.HasOne("Donut.DonutScriptInfo", "DonutScript")
                        .WithMany()
                        .HasForeignKey("DonutScriptId");

                    b.HasOne("Netlyt.Interfaces.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Donut.Models.ModelIntegration", b =>
                {
                    b.HasOne("Donut.Data.DataIntegration", "Integration")
                        .WithMany("Models")
                        .HasForeignKey("IntegrationId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Donut.Models.Model", "Model")
                        .WithMany("DataIntegrations")
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Donut.Models.ModelRule", b =>
                {
                    b.HasOne("Donut.Models.Model", "Model")
                        .WithMany("Rules")
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Donut.Models.Rule", "Rule")
                        .WithMany("Models")
                        .HasForeignKey("RuleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Donut.Models.ModelTrainingPerformance", b =>
                {
                    b.HasOne("Donut.Models.Model", "Model")
                        .WithOne("Performance")
                        .HasForeignKey("Donut.Models.ModelTrainingPerformance", "ModelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Donut.Models.TrainingTask", "Task")
                        .WithOne("Performance")
                        .HasForeignKey("Donut.Models.ModelTrainingPerformance", "TaskId");
                });

            modelBuilder.Entity("Donut.Models.Rule", b =>
                {
                    b.HasOne("Netlyt.Interfaces.Models.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId");
                });

            modelBuilder.Entity("Donut.Models.TrainingScript", b =>
                {
                    b.HasOne("Donut.Models.TrainingTask", "TrainingTask")
                        .WithOne("Script")
                        .HasForeignKey("Donut.Models.TrainingScript", "TrainingTaskId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Donut.Models.TrainingTask", b =>
                {
                    b.HasOne("Donut.Models.Model", "Model")
                        .WithMany("TrainingTasks")
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Donut.Data.ModelTarget", "Target")
                        .WithMany()
                        .HasForeignKey("TargetId");
                });

            modelBuilder.Entity("Donut.Source.FieldDefinition", b =>
                {
                    b.HasOne("Donut.Source.FieldExtras", "Extras")
                        .WithMany()
                        .HasForeignKey("ExtrasId");

                    b.HasOne("Donut.Data.DataIntegration", "Integration")
                        .WithMany("Fields")
                        .HasForeignKey("IntegrationId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Donut.Source.FieldExtra", b =>
                {
                    b.HasOne("Donut.Source.FieldExtras")
                        .WithMany("Extra")
                        .HasForeignKey("FieldExtrasId");

                    b.HasOne("Donut.Source.FieldDefinition", "Field")
                        .WithMany()
                        .HasForeignKey("FieldId");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Netlyt.Interfaces.Models.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Netlyt.Interfaces.Models.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Netlyt.Interfaces.Models.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Netlyt.Interfaces.Models.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Netlyt.Interfaces.ApiUser", b =>
                {
                    b.HasOne("Netlyt.Interfaces.Models.ApiAuth", "Api")
                        .WithMany("Users")
                        .HasForeignKey("ApiId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Netlyt.Interfaces.Models.User", "User")
                        .WithMany("ApiKeys")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Netlyt.Interfaces.IntegrationExtra", b =>
                {
                    b.HasOne("Donut.Data.DataIntegration")
                        .WithMany("Extras")
                        .HasForeignKey("DataIntegrationId");
                });

            modelBuilder.Entity("Netlyt.Interfaces.Models.ApiPermission", b =>
                {
                    b.HasOne("Netlyt.Interfaces.Models.ApiPermissionsSet")
                        .WithMany("Required")
                        .HasForeignKey("ApiPermissionsSetId");
                });

            modelBuilder.Entity("Netlyt.Interfaces.Models.ApiPermissionsSet", b =>
                {
                    b.HasOne("Netlyt.Interfaces.Models.ApiAuth")
                        .WithMany("Permissions")
                        .HasForeignKey("ApiAuthId");
                });

            modelBuilder.Entity("Netlyt.Interfaces.Models.Organization", b =>
                {
                    b.HasOne("Netlyt.Interfaces.Models.ApiAuth", "ApiKey")
                        .WithMany()
                        .HasForeignKey("ApiKeyId");
                });

            modelBuilder.Entity("Netlyt.Interfaces.Models.User", b =>
                {
                    b.HasOne("Netlyt.Interfaces.Models.Organization", "Organization")
                        .WithMany("Members")
                        .HasForeignKey("OrganizationId");

                    b.HasOne("Netlyt.Interfaces.Models.UserRole", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId");
                });
#pragma warning restore 612, 618
        }
    }
}