namespace FoodieGuide.Web.Models
{
    using System.ComponentModel.DataAnnotations;

    public class SettingsViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, EmailAddress]
        public string Name { get; set; }

        [Phone]
        public string Phone { get; set; }

        [Display(Name = "Bio")]
        [StringLength(300)]
        public string Bio { get; set; }

        public string Street { get; set; }

        public string City { get; set; }

        public string Zip { get; set; }
    }
}