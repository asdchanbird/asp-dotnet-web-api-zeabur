using System;
using System.Collections.Generic;
using ChitChit.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ChitChit.Models
{
    public partial class chitchitContext : IdentityDbContext<ChitChitUser>
    {
        public chitchitContext()
        {
        }

        public chitchitContext(DbContextOptions<chitchitContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);

            modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.HasKey(l => new { l.LoginProvider, l.ProviderKey });
            });
            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.HasKey(ir => new { ir.UserId, ir.RoleId });
            });
            modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
            });
        }



        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

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
        public DbSet<News> News { get; set; }
        public DbSet<FriendRequest> FriendRequest { get; set; }
        public DbSet<FriendBlocked> FriendBlocked { get; set; }
    }
}
