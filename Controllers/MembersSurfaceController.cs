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
using Umbraco.Cms.Core.IO;

namespace FoodieGuide.Web.Controllers
{
    public class MembersSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;

        private readonly IMemberSignInManager _signInManager;

        private readonly IMemberService _memberService;

        private readonly MediaFileManager _mediaFileManager;

        public MembersSurfaceController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IMemberService memberService,
            MediaFileManager mediaFileManager,
            IMemberManager memberManager,
            IMemberSignInManager signInManager
        ) : base(umbracoContextAccessor, databaseFactory, serviceContext, appCaches,
        profilingLogger, publishedUrlProvider)
        {
            _mediaFileManager = mediaFileManager;
            _memberService = memberService;
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

            var result = _signInManager.PasswordSignInAsync(model.Username, model.Password,
            false, false).Result;

            if (result.Succeeded)
            {
                TempData["LoginSuccess"] = "Login successful. Welcome back!";
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
            var memberUser = await _memberManager.GetCurrentMemberAsync();
            var memberEntity = _memberService.GetByKey(memberUser.Key);

            var vm = new AccountViewModel
            {
                Username = memberUser.UserName,
                Name = memberUser.Name,
                Email = memberUser.Email,

                Phone = memberEntity.GetValue<string>("phone"),
                Bio = memberEntity.GetValue<string>("bio"),
                Street = memberEntity.GetValue<string>("street"),
                City = memberEntity.GetValue<string>("city"),
                Zip = memberEntity.GetValue<string>("zip")
            };

            return View("AccountPage", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var memberUser = await _memberManager.GetCurrentMemberAsync();

            var memberEntity = _memberService.GetByKey(memberUser.Key);

            var vm = new SettingsViewModel
            {
                Email = memberUser.Email,
                Name = memberUser.Name,

                Phone = memberEntity.GetValue<string>("phone"),
                Bio = memberEntity.GetValue<string>("bio"),
                Street = memberEntity.GetValue<string>("street"),
                City = memberEntity.GetValue<string>("city"),
                Zip = memberEntity.GetValue<string>("zip")
            };

            return View("SettingsPage", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SettingsViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View("SettingsPage", vm);
            }

            var memberUser = await _memberManager.GetCurrentMemberAsync();
            var memberEntity = _memberService.GetByKey(memberUser.Key);

            memberUser.Email = vm.Email;
            memberUser.Name = vm.Name;

            memberEntity.SetValue("phone", vm.Phone);
            memberEntity.SetValue("bio", vm.Bio);
            memberEntity.SetValue("street", vm.Street);
            memberEntity.SetValue("city", vm.City);
            memberEntity.SetValue("zip", vm.Zip);

            _memberService.Save(memberEntity);
            await _memberManager.UpdateAsync(memberUser);

            TempData["SettingsSaved"] = "Profile updated.";
            return RedirectToAction(nameof(Settings));
        }
    }

}
