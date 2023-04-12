using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Contexts
{
    /// <summary>
    /// Streaming Db Context contains Songs, Playlists, PlaylistSongs, StreamingUsers, and Issues.
    /// </summary>
    public class StreamingDbContext : DbContext
    {
        public StreamingDbContext(DbContextOptions<StreamingDbContext> options) : base(options) { }
        public DbSet<Song> Songs { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistSong> PlaylistSongs { get; set; }
        public DbSet<StreamingUser> StreamingUsers { get; set; }
        public DbSet<Issue> Issues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Playlist>()
            .HasOne(p => p.StreamingUser)
            .WithMany(u => u.Playlists)
            .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<PlaylistSong>()
                .HasKey(ps => new { ps.PlaylistId, ps.SongId });

            modelBuilder.Entity<PlaylistSong>()
                .HasOne(ps => ps.Playlist)
                .WithMany(p => p.PlaylistSongs)
                .HasForeignKey(ps => ps.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlaylistSong>()
                .HasOne(ps => ps.Song)
                .WithMany(p => p.PlaylistSongs)
                .HasForeignKey(ps => ps.SongId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Song>()
                .HasMany(s => s.PlaylistSongs)
                .WithOne(ps => ps.Song)
                .HasForeignKey(ps => ps.SongId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
