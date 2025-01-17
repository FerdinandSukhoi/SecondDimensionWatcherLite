﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher.Migrations
{
    [DbContext(typeof(AppDataContext))]
    [Migration("20210424043029_StoreInfo")]
    partial class StoreInfo
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.5")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("SecondDimensionWatcher.Data.AnimationInfo", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Hash")
                        .HasColumnType("text");

                    b.Property<bool>("IsFinished")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsTracked")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset>("PublishTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("StorePath")
                        .HasColumnType("text");

                    b.Property<byte[]>("TorrentData")
                        .HasColumnType("bytea");

                    b.Property<string>("TorrentUrl")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("TrackTime")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("Hash")
                        .IsUnique();

                    b.ToTable("AnimationInfo");
                });
#pragma warning restore 612, 618
        }
    }
}
