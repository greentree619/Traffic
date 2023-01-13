using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Traffic.Models;
using Traffic.Models.AccountViewModels;
using Traffic.Services;

namespace Traffic.Controllers
{
  [Authorize]
  [Route("[controller]/[action]")]
  public class AccountController : Controller
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger _logger;

    public AccountController(
      UserManager<ApplicationUser> userManager,
      SignInManager<ApplicationUser> signInManager,
      IEmailSender emailSender,
      ILogger<AccountController> logger)
    {
      this._userManager = userManager;
      this._signInManager = signInManager;
      this._emailSender = emailSender;
      this._logger = (ILogger) logger;
    }

    [Microsoft.AspNetCore.Mvc.TempData]
    public string ErrorMessage { get; set; }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string returnUrl = null)
    {
      AccountController accountController = this;
      await AuthenticationHttpContextExtensions.SignOutAsync(accountController.HttpContext, IdentityConstants.ExternalScheme);
      accountController.ViewData["ReturnUrl"] = (object) returnUrl;
      return (IActionResult) accountController.View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
      AccountController accountController = this;
      accountController.ViewData["ReturnUrl"] = (object) returnUrl;
      if (!accountController.ModelState.IsValid)
        return (IActionResult) accountController.View((object) model);
      Microsoft.AspNetCore.Identity.SignInResult signInResult = await accountController._signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
      if (signInResult.Succeeded)
      {
        accountController._logger.LogInformation("User logged in.");
        return accountController.RedirectToLocal(returnUrl);
      }
      if (signInResult.RequiresTwoFactor)
        return (IActionResult) accountController.RedirectToAction("LoginWith2fa", (object) new
        {
          returnUrl = returnUrl,
          RememberMe = model.RememberMe
        });
      if (signInResult.IsLockedOut)
      {
        accountController._logger.LogWarning("User account locked out.");
        return (IActionResult) accountController.RedirectToAction("Lockout");
      }
      accountController.ModelState.AddModelError(string.Empty, "Invalid login attempt.");
      return (IActionResult) accountController.View((object) model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
    {
      AccountController accountController = this;
      if (await accountController._signInManager.GetTwoFactorAuthenticationUserAsync() == null)
        throw new ApplicationException("Unable to load two-factor authentication user.");
      LoginWith2faViewModel model = new LoginWith2faViewModel()
      {
        RememberMe = rememberMe
      };
      accountController.ViewData["ReturnUrl"] = (object) returnUrl;
      return (IActionResult) accountController.View((object) model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(
      LoginWith2faViewModel model,
      bool rememberMe,
      string returnUrl = null)
    {
      AccountController accountController = this;
      if (!accountController.ModelState.IsValid)
        return (IActionResult) accountController.View((object) model);
      ApplicationUser user = await accountController._signInManager.GetTwoFactorAuthenticationUserAsync();
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + accountController._userManager.GetUserId(accountController.User) + "'.");
      string str = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
      Microsoft.AspNetCore.Identity.SignInResult signInResult = await accountController._signInManager.TwoFactorAuthenticatorSignInAsync(str, rememberMe, model.RememberMachine);
      if (signInResult.Succeeded)
      {
        accountController._logger.LogInformation("User with ID {UserId} logged in with 2fa.", (object) user.Id);
        return accountController.RedirectToLocal(returnUrl);
      }
      if (signInResult.IsLockedOut)
      {
        accountController._logger.LogWarning("User with ID {UserId} account locked out.", (object) user.Id);
        return (IActionResult) accountController.RedirectToAction("Lockout");
      }
      accountController._logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", (object) user.Id);
      accountController.ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
      return (IActionResult) accountController.View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
    {
      AccountController accountController = this;
      if (await accountController._signInManager.GetTwoFactorAuthenticationUserAsync() == null)
        throw new ApplicationException("Unable to load two-factor authentication user.");
      accountController.ViewData["ReturnUrl"] = (object) returnUrl;
      return (IActionResult) accountController.View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(
      LoginWithRecoveryCodeViewModel model,
      string returnUrl = null)
    {
      AccountController accountController = this;
      if (!accountController.ModelState.IsValid)
        return (IActionResult) accountController.View((object) model);
      ApplicationUser user = await accountController._signInManager.GetTwoFactorAuthenticationUserAsync();
      if (user == null)
        throw new ApplicationException("Unable to load two-factor authentication user.");
      string str = model.RecoveryCode.Replace(" ", string.Empty);
      Microsoft.AspNetCore.Identity.SignInResult signInResult = await accountController._signInManager.TwoFactorRecoveryCodeSignInAsync(str);
      if (signInResult.Succeeded)
      {
        accountController._logger.LogInformation("User with ID {UserId} logged in with a recovery code.", (object) user.Id);
        return accountController.RedirectToLocal(returnUrl);
      }
      if (signInResult.IsLockedOut)
      {
        accountController._logger.LogWarning("User with ID {UserId} account locked out.", (object) user.Id);
        return (IActionResult) accountController.RedirectToAction("Lockout");
      }
      accountController._logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", (object) user.Id);
      accountController.ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
      return (IActionResult) accountController.View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout() => (IActionResult) this.View();

    public async Task<IActionResult> DeleteUser(string id)
    {
      AccountController accountController = this;
      IEnumerable<ApplicationUser> source = (IEnumerable<ApplicationUser>) accountController._userManager.Users.Where<ApplicationUser>((Expression<Func<ApplicationUser, bool>>) (m => m.Id == id));
      if (source.Count<ApplicationUser>() != 1)
        return (IActionResult) accountController.RedirectToAction("Manage");
      IdentityResult identityResult = await accountController._userManager.DeleteAsync(source.ElementAt<ApplicationUser>(0));
      return (IActionResult) accountController.RedirectToAction("Manage");
    }

    public async Task<IActionResult> EditUser(string id)
    {
      AccountController accountController = this;
      if (!accountController.User.IsInRole("Admin"))
        return (IActionResult) accountController.RedirectToAction("Manage");
      IEnumerable<ApplicationUser> source1 = (IEnumerable<ApplicationUser>) accountController._userManager.Users.Where<ApplicationUser>((Expression<Func<ApplicationUser, bool>>) (r => r.Id == id));
      if (source1.Count<ApplicationUser>() != 1)
        return (IActionResult) accountController.RedirectToAction("Manage");
      RegisterViewModel model = new RegisterViewModel();
      model.Id = id;
      model.Email = source1.ElementAt<ApplicationUser>(0).Email;
      model.MaxJourneyCount = source1.ElementAt<ApplicationUser>(0).MaxJourneyCount;
      model.MaxRunCountPerDay = source1.ElementAt<ApplicationUser>(0).MaxRunCountPerDay;
      model.EnableCreateJobFromFile = source1.ElementAt<ApplicationUser>(0).EnableCreateJobFromFile;
      model.EnableGoogleSearchConsole = source1.ElementAt<ApplicationUser>(0).EnableGoogleSearchConsole;
      model.EnableSeeAdminLog = source1.ElementAt<ApplicationUser>(0).EnableSeeAdminLog;
      model.EnableScheduleInterupt = source1.ElementAt<ApplicationUser>(0).EnableScheduleInterupt;
      model.EnableGeoLocation = source1.ElementAt<ApplicationUser>(0).EnableGeoLocation;
      model.EnableGMB = source1.ElementAt<ApplicationUser>(0).EnableGMB;
      model.EnableHtml = source1.ElementAt<ApplicationUser>(0).EnableHtml;
      model.EnableTwitter = source1.ElementAt<ApplicationUser>(0).EnableTwitter;
      model.Password = "asdfQWER1234!@#$";
      model.ConfirmPassword = "asdfQWER1234!@#$";
      List<string> source2 = new List<string>();
      if (!string.IsNullOrEmpty(source1.ElementAt<ApplicationUser>(0).JourneyOptionList))
        source2 = ((IEnumerable<string>) source1.ElementAt<ApplicationUser>(0).JourneyOptionList.Split(";", StringSplitOptions.None)).ToList<string>();
      List<CheckboxJourney> list = ((IEnumerable<string>) SettingManager.JoureyOptions).Where<string>((Func<string, bool>) (x => !x.Contains("Google Form"))).Select<string, CheckboxJourney>((Func<string, CheckboxJourney>) (x => new CheckboxJourney()
      {
        Title = x,
        Selected = false
      })).ToList<CheckboxJourney>();
      foreach (CheckboxJourney checkboxJourney in list)
      {
        for (int index = 0; index < source2.Count<string>(); ++index)
        {
          if (source2[index] == checkboxJourney.Title)
          {
            checkboxJourney.Selected = true;
            break;
          }
        }
      }
      model.JourneyOptions = list;
      return (IActionResult) accountController.View((object) model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(
      RegisterViewModel model,
      List<string> selectoptions,
      string returnUrl = null)
    {
      AccountController accountController = this;
      if (!accountController.User.IsInRole("Admin"))
        return accountController.RedirectToLocal(returnUrl);
      accountController.ViewData["ReturnUrl"] = (object) returnUrl;
      if (accountController.ModelState.IsValid)
      {
        string str1 = "";
        IEnumerable<ApplicationUser> source = (IEnumerable<ApplicationUser>) accountController._userManager.Users.Where<ApplicationUser>((Expression<Func<ApplicationUser, bool>>) (r => r.Id == model.Id));
        if (source.Count<ApplicationUser>() != 1)
          return (IActionResult) accountController.RedirectToAction("Manage");
        ApplicationUser applicationUser = source.ElementAt<ApplicationUser>(0);
        List<string> list = ((IEnumerable<string>) SettingManager.JoureyOptions).Where<string>((Func<string, bool>) (x => !x.Contains("Google Form"))).ToList<string>();
        int index = 0;
        foreach (string str2 in list)
        {
          if (model.JourneyOptions.Count > index && model.JourneyOptions[index].Selected)
            str1 = str1 + str2 + ";";
          ++index;
        }
        applicationUser.MaxJourneyCount = model.MaxJourneyCount;
        applicationUser.MaxRunCountPerDay = model.MaxRunCountPerDay;
        applicationUser.EnableCreateJobFromFile = model.EnableCreateJobFromFile;
        applicationUser.EnableGoogleSearchConsole = model.EnableGoogleSearchConsole;
        applicationUser.EnableSeeAdminLog = model.EnableSeeAdminLog;
        applicationUser.EnableScheduleInterupt = model.EnableScheduleInterupt;
        applicationUser.EnableGeoLocation = model.EnableGeoLocation;
        applicationUser.EnableGMB = model.EnableGMB;
        applicationUser.EnableHtml = model.EnableHtml;
        applicationUser.EnableTwitter = model.EnableTwitter;
        applicationUser.JourneyOptionList = str1;
        IdentityResult result = await accountController._userManager.UpdateAsync(applicationUser);
        if (result.Succeeded)
        {
          accountController._logger.LogInformation("User Modified successfully");
          return (IActionResult) accountController.RedirectToAction("Manage");
        }
        accountController.AddErrors(result);
      }
      return (IActionResult) accountController.View((object) model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Manage(string returnUrl = null)
    {
      AccountController accountController = this;
      if (!accountController.User.IsInRole("Admin"))
        return accountController.RedirectToLocal(returnUrl);
      string currentUserID = accountController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
      List<ApplicationUser> list = accountController._userManager.Users.Where<ApplicationUser>((Expression<Func<ApplicationUser, bool>>) (x => x.Id != currentUserID)).ToList<ApplicationUser>();
      return (IActionResult) accountController.View((object) list);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register(string returnUrl = null)
    {
      AccountController accountController = this;
      if (!accountController.User.IsInRole("Admin"))
        return accountController.RedirectToLocal(returnUrl);
      accountController.ViewData["ReturnUrl"] = (object) returnUrl;
      return (IActionResult) accountController.View((object) new RegisterViewModel()
      {
        JourneyOptions = ((IEnumerable<string>) SettingManager.JoureyOptions).Where<string>((Func<string, bool>) (x => !x.Contains("Google Form"))).Select<string, CheckboxJourney>((Func<string, CheckboxJourney>) (x => new CheckboxJourney()
        {
          Title = x,
          Selected = false
        })).ToList<CheckboxJourney>()
      });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
      RegisterViewModel model,
      List<string> selectoptions,
      string returnUrl = null)
    {
      AccountController accountController = this;
      if (!accountController.User.IsInRole("Admin"))
        return accountController.RedirectToLocal(returnUrl);
      accountController.ViewData["ReturnUrl"] = (object) returnUrl;
      if (accountController.ModelState.IsValid)
      {
        string str1 = "";
        List<string> list = ((IEnumerable<string>) SettingManager.JoureyOptions).Where<string>((Func<string, bool>) (x => !x.Contains("Google Form"))).ToList<string>();
        int index = 0;
        foreach (string str2 in list)
        {
          if (model.JourneyOptions.Count > index && model.JourneyOptions[index].Selected)
            str1 = str1 + str2 + ";";
          ++index;
        }
        ApplicationUser applicationUser = new ApplicationUser();
        applicationUser.UserName = model.Email;
        applicationUser.Email = model.Email;
        applicationUser.MaxJourneyCount = model.MaxJourneyCount;
        applicationUser.MaxRunCountPerDay = model.MaxRunCountPerDay;
        applicationUser.EnableCreateJobFromFile = model.EnableCreateJobFromFile;
        applicationUser.EnableGoogleSearchConsole = model.EnableGoogleSearchConsole;
        applicationUser.EnableSeeAdminLog = model.EnableSeeAdminLog;
        applicationUser.EnableScheduleInterupt = model.EnableScheduleInterupt;
        applicationUser.EnableGeoLocation = model.EnableGeoLocation;
        applicationUser.EnableGMB = model.EnableGMB;
        applicationUser.EnableHtml = model.EnableHtml;
        applicationUser.EnableTwitter = model.EnableTwitter;
        applicationUser.JourneyOptionList = str1;
        ApplicationUser user = applicationUser;
        IdentityResult async = await accountController._userManager.CreateAsync(user, model.Password);
        if (async.Succeeded)
        {
          IdentityResult roleAsync = await accountController._userManager.AddToRoleAsync(user, "Member");
          accountController._logger.LogInformation("User created a new account with password.");
          string confirmationTokenAsync = await accountController._userManager.GenerateEmailConfirmationTokenAsync(user);
          string link = accountController.Url.EmailConfirmationLink(user.Id, confirmationTokenAsync, accountController.Request.Scheme);
          await accountController._emailSender.SendEmailConfirmationAsync(model.Email, link);
          return (IActionResult) accountController.RedirectToAction("Manage");
        }
        accountController.AddErrors(async);
        user = (ApplicationUser) null;
      }
      return (IActionResult) accountController.View((object) model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
      AccountController accountController = this;
      await accountController._signInManager.SignOutAsync();
      accountController._logger.LogInformation("User logged out.");
      return (IActionResult) accountController.RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
      string redirectUrl = this.Url.Action("ExternalLoginCallback", "Account", (object) new
      {
        returnUrl = returnUrl
      });
      return (IActionResult) this.Challenge(this._signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl), provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
    {
      AccountController accountController = this;
      if (remoteError != null)
      {
        accountController.ErrorMessage = "Error from external provider: " + remoteError;
        return (IActionResult) accountController.RedirectToAction("Login");
      }
      ExternalLoginInfo info = await accountController._signInManager.GetExternalLoginInfoAsync((string) null);
      if (info == null)
        return (IActionResult) accountController.RedirectToAction("Login");
      Microsoft.AspNetCore.Identity.SignInResult signInResult = await accountController._signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false, true);
      if (signInResult.Succeeded)
      {
        accountController._logger.LogInformation("User logged in with {Name} provider.", (object) info.LoginProvider);
        return accountController.RedirectToLocal(returnUrl);
      }
      if (signInResult.IsLockedOut)
        return (IActionResult) accountController.RedirectToAction("Lockout");
      accountController.ViewData["ReturnUrl"] = (object) returnUrl;
      accountController.ViewData["LoginProvider"] = (object) info.LoginProvider;
      string firstValue = info.Principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
      return (IActionResult) accountController.View("ExternalLogin", (object) new ExternalLoginViewModel()
      {
        Email = firstValue
      });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(
      ExternalLoginViewModel model,
      string returnUrl = null)
    {
      AccountController accountController = this;
      if (accountController.ModelState.IsValid)
      {
        ExternalLoginInfo info = await accountController._signInManager.GetExternalLoginInfoAsync((string) null);
        if (info == null)
          throw new ApplicationException("Error loading external login information during confirmation.");
        ApplicationUser applicationUser = new ApplicationUser();
        applicationUser.UserName = model.Email;
        applicationUser.Email = model.Email;
        ApplicationUser user = applicationUser;
        IdentityResult result = await accountController._userManager.CreateAsync(user);
        if (result.Succeeded)
        {
          result = await accountController._userManager.AddLoginAsync(user, (UserLoginInfo) info);
          if (result.Succeeded)
          {
            await accountController._signInManager.SignInAsync(user, false, (string) null);
            accountController._logger.LogInformation("User created an account using {Name} provider.", (object) info.LoginProvider);
            return accountController.RedirectToLocal(returnUrl);
          }
        }
        accountController.AddErrors(result);
        info = (ExternalLoginInfo) null;
        user = (ApplicationUser) null;
      }
      accountController.ViewData["ReturnUrl"] = (object) returnUrl;
      return (IActionResult) accountController.View("ExternalLogin", (object) model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
      AccountController accountController = this;
      if (userId == null || code == null)
        return (IActionResult) accountController.RedirectToAction("Index", "Home");
      ApplicationUser byIdAsync = await accountController._userManager.FindByIdAsync(userId);
      if (byIdAsync == null)
        throw new ApplicationException("Unable to load user with ID '" + userId + "'.");
      IdentityResult identityResult = await accountController._userManager.ConfirmEmailAsync(byIdAsync, code);
      return (IActionResult) accountController.View(identityResult.Succeeded ? nameof (ConfirmEmail) : "Error");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => (IActionResult) this.View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
      AccountController accountController = this;
      if (!accountController.ModelState.IsValid)
        return (IActionResult) accountController.View((object) model);
      ApplicationUser user = await accountController._userManager.FindByEmailAsync(model.Email);
      bool flag = user == null;
      if (!flag)
        flag = !await accountController._userManager.IsEmailConfirmedAsync(user);
      if (flag)
        return (IActionResult) accountController.RedirectToAction("ForgotPasswordConfirmation");
      string passwordResetTokenAsync = await accountController._userManager.GeneratePasswordResetTokenAsync(user);
      string str = accountController.Url.ResetPasswordCallbackLink(user.Id, passwordResetTokenAsync, accountController.Request.Scheme);
      await accountController._emailSender.SendEmailAsync(model.Email, "Reset Password", "Please reset your password by clicking here: <a href='" + str + "'>link</a>");
      return (IActionResult) accountController.RedirectToAction("ForgotPasswordConfirmation");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => (IActionResult) this.View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string code = null) => code != null ? (IActionResult) this.View((object) new ResetPasswordViewModel()
    {
      Code = code
    }) : throw new ApplicationException("A code must be supplied for password reset.");

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
      AccountController accountController = this;
      if (!accountController.ModelState.IsValid)
        return (IActionResult) accountController.View((object) model);
      ApplicationUser byEmailAsync = await accountController._userManager.FindByEmailAsync(model.Email);
      if (byEmailAsync == null)
        return (IActionResult) accountController.RedirectToAction("ResetPasswordConfirmation");
      IdentityResult result = await accountController._userManager.ResetPasswordAsync(byEmailAsync, model.Code, model.Password);
      if (result.Succeeded)
        return (IActionResult) accountController.RedirectToAction("ResetPasswordConfirmation");
      accountController.AddErrors(result);
      return (IActionResult) accountController.View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => (IActionResult) this.View();

    [HttpGet]
    public IActionResult AccessDenied() => (IActionResult) this.View();

    private void AddErrors(IdentityResult result)
    {
      foreach (IdentityError error in result.Errors)
        this.ModelState.AddModelError(string.Empty, error.Description);
    }

    private IActionResult RedirectToLocal(string returnUrl) => this.Url.IsLocalUrl(returnUrl) ? (IActionResult) this.Redirect(returnUrl) : (IActionResult) this.RedirectToAction("Index", "Home");
  }
}
