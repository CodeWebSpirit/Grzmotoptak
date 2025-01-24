using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grzmotoptak.Models
{
    public class Zapisy
    {
        public int Id { get; set; }

        // Id użytkownika (używamy IdentityUser)

        public string UserId { get; set; }

        // Id wydarzenia
        [Required]
        public int EventId { get; set; }

        // Powiązanie z użytkownikiem IdentityUser
        public virtual IdentityUser User { get; set; }

        // Powiązanie z wydarzeniem
        [ForeignKey("EventId")]
        public virtual wydarzenia Event { get; set; }

        public Zapisy(string userId, int eventId)
        {
            UserId = userId;
            EventId = eventId;
        }
        

    }
}
