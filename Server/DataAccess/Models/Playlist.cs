using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class Playlist
    {
        [Key]
        public int PlaylistId { get; set; }
        public string PlaylistName { get; set; }
        public bool IsDeleted { get; set; }
        public string UserId { get; set; }
        public StreamingUser StreamingUser { get; set; }
        public ICollection<PlaylistSong> PlaylistSongs { get; set; }
    }
}
