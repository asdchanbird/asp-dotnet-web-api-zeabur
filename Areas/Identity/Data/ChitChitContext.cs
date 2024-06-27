using ChitChit.Areas.Identity.Data;
using ChitChit.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChitChit.Areas.Identity.Data;

public class ChitChitContext : IdentityDbContext<ChitChitUser>
{
    public ChitChitContext(DbContextOptions<ChitChitContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
        //builder.Entity<GroupMessage>()
        //    .HasOne<GroupChat>()
        //    .WithMany(m => m.GroupMessage)
        //    .HasForeignKey(m => m.GroupChatId)
        //    .OnDelete(DeleteBehavior.SetNull);

        //builder.Entity<GroupReadMessage>()
        //    .HasOne<GroupChat>()
        //    .WithMany(m => m.GroupReadMessage)
        //    .HasForeignKey(m => m.GroupChatId)
        //    .OnDelete(DeleteBehavior.SetNull);
        //builder.Entity<GroupRel>
        //builder.Entity<PostComment>
        //builder.Entity<PostImageRepo>
        //builder.Entity<PostLike>
        //builder.Entity<PrivateChat>
        //builder.Entity<PrivateReadMessage>


    }

    public DbSet<ChitChitUser> ChitChitUser { get; set; }
    public DbSet<GroupChat> GroupChat { get; set; }
    public DbSet<PrivateChat> PrivateChat { get; set; }
    public DbSet<FriendRel> FriendRel { get; set; }
    public DbSet<GroupMessage> GroupMessage { get; set; }
    public DbSet<GroupReadMessage> GroupReadMessage { get; set; }
    public DbSet<GroupRel> GroupRel { get; set; }
    public DbSet<PersonalSetup> PersonalSetup { get; set; }
    public DbSet<PostComment> PostComment { get; set; }
    public DbSet<PostHashtag> PostHashtag { get; set; }
    public DbSet<PostImageRepo> PostImageRepo { get; set; }
    public DbSet<PostLike> PostLike { get; set; }
    public DbSet<PrivateMessage> PrivateMessage { get; set; }
    public DbSet<PrivateReadMessage> PrivateReadMessage { get; set; }
    public DbSet<Post> Post { get; set; }
    public DbSet<News> News {  get; set; }
    public DbSet<FriendRequest> FriendRequest {  get; set; }
    public DbSet<FriendBlocked> FriendBlocked { get; set; }



}
