using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
