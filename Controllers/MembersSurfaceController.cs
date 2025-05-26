using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using FoodieGuide.Web.Models;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Security;

namespace FoodieGuide.Web.Controllers
{
    public class MembersSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMemberSignInManager _signInManager;

        public MembersSurfaceController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IMemberManager memberManager,
            IMemberSignInManager signInManager
        ) : base(umbracoContextAccessor, databaseFactory, serviceContext, appCaches,
        profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager;
            _signInManager = signInManager;
        }

        [HttpPost]
        public async Task<IActionResult> HandleRegister(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            if (await _memberManager.FindByEmailAsync(model.Email) != null
            || await _memberManager.FindByNameAsync(model.Username) != null)
            {
                ModelState.AddModelError("", "Username or email already exists");
                return CurrentUmbracoPage();
            }

            var member = new MemberIdentityUser
            {
                UserName = model.Username,
                Name = model.Name,
                Email = model.Email,
                MemberTypeAlias = "Member"
            };

            var create = await _memberManager.CreateAsync(member, model.Password);
            if (!create.Succeeded)
            {
                ModelState.AddModelError("", create.Errors.First().Description);
                return CurrentUmbracoPage();
            }

            TempData["RegistrationSuccess"] = "Registration complete. Proceed to login";
            return RedirectToCurrentUmbracoPage();
        }

        [HttpPost]
        public async Task<IActionResult> HandleLogin(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var result = await _signInManager.PasswordSignInAsync(
                userName: model.Username,
                password: model.Password,
                isPersistent: false,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                var returnUrl = !string.IsNullOrWhiteSpace(model.ReturnUrl)
                                && Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : "/";

                return Redirect(returnUrl);
            }

            ModelState.AddModelError("", "Invalid username or password");

            return CurrentUmbracoPage();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/");
        }
    }
}
