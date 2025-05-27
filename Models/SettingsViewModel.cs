namespace FoodieGuide.Web.Models
{
    using System.ComponentModel.DataAnnotations;

    public class SettingsViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}