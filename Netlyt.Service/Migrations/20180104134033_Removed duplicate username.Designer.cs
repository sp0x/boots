﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Netlyt.Service.Data;
using System;


namespace Netlyt.Service.Migrations
{
    [DbContext(typeof(ManagementDbContext))]
    [Migration("20180104134033_Removed duplicate username")]
    partial class Removedduplicateusername
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125");

            modelBuilder.Entity("Netlyt.Service.Integration.DataIntegration", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("APIKeyId");

                    b.Property<string>("Collection");

                    b.Property<int>("DataEncoding");

                    b.Property<string>("DataFormatType");

                    b.Property<string>("FeatureScript");

                    b.Property<string>("Name");

                    b.Property<string>("OwnerId");

                    b.Property<string>("Source");

                    b.HasKey("Id");

                    b.HasIndex("APIKeyId");

                    b.HasIndex("OwnerId");

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

            modelBuilder.Entity("Netlyt.Service.Ml.Model", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Callback");

                    b.Property<string>("ClassifierType");

                    b.Property<string>("CurrentModel");

                    b.Property<string>("HyperParams");

                    b.Property<string>("ModelName");

                    b.Property<string>("TrainingParams");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Models");
                });

            modelBuilder.Entity("Netlyt.Service.Ml.ModelIntegration", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("IntegrationId");

                    b.Property<long?>("IntegrationId1");

                    b.Property<long>("ModelId");

                    b.HasKey("Id");

                    b.HasIndex("IntegrationId1");

                    b.HasIndex("ModelId");

                    b.ToTable("ModelIntegration");
                });

            modelBuilder.Entity("Netlyt.Service.ModelRule", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ModelId");

                    b.Property<long>("RuleId");

                    b.HasKey("Id");

                    b.HasIndex("ModelId");

                    b.HasIndex("RuleId");

                    b.ToTable("ModelRule");
                });

            modelBuilder.Entity("Netlyt.Service.Organization", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("Id");

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

                    b.Property<long?>("DataIntegrationId");

                    b.Property<long?>("ExtrasId");

                    b.Property<string>("Name");

                    b.Property<string>("Type");

                    b.HasKey("Id");

                    b.HasIndex("DataIntegrationId");

                    b.HasIndex("ExtrasId");

                    b.ToTable("Fields");
                });

            modelBuilder.Entity("Netlyt.Service.Source.FieldExtra", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("FieldExtrasId");

                    b.Property<string>("Key");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("FieldExtrasId");

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

                    b.Property<string>("ConcurrencyStamp");

                    b.Property<string>("Email");

                    b.Property<bool>("EmailConfirmed");

                    b.Property<string>("FirstName");

                    b.Property<string>("LastName");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NormalizedEmail");

                    b.Property<string>("NormalizedUserName");

                    b.Property<long?>("OrganizationId");

                    b.Property<string>("Password");

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName");

                    b.HasKey("Id");

                    b.HasIndex("OrganizationId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Netlyt.Service.UserRole", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConcurrencyStamp");

                    b.Property<string>("Name");

                    b.Property<string>("NormalizedName");

                    b.HasKey("Id");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("nvoid.Integration.ApiAuth", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AppId");

                    b.Property<string>("AppSecret");

                    b.Property<string>("Endpoint");

                    b.Property<string>("Type");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("ApiKeys");
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

            modelBuilder.Entity("Netlyt.Service.Integration.DataIntegration", b =>
                {
                    b.HasOne("nvoid.Integration.ApiAuth", "APIKey")
                        .WithMany()
                        .HasForeignKey("APIKeyId");

                    b.HasOne("Netlyt.Service.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId");
                });

            modelBuilder.Entity("Netlyt.Service.Integration.IntegrationExtra", b =>
                {
                    b.HasOne("Netlyt.Service.Integration.DataIntegration")
                        .WithMany("Extras")
                        .HasForeignKey("DataIntegrationId");
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
                        .HasForeignKey("IntegrationId1");

                    b.HasOne("Netlyt.Service.Ml.Model", "Model")
                        .WithMany("DataIntegrations")
                        .HasForeignKey("ModelId")
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

            modelBuilder.Entity("Netlyt.Service.Rule", b =>
                {
                    b.HasOne("Netlyt.Service.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId");
                });

            modelBuilder.Entity("Netlyt.Service.Source.FieldDefinition", b =>
                {
                    b.HasOne("Netlyt.Service.Integration.DataIntegration")
                        .WithMany("Fields")
                        .HasForeignKey("DataIntegrationId");

                    b.HasOne("Netlyt.Service.Source.FieldExtras", "Extras")
                        .WithMany()
                        .HasForeignKey("ExtrasId");
                });

            modelBuilder.Entity("Netlyt.Service.Source.FieldExtra", b =>
                {
                    b.HasOne("Netlyt.Service.Source.FieldExtras")
                        .WithMany("Extra")
                        .HasForeignKey("FieldExtrasId");
                });

            modelBuilder.Entity("Netlyt.Service.User", b =>
                {
                    b.HasOne("Netlyt.Service.Organization", "Organization")
                        .WithMany("Members")
                        .HasForeignKey("OrganizationId");
                });

            modelBuilder.Entity("nvoid.Integration.ApiAuth", b =>
                {
                    b.HasOne("Netlyt.Service.User")
                        .WithMany("ApiKeys")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("nvoid.Integration.ApiPermission", b =>
                {
                    b.HasOne("nvoid.Integration.ApiPermissionsSet")
                        .WithMany("Required")
                        .HasForeignKey("ApiPermissionsSetId");
                });

            modelBuilder.Entity("nvoid.Integration.ApiPermissionsSet", b =>
                {
                    b.HasOne("nvoid.Integration.ApiAuth")
                        .WithMany("Permissions")
                        .HasForeignKey("ApiAuthId");
                });
#pragma warning restore 612, 618
        }
    }
}
