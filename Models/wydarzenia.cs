using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Permissions;
using Microsoft.AspNetCore.Identity;

namespace Grzmotoptak.Models
{
    public class wydarzenia
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Tytul { get; set; }
        [Required]
        public string Opis { get; set; }
        [Required]
        public DateTime Data { get; set; }
        [Required]
        public string Typ { get; set; }
        public string? tworca_id { get; set; }
        [ForeignKey("tworca_id")]
        public IdentityUser? User { get; set; }
     
        public string? Obrazek { get; set; }

        // Metoda do przypisania tworca_id
        public void SetTworcaId(string userId)
        {
            if (string.IsNullOrEmpty(tworca_id))
            {
                tworca_id = userId;
            }
        }
    }

}

