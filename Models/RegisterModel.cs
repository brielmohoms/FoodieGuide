namespace FoodieGuide.Web.Models
{
    using System.ComponentModel.DataAnnotations;

    public class RegisterModel
    {
        [Required] public string Name { get; set; }
        [Required] public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords must match")]
        public string ConfirmPassword { get; set; }
    }
}
