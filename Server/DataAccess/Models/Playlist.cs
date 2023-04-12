using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    public class Playlist
    {
        [Key]
        public int PlaylistId { get; set; }
        public string PlaylistName { get; set; }
        public double Duration { get; set; }
        public bool IsDeleted { get; set; }
        public string UserId { get; set; }
        public StreamingUser StreamingUser { get; set; }
        public ICollection<PlaylistSong> PlaylistSongs { get; set; }
    }
}
