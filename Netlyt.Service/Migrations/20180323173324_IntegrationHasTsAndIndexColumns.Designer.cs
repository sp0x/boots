﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Netlyt.Service.Data;
using Netlyt.Service.Models;
using System;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Netlyt.Service.Migrations
{
    [DbContext(typeof(ManagementDbContext))]
    [Migration("20180323173324_IntegrationHasTsAndIndexColumns")]
    partial class IntegrationHasTsAndIndexColumns
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125");

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

            modelBuilder.Entity("Netlyt.Service.ApiAuth", b =>
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

            modelBuilder.Entity("Netlyt.Service.Integration.DataIntegration", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("APIKeyId");

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

            modelBuilder.Entity("Netlyt.Service.Integration.IntegrationExtra", b =>
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

            modelBuilder.Entity("Netlyt.Service.Lex.Data.DonutScriptInfo", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AssemblyPath");

                    b.Property<string>("DonutScriptContent");

                    b.Property<long?>("ModelId");

                    b.HasKey("Id");

                    b.HasIndex("ModelId")
                        .IsUnique();

                    b.ToTable("DonutScripts");
                });

            modelBuilder.Entity("Netlyt.Service.Ml.ApiUser", b =>
                {
                    b.Property<long>("ApiId");

                    b.Property<string>("UserId");

                    b.HasKey("ApiId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("ApiUsers");
                });

            modelBuilder.Entity("Netlyt.Service.Ml.Model", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Callback");

                    b.Property<string>("ClassifierType");

                    b.Property<string>("CurrentModel");

                    b.Property<string>("HyperParams");

                    b.Property<string>("ModelName");

                    b.Property<string>("TargetAttribute");

                    b.Property<string>("TrainingParams");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Models");
                });

            modelBuilder.Entity("Netlyt.Service.Ml.ModelIntegration", b =>
                {
                    b.Property<long>("ModelId");

                    b.Property<long>("IntegrationId");

                    b.HasKey("ModelId", "IntegrationId");

                    b.HasIndex("IntegrationId");

                    b.ToTable("ModelIntegration");
                });

            modelBuilder.Entity("Netlyt.Service.Ml.ModelTrainingPerformance", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<double>("Accuracy");

                    b.Property<string>("FeatureImportance");

                    b.Property<long>("ModelId");

                    b.Property<string>("ReportUrl")
                        .HasColumnType("VARCHAR")
                        .HasMaxLength(255);

                    b.Property<string>("TestResultsUrl")
                        .HasColumnType("VARCHAR")
                        .HasMaxLength(255);

                    b.Property<DateTime>("TrainedTs");

                    b.HasKey("Id");

                    b.HasIndex("ModelId")
                        .IsUnique();

                    b.ToTable("ModelPerformance");
                });

            modelBuilder.Entity("Netlyt.Service.ModelRule", b =>
                {
                    b.Property<long>("ModelId");

                    b.Property<long>("RuleId");

                    b.HasKey("ModelId", "RuleId");

                    b.HasIndex("RuleId");

                    b.ToTable("ModelRule");
                });

            modelBuilder.Entity("Netlyt.Service.Models.FeatureGenerationTask", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ModelId");

                    b.Property<int>("Status");

                    b.HasKey("Id");

                    b.HasIndex("ModelId");

                    b.ToTable("FeatureGenerationTasks");
                });

            modelBuilder.Entity("Netlyt.Service.Organization", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ApiKeyId");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.HasIndex("ApiKeyId");

                    b.ToTable("Organizations");
                });

            modelBuilder.Entity("Netlyt.Service.Rule", b =>
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

            modelBuilder.Entity("Netlyt.Service.Source.FieldDefinition", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ExtrasId");

                    b.Property<long>("IntegrationId");

                    b.Property<string>("Name");

                    b.Property<string>("Type");

                    b.HasKey("Id");

                    b.HasIndex("ExtrasId")
                        .IsUnique();

                    b.HasIndex("IntegrationId");

                    b.ToTable("Fields");
                });

            modelBuilder.Entity("Netlyt.Service.Source.FieldExtra", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("FieldExtrasId");

                    b.Property<long?>("FieldId");

                    b.Property<string>("Key");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("FieldExtrasId");

                    b.HasIndex("FieldId");

                    b.ToTable("FieldExtra");
                });

            modelBuilder.Entity("Netlyt.Service.Source.FieldExtras", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("Nullable");

                    b.Property<bool>("Unique");

                    b.HasKey("Id");

                    b.ToTable("FieldExtras");
                });

            modelBuilder.Entity("Netlyt.Service.User", b =>
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

            modelBuilder.Entity("nvoid.Integration.ApiPermission", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ApiPermissionsSetId");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("ApiPermissionsSetId");

                    b.ToTable("ApiPermission");
                });

            modelBuilder.Entity("nvoid.Integration.ApiPermissionsSet", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ApiAuthId");

                    b.Property<string>("Type");

                    b.HasKey("Id");

                    b.HasIndex("ApiAuthId");

                    b.ToTable("ApiPermissionsSet");
                });

            modelBuilder.Entity("Netlyt.Service.UserRole", b =>
                {
                    b.HasBaseType("Microsoft.AspNetCore.Identity.IdentityRole");


                    b.ToTable("UserRole");

                    b.HasDiscriminator().HasValue("UserRole");
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
                    b.HasOne("Netlyt.Service.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Netlyt.Service.User")
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

                    b.HasOne("Netlyt.Service.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Netlyt.Service.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Netlyt.Service.Integration.DataIntegration", b =>
                {
                    b.HasOne("Netlyt.Service.ApiAuth", "APIKey")
                        .WithMany()
                        .HasForeignKey("APIKeyId");

                    b.HasOne("Netlyt.Service.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId");

                    b.HasOne("Netlyt.Service.ApiAuth", "PublicKey")
                        .WithMany()
                        .HasForeignKey("PublicKeyId");
                });

            modelBuilder.Entity("Netlyt.Service.Integration.IntegrationExtra", b =>
                {
                    b.HasOne("Netlyt.Service.Integration.DataIntegration")
                        .WithMany("Extras")
                        .HasForeignKey("DataIntegrationId");
                });

            modelBuilder.Entity("Netlyt.Service.Lex.Data.DonutScriptInfo", b =>
                {
                    b.HasOne("Netlyt.Service.Ml.Model", "Model")
                        .WithOne("DonutScript")
                        .HasForeignKey("Netlyt.Service.Lex.Data.DonutScriptInfo", "ModelId");
                });

            modelBuilder.Entity("Netlyt.Service.Ml.ApiUser", b =>
                {
                    b.HasOne("Netlyt.Service.ApiAuth", "Api")
                        .WithMany("Users")
                        .HasForeignKey("ApiId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Netlyt.Service.User", "User")
                        .WithMany("ApiKeys")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Netlyt.Service.Ml.Model", b =>
                {
                    b.HasOne("Netlyt.Service.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Netlyt.Service.Ml.ModelIntegration", b =>
                {
                    b.HasOne("Netlyt.Service.Integration.DataIntegration", "Integration")
                        .WithMany("Models")
                        .HasForeignKey("IntegrationId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Netlyt.Service.Ml.Model", "Model")
                        .WithMany("DataIntegrations")
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Netlyt.Service.Ml.ModelTrainingPerformance", b =>
                {
                    b.HasOne("Netlyt.Service.Ml.Model", "Model")
                        .WithOne("Performance")
                        .HasForeignKey("Netlyt.Service.Ml.ModelTrainingPerformance", "ModelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Netlyt.Service.ModelRule", b =>
                {
                    b.HasOne("Netlyt.Service.Ml.Model", "Model")
                        .WithMany("Rules")
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Netlyt.Service.Rule", "Rule")
                        .WithMany("Models")
                        .HasForeignKey("RuleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Netlyt.Service.Models.FeatureGenerationTask", b =>
                {
                    b.HasOne("Netlyt.Service.Ml.Model", "Model")
                        .WithMany("FeatureGenerationTasks")
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Netlyt.Service.Organization", b =>
                {
                    b.HasOne("Netlyt.Service.ApiAuth", "ApiKey")
                        .WithMany()
                        .HasForeignKey("ApiKeyId");
                });

            modelBuilder.Entity("Netlyt.Service.Rule", b =>
                {
                    b.HasOne("Netlyt.Service.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId");
                });

            modelBuilder.Entity("Netlyt.Service.Source.FieldDefinition", b =>
                {
                    b.HasOne("Netlyt.Service.Source.FieldExtras", "Extras")
                        .WithOne("Field")
                        .HasForeignKey("Netlyt.Service.Source.FieldDefinition", "ExtrasId");

                    b.HasOne("Netlyt.Service.Integration.DataIntegration", "Integration")
                        .WithMany("Fields")
                        .HasForeignKey("IntegrationId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Netlyt.Service.Source.FieldExtra", b =>
                {
                    b.HasOne("Netlyt.Service.Source.FieldExtras")
                        .WithMany("Extra")
                        .HasForeignKey("FieldExtrasId");

                    b.HasOne("Netlyt.Service.Source.FieldDefinition", "Field")
                        .WithMany()
                        .HasForeignKey("FieldId");
                });

            modelBuilder.Entity("Netlyt.Service.User", b =>
                {
                    b.HasOne("Netlyt.Service.Organization", "Organization")
                        .WithMany("Members")
                        .HasForeignKey("OrganizationId");

                    b.HasOne("Netlyt.Service.UserRole", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId");
                });

            modelBuilder.Entity("nvoid.Integration.ApiPermission", b =>
                {
                    b.HasOne("nvoid.Integration.ApiPermissionsSet")
                        .WithMany("Required")
                        .HasForeignKey("ApiPermissionsSetId");
                });

            modelBuilder.Entity("nvoid.Integration.ApiPermissionsSet", b =>
                {
                    b.HasOne("Netlyt.Service.ApiAuth")
                        .WithMany("Permissions")
                        .HasForeignKey("ApiAuthId");
                });
#pragma warning restore 612, 618
        }
    }
}
