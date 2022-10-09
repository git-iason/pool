// Licensed to the.NET Foundation under one or more agreements.

// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

using System.ComponentModel.DataAnnotations;

using System.Security.Claims;

using System.Text;

using System.Text.Encodings.Web;

using System.Threading;

using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using Microsoft.Extensions.Options;

using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Identity.UI.Services;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.RazorPages;

using Microsoft.AspNetCore.WebUtilities;

using Microsoft.Extensions.Logging;

using hosted_pool.Data;
namespace hosted_pool.Areas.Identity.Pages.Account
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;

        private readonly IUserEmailStore<IdentityUser> _emailStore;

        private readonly IEmailSender _emailSender;

        private readonly ILogger<ExternalLoginModel> _logger;

        private readonly PoolService poolService;
        public ExternalLoginModel(

            SignInManager<IdentityUser> signInManager,

            UserManager<IdentityUser> userManager,

            IUserStore<IdentityUser> userStore,

            ILogger<ExternalLoginModel> logger,

            IEmailSender emailSender)

        {

            _signInManager = signInManager;

            _userManager = userManager;

            _userStore = userStore;

            _emailStore = GetEmailStore();

            _logger = logger;

            _emailSender = emailSender;

            poolService = new PoolService();
        }

        [BindProperty]

        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]

        public string ErrorMessage { get; set; }

        [ViewData]

        public string AssociateExistingAccount { get; set; } = "false";

        public class InputModel

        {

            [Required]

            [EmailAddress]

            public string Email { get; set; }

            [DataType(DataType.Password)]

            public string Password { get; set; }

        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string returnUrl = null)

        {

            // Request a redirect to the external login provider.

            var redirectUrl =
            Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });

            var properties =

                _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return new ChallengeResult(provider, properties);

        }

        public async Task<IActionResult> OnGetCallbackAsync(

            string returnUrl = null, string remoteError = null)

        {

            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)

            {

                ErrorMessage = $"Error from external provider: {remoteError}";

                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });

            }

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)

            {

                ErrorMessage = "Error loading external login information.";

                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });

            }

            // Sign in the user with this external login provider if the user already has a login.

            var result = await _signInManager.ExternalLoginSignInAsync(

                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)

            {
                return LocalRedirect(returnUrl);

            }

            if (result.IsLockedOut)

            {

                return RedirectToPage("./Lockout");

            }

            else

            {

                var userEmail = info.Principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email).Value.ToString();
                var userName = info.Principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();
                var userSplit = userName.Split(" ");
                var userCombined = string.Join(".", userSplit);
                var user = new IdentityUser { UserName = userCombined, Email = userEmail, NormalizedUserName=userName };
                var resultCreateUser = await _userManager.CreateAsync(user);
                if (resultCreateUser.Succeeded)
                {
                    var resultAddLogin = await _userManager.AddLoginAsync(user, info);
                    if (resultAddLogin.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }

                return Page();

            }

        }

        private IdentityUser CreateUser()

        {

            try

            {

                return Activator.CreateInstance<IdentityUser>();

            }

            catch

            {

                throw new InvalidOperationException($"Can't create an instance of " +

                    $"'{nameof(IdentityUser)}'. " +

                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class " +

                    $"and has a parameterless constructor, or alternatively " +

                    $"override the external login page in " +

                    $"/Areas/Identity/Pages/Account/ExternalLogin.cshtml");

            }

        }

        private IUserEmailStore<IdentityUser> GetEmailStore()

        {

            if (!_userManager.SupportsUserEmail)

            {

                throw new NotSupportedException(

                    "The default UI requires a user store with email support.");

            }

            return (IUserEmailStore<IdentityUser>)_userStore;

        }

    }

}