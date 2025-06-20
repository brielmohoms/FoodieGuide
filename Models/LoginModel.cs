namespace FoodieGuide.Web.Models
{
    using System.ComponentModel.DataAnnotations;

    public class LoginModel
    {
        [Required] public string Username { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        public string? ReturnUrl { get; set; }

        public bool RememberMe { get; set; }
    }
}
