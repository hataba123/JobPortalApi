using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortalApi.Migrations
{
    /// <summary>
    /// Bổ sung các bảng blog và cột công ty bị thiếu trong các migration cũ.
    /// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260910010000_RepairCompanyAndBlogSchema")]
public partial class RepairCompanyAndBlogSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Companies', N'UserId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [UserId] uniqueidentifier NULL;
END;
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Companies_UserId'
      AND object_id = OBJECT_ID(N'[dbo].[Companies]'))
BEGIN
    CREATE INDEX [IX_Companies_UserId] ON [dbo].[Companies] ([UserId]);
END;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[BlogAuthors]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BlogAuthors]
    (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Avatar] nvarchar(500) NOT NULL,
        [Role] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BlogAuthors] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[BlogCategories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BlogCategories]
    (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BlogCategories] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Blogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Blogs]
    (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Excerpt] nvarchar(500) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Slug] nvarchar(100) NULL,
        [Category] nvarchar(50) NOT NULL,
        [Tags] nvarchar(max) NOT NULL,
        [PublishedAt] datetime2 NOT NULL,
        [ReadTime] nvarchar(20) NOT NULL,
        [Views] int NOT NULL,
        [Likes] int NOT NULL,
        [Featured] bit NOT NULL,
        [Image] nvarchar(500) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [AuthorId] int NOT NULL,
        CONSTRAINT [PK_Blogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Blogs_BlogAuthors_AuthorId]
            FOREIGN KEY ([AuthorId]) REFERENCES [dbo].[BlogAuthors] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_Blogs_AuthorId] ON [dbo].[Blogs] ([AuthorId]);
END;

IF OBJECT_ID(N'[dbo].[BlogViews]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BlogViews]
    (
        [Id] int IDENTITY(1,1) NOT NULL,
        [BlogId] int NOT NULL,
        [UserId] nvarchar(max) NULL,
        [IpAddress] nvarchar(max) NULL,
        [ViewedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BlogViews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BlogViews_Blogs_BlogId]
            FOREIGN KEY ([BlogId]) REFERENCES [dbo].[Blogs] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_BlogViews_BlogId] ON [dbo].[BlogViews] ([BlogId]);
END;

IF OBJECT_ID(N'[dbo].[BlogLikes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BlogLikes]
    (
        [Id] int IDENTITY(1,1) NOT NULL,
        [BlogId] int NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [LikedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BlogLikes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BlogLikes_Blogs_BlogId]
            FOREIGN KEY ([BlogId]) REFERENCES [dbo].[Blogs] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_BlogLikes_BlogId] ON [dbo].[BlogLikes] ([BlogId]);
END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[BlogLikes]', N'U') IS NOT NULL DROP TABLE [dbo].[BlogLikes];
IF OBJECT_ID(N'[dbo].[BlogViews]', N'U') IS NOT NULL DROP TABLE [dbo].[BlogViews];
IF OBJECT_ID(N'[dbo].[Blogs]', N'U') IS NOT NULL DROP TABLE [dbo].[Blogs];
IF OBJECT_ID(N'[dbo].[BlogCategories]', N'U') IS NOT NULL DROP TABLE [dbo].[BlogCategories];
IF OBJECT_ID(N'[dbo].[BlogAuthors]', N'U') IS NOT NULL DROP TABLE [dbo].[BlogAuthors];
IF COL_LENGTH(N'dbo.Companies', N'UserId') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_Companies_UserId'
          AND object_id = OBJECT_ID(N'[dbo].[Companies]'))
        DROP INDEX [IX_Companies_UserId] ON [dbo].[Companies];
    ALTER TABLE [dbo].[Companies] DROP COLUMN [UserId];
END;
");
        }
    }
}
