using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NoteBoard.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Board> Boards { get; set; } = null!;

        public DbSet<Note> Notes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Board

            builder.Entity<Board>()
                .Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<Board>()
                .Property(b => b.Description)
                .HasMaxLength(500);

            // Note

            builder.Entity<Note>()
                .Property(n => n.Caption)
                .HasMaxLength(100);

            builder.Entity<Note>()
                .Property(n => n.Content)
                .HasMaxLength(1000);

            // Relationships

            builder.Entity<AppUser>()
                .HasMany(u => u.Boards)
                .WithOne(b => b.User);

            builder.Entity<Board>()
                .HasMany(b => b.Notes)
                .WithOne(n => n.Board);
        }
    }
}
