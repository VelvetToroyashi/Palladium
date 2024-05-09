﻿// <auto-generated />
using System;
using Bookmarker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Bookmarker.Migrations
{
    [DbContext(typeof(BookmarkContext))]
    partial class BookmarkContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("Bookmarker.Data.BookmarkEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Attachments")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("AuthorID")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ChannelID")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<ulong?>("GuildID")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MessageID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PartialContent")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("Tags")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UserID")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Bookmarks");
                });
#pragma warning restore 612, 618
        }
    }
}