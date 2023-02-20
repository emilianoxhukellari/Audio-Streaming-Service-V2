using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class SongInput
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Song Name")]
        public string SongName { get; set; }
        [Required]
        [StringLength(200)]
        [Display(Name = "Artist Name")]
        public string ArtistName { get; set; }
        [Required]
        [Display(Name = "Image File")]
        public IFormFile ImageFile { get; set; }
        [Required]
        [Display(Name = "Song File")]
        public IFormFile SongFile { get; set; }
    }
}
