using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    public class StreamingUser
    {
        [Key]
        public string UserId { get; set; }
        public string UserName { get; set; }
        public ICollection<Playlist> Playlists { get; set; }
    }
}
