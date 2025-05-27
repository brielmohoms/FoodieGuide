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

            TempData["RegistrationSuccess"] = "Your account has been created successfully. Please proceed to login.";

            return Redirect("/login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HandleLogin(LoginModel model)
        {
            if (!ModelState.IsValid)
                return CurrentUmbracoPage();

            var result = _signInManager.PasswordSignInAsync(model.Username, model.Password, false, false).Result;
            if (result.Succeeded)
            {
                // Flag for your view
                TempData["LoginSuccess"] = "Welcome back!";
                // Redirect to the site root (no hard-coded ID)
                return Redirect("/");
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

        [HttpGet]
        public async Task<IActionResult> Account()
        {
            var member = await _memberManager.GetCurrentMemberAsync();

            var vm = new AccountViewModel
            {
                Username = member.UserName,
                Name = member.Name,
                Email = member.Email
            };

            return View("AccountPage", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var member = await _memberManager.GetCurrentMemberAsync();

            var vm = new SettingsViewModel
            {
                Email = member.Email
            };

            return View("SettingsPage", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SettingsViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var member = await _memberManager.GetCurrentMemberAsync();

            member.Email = vm.Email;
            var result = await _memberManager.UpdateAsync(member);

            if (result.Succeeded)
            {
                TempData["SettingsSaved"] = "Profile updated.";
                return RedirectToCurrentUmbracoPage();
            }

            ModelState.AddModelError("", result.Errors.First().Description);
            return CurrentUmbracoPage();
        }
    }

}
