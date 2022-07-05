using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Oyster.Areas.Identity.Data;

namespace Oyster.Data {
public class OysterContext : IdentityDbContext<OysterUser> {
  public OysterContext(DbContextOptions<OysterContext> options)
    : base(options) { }

  public DbSet<Comment> Comments { get; set; }
  public DbSet<Event> Events { get; set; }
  public DbSet<OysterGroup> OysterGroups { get; set; }
  public DbSet<Photo> Photos { get; set; }
  public DbSet<Post> Posts { get; set; }
  public DbSet<Poll> Polls { get; set; }
  public DbSet<PollOption> PollOptions { get; set; }

  protected override void OnModelCreating(ModelBuilder builder) {
    base.OnModelCreating(builder);

    builder.HasSequence<int>("UniqueUserNumber").StartsAt(1).IncrementsBy(1);
    builder.HasSequence<int>("UniquePhotoId").StartsAt(1).IncrementsBy(1);

    builder.Entity<OysterUser>()
      .HasIndex(u => u.PublicUsername)
      .IsUnique();

    builder.Entity<OysterUser>()
      .HasIndex(u => u.DisplayName);
      
    builder.Entity<OysterGroup>()
      .HasMany(p => p.Members)
      .WithMany(p => p.Groups)
      .UsingEntity<Dictionary<string, object>>(
        "OysterUsersInOysterGroups",
        j => j
          .HasOne<OysterUser>()
          .WithMany()
          .HasForeignKey("UserId"),
        j => j
          .HasOne<OysterGroup>()
          .WithMany()
          .HasForeignKey("GroupId"));

    builder.Entity<PollOption>()
      .HasMany(p => p.Voters)
      .WithMany(p => p.Votes)
      .UsingEntity<Dictionary<string, object>>(
        "VotersInPollOption",
        j => j
          .HasOne<OysterUser>()
          .WithMany()
          .HasForeignKey("UserId"),
        j => j
          .HasOne<PollOption>()
          .WithMany()
          .HasForeignKey("PollOptionId"));
    
    builder.Entity<Post>()
      .HasOne(p => p.Poll)
      .WithOne(p => p.Post)
      .HasForeignKey<Poll>(p => p.Id);

    builder.Entity<OysterGroup>()
      .HasOne(p => p.Founder)
      .WithMany(p => p.GroupsCreated);

    builder.Entity<OysterUser>().ToTable("OysterUsers");
    builder.Entity<Comment>().ToTable("Comments");
    builder.Entity<Event>().ToTable("Events");
    builder.Entity<OysterGroup>().ToTable("OysterGroups");
    builder.Entity<Photo>().ToTable("Photos");
    builder.Entity<Post>().ToTable("Posts");
    builder.Entity<Poll>().ToTable("Polls");
    builder.Entity<PollOption>().ToTable("PollOptions");
    // builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
    // builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
    // builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
    // builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
    // builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
  }

// Customize the ASP.NET Identity model and override the defaults if needed.
// For example, you can rename the ASP.NET Identity table names and more.
// Add your customizations after calling base.OnModelCreating(builder);
}
}