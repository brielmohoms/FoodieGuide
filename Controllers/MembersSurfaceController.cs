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

        private readonly ITwoFactorLoginService _twoFactor;

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
            IMemberSignInManager signInManager,
            ITwoFactorLoginService twoFactor
        ) : base(umbracoContextAccessor, databaseFactory, serviceContext, appCaches,
        profilingLogger, publishedUrlProvider)
        {
            _mediaFileManager = mediaFileManager;
            _memberService = memberService;
            _memberManager = memberManager;
            _signInManager = signInManager;
            _twoFactor = twoFactor;
        }

        [HttpGet]
        [Route("register")]
        public IActionResult Register()
        {
            var vm = new RegisterModel();
            return View("RegisterPage", vm);
        }

        [HttpPost]
        public async Task<IActionResult> HandleRegister(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData.Model = model;
                return CurrentUmbracoPage();

            }

            if (await _memberManager.FindByEmailAsync(model.Email) != null
            || await _memberManager.FindByNameAsync(model.Username) != null)
            {
                ModelState.AddModelError("", "Username or email already exists");
                ViewData.Model = model;
                return CurrentUmbracoPage();

            }

            var member = new MemberIdentityUser
            {
                UserName = model.Username,
                Name = model.Name,
                Email = model.Email,
                MemberTypeAlias = "Member",
                IsApproved      = true
            };

            var create = await _memberManager.CreateAsync(member, model.Password);
            if (!create.Succeeded)
            {
                ModelState.AddModelError("", create.Errors.First().Description);

                ViewData.Model = model;
                return CurrentUmbracoPage();

            }

            TempData["RegistrationSuccess"] = "Your account has been created successfully. Please proceed to login.";

            return Redirect("/login");
        }

        [HttpGet]
        [Route("login")]
        public IActionResult Login()
        {
            var vm = new LoginModel();
            return View("LoginPage", vm);
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HandleLogin(LoginModel model)
        {
            if (!ModelState.IsValid) return View("LoginPage", model);

            var result = await _signInManager.PasswordSignInAsync(
                             model.Username, model.Password,
                             model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                TempData["LoginSuccess"] = "Login successful. Welcome back!";
                return Redirect("/");
            }

            ModelState.AddModelError("", "Invalid username or password");
            return View("LoginPage", model);
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
