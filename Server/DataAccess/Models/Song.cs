using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    public class Song
    {
        [Key]
        public int SongId { get; set; }
        [Required]
        [StringLength(200)]
        public string SongName { get; set; }
        [Required]
        [StringLength(200)]
        public string ArtistName { get; set; }
        [Required]
        [StringLength(200)]
        public string NormalizedSongName { get; set; }
        [Required]
        [StringLength(200)]
        public string NormalizedArtistname { get; set; }
        [Required]
        public double Duration { get; set; }
        [Required]
        public string SongFileName { get; set; }
        [Required]
        public string ImageFileName { get; set; }
        public ICollection<PlaylistSong> PlaylistSongs { get; set; }
    }
}
