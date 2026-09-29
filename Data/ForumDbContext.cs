using ForumWebAPI.Model;
using System.Collections.Generic;
using System.Reflection.Emit;

using Microsoft.EntityFrameworkCore;

namespace Forum.Data;

public class ForumDbContext : DbContext
{
    public ForumDbContext(DbContextOptions<ForumDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Subforum> Subforums => Set<Subforum>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<Ban> Bans => Set<Ban>();
    public DbSet<SubforumRole> SubforumRoles => Set<SubforumRole>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            e.Property(x => x.RegistrationDate).HasColumnName("registration_date").HasDefaultValueSql("NOW()");
            e.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        });

        mb.Entity<Subforum>(e =>
        {
            e.ToTable("subforums");
            e.HasKey(x => x.SubforumId);
            e.Property(x => x.SubforumId).HasColumnName("subforum_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        });

        mb.Entity<Tag>(e =>
        {
            e.ToTable("tags");
            e.HasKey(x => x.TagId);
            e.Property(x => x.TagId).HasColumnName("tag_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        mb.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.Property(x => x.RoleName).HasColumnName("role_name").HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.RoleName).IsUnique();
        });

        mb.Entity<Post>(e =>
        {
            e.ToTable("posts");
            e.HasKey(x => x.PostId);
            e.Property(x => x.PostId).HasColumnName("post_id");
            e.Property(x => x.SubforumId).HasColumnName("subforum_id").IsRequired();
            e.Property(x => x.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            e.Property(x => x.Content).HasColumnName("content").IsRequired();
            e.Property(x => x.CreationDate).HasColumnName("creation_date").HasDefaultValueSql("NOW()");
            e.Property(x => x.AuthorId).HasColumnName("author_id").IsRequired();
            e.Property(x => x.Rating).HasColumnName("rating").HasDefaultValue(0);
            e.Property(x => x.TagId).HasColumnName("tag_id");
            e.Property(x => x.IsPinned).HasColumnName("is_pinned").HasDefaultValue(false);
            e.Property(x => x.IsClosed).HasColumnName("is_closed").HasDefaultValue(false);
            e.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

            e.HasOne(x => x.Subforum).WithMany(s => s.Posts)
                .HasForeignKey(x => x.SubforumId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Author).WithMany(u => u.Posts)
                .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tag).WithMany(t => t.Posts)
                .HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.SubforumId);
            e.HasIndex(x => x.AuthorId);
            e.HasIndex(x => x.TagId);
        });

        mb.Entity<Comment>(e =>
        {
            e.ToTable("comments");
            e.HasKey(x => x.CommentId);
            e.Property(x => x.CommentId).HasColumnName("comment_id");
            e.Property(x => x.Content).HasColumnName("content").IsRequired();
            e.Property(x => x.CreationDate).HasColumnName("creation_date").HasDefaultValueSql("NOW()");
            e.Property(x => x.AuthorId).HasColumnName("author_id").IsRequired();
            e.Property(x => x.PostId).HasColumnName("post_id").IsRequired();
            e.Property(x => x.ParentCommentId).HasColumnName("parent_comment_id");
            e.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            e.Property(x => x.Rating).HasColumnName("rating").HasDefaultValue(0);

            e.HasOne(x => x.Author).WithMany(u => u.Comments)
                .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Post).WithMany(p => p.Comments)
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ParentComment).WithMany(c => c.Replies)
                .HasForeignKey(x => x.ParentCommentId).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.AuthorId);
            e.HasIndex(x => x.ParentCommentId);
        });

        mb.Entity<Vote>(e =>
        {
            e.ToTable("votes");
            e.HasKey(x => x.VoteId);
            e.Property(x => x.VoteId).HasColumnName("vote_id");
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.PostId).HasColumnName("post_id");
            e.Property(x => x.CommentId).HasColumnName("comment_id");
            e.Property(x => x.VoteType).HasColumnName("vote_type").IsRequired();
            e.Property(x => x.VoteDate).HasColumnName("vote_date").HasDefaultValueSql("NOW()");

            e.HasOne(x => x.User).WithMany(u => u.Votes)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Post).WithMany(p => p.Votes)
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Comment).WithMany(c => c.Votes)
                .HasForeignKey(x => x.CommentId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.UserId, x.PostId }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.CommentId }).IsUnique();
            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.CommentId);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("chk_votes_type", "vote_type IN (1, -1)");
                t.HasCheckConstraint("chk_votes_one_target",
                    "(post_id IS NOT NULL AND comment_id IS NULL) OR (post_id IS NULL AND comment_id IS NOT NULL)");
            });
        });

        mb.Entity<Ban>(e =>
        {
            e.ToTable("bans");
            e.HasKey(x => x.BanId);
            e.Property(x => x.BanId).HasColumnName("ban_id");
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.SubforumId).HasColumnName("subforum_id");
            e.Property(x => x.BannedBy).HasColumnName("banned_by");
            e.Property(x => x.BanReason).HasColumnName("ban_reason");
            e.Property(x => x.BannedDate).HasColumnName("banned_date").HasDefaultValueSql("NOW()");
            e.Property(x => x.ExpiresDate).HasColumnName("expires_date");

            e.HasOne(x => x.User).WithMany(u => u.ReceivedBans)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.BannedByUser).WithMany(u => u.IssuedBans)
                .HasForeignKey(x => x.BannedBy).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Subforum).WithMany(s => s.Bans)
                .HasForeignKey(x => x.SubforumId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.SubforumId);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("chk_bans_expires_date",
                    "expires_date IS NULL OR expires_date > banned_date");
            });
        });

        mb.Entity<SubforumRole>(e =>
        {
            e.ToTable("subforum_roles");
            e.HasKey(x => x.SubfRoleId);
            e.Property(x => x.SubfRoleId).HasColumnName("subf_role_id");
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.SubforumId).HasColumnName("subforum_id");
            e.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
            e.Property(x => x.AssignedAt).HasColumnName("assigned_at").HasDefaultValueSql("NOW()");

            e.HasOne(x => x.User).WithMany(u => u.SubforumRoles)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Subforum).WithMany(s => s.SubforumRoles)
                .HasForeignKey(x => x.SubforumId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(r => r.SubforumRoles)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.UserId, x.SubforumId, x.RoleId }).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.SubforumId);
        });
    }
}