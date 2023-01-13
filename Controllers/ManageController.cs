using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Traffic.Models;
using Traffic.Models.ManageViewModels;
using Traffic.Services;

namespace Traffic.Controllers
{
  [Authorize]
  [Route("[controller]/[action]")]
  public class ManageController : Controller
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger _logger;
    private readonly UrlEncoder _urlEncoder;
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
    private const string RecoveryCodesKey = "RecoveryCodesKey";

    public ManageController(
      UserManager<ApplicationUser> userManager,
      SignInManager<ApplicationUser> signInManager,
      IEmailSender emailSender,
      ILogger<ManageController> logger,
      UrlEncoder urlEncoder)
    {
      this._userManager = userManager;
      this._signInManager = signInManager;
      this._emailSender = emailSender;
      this._logger = (ILogger) logger;
      this._urlEncoder = urlEncoder;
    }

    [Microsoft.AspNetCore.Mvc.TempData]
    public string StatusMessage { get; set; }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
      ManageController manageController = this;
      ApplicationUser userAsync = await manageController._userManager.GetUserAsync(manageController.User);
      if (userAsync == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      IndexViewModel model = new IndexViewModel()
      {
        Username = userAsync.UserName,
        Email = userAsync.Email,
        PhoneNumber = userAsync.PhoneNumber,
        IsEmailConfirmed = userAsync.EmailConfirmed,
        StatusMessage = manageController.StatusMessage
      };
      return (IActionResult) manageController.View((object) model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IndexViewModel model)
    {
      ManageController manageController = this;
      if (!manageController.ModelState.IsValid)
        return (IActionResult) manageController.View((object) model);
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      if (model.Email != user.Email)
      {
        if (!(await manageController._userManager.SetEmailAsync(user, model.Email)).Succeeded)
          throw new ApplicationException("Unexpected error occurred setting email for user with ID '" + user.Id + "'.");
      }
      if (model.PhoneNumber != user.PhoneNumber)
      {
        if (!(await manageController._userManager.SetPhoneNumberAsync(user, model.PhoneNumber)).Succeeded)
          throw new ApplicationException("Unexpected error occurred setting phone number for user with ID '" + user.Id + "'.");
      }
      manageController.StatusMessage = "Your profile has been updated";
      return (IActionResult) manageController.RedirectToAction(nameof (Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendVerificationEmail(IndexViewModel model)
    {
      ManageController manageController = this;
      if (!manageController.ModelState.IsValid)
        return (IActionResult) manageController.View((object) model);
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      string confirmationTokenAsync = await manageController._userManager.GenerateEmailConfirmationTokenAsync(user);
      string link = manageController.Url.EmailConfirmationLink(user.Id, confirmationTokenAsync, manageController.Request.Scheme);
      string email = user.Email;
      await manageController._emailSender.SendEmailConfirmationAsync(email, link);
      manageController.StatusMessage = "Verification email sent. Please check your email.";
      return (IActionResult) manageController.RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
      ManageController manageController = this;
      ApplicationUser userAsync = await manageController._userManager.GetUserAsync(manageController.User);
      if (userAsync == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      if (!await manageController._userManager.HasPasswordAsync(userAsync))
        return (IActionResult) manageController.RedirectToAction("SetPassword");
      ChangePasswordViewModel model = new ChangePasswordViewModel()
      {
        StatusMessage = manageController.StatusMessage
      };
      return (IActionResult) manageController.View((object) model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
      ManageController manageController = this;
      if (!manageController.ModelState.IsValid)
        return (IActionResult) manageController.View((object) model);
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      IdentityResult result = await manageController._userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
      if (!result.Succeeded)
      {
        manageController.AddErrors(result);
        return (IActionResult) manageController.View((object) model);
      }
      await manageController._signInManager.SignInAsync(user, false, (string) null);
      manageController._logger.LogInformation("User changed their password successfully.");
      manageController.StatusMessage = "Your password has been changed.";
      return (IActionResult) manageController.RedirectToAction(nameof (ChangePassword));
    }

    [HttpGet]
    public async Task<IActionResult> SetPassword()
    {
      ManageController manageController = this;
      ApplicationUser userAsync = await manageController._userManager.GetUserAsync(manageController.User);
      if (userAsync == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      if (await manageController._userManager.HasPasswordAsync(userAsync))
        return (IActionResult) manageController.RedirectToAction("ChangePassword");
      SetPasswordViewModel model = new SetPasswordViewModel()
      {
        StatusMessage = manageController.StatusMessage
      };
      return (IActionResult) manageController.View((object) model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
      ManageController manageController = this;
      if (!manageController.ModelState.IsValid)
        return (IActionResult) manageController.View((object) model);
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      IdentityResult result = await manageController._userManager.AddPasswordAsync(user, model.NewPassword);
      if (!result.Succeeded)
      {
        manageController.AddErrors(result);
        return (IActionResult) manageController.View((object) model);
      }
      await manageController._signInManager.SignInAsync(user, false, (string) null);
      manageController.StatusMessage = "Your password has been set.";
      return (IActionResult) manageController.RedirectToAction(nameof (SetPassword));
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLogins()
    {
      ManageController manageController = this;
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      ExternalLoginsViewModel externalLoginsViewModel1 = new ExternalLoginsViewModel();
      ExternalLoginsViewModel externalLoginsViewModel2 = externalLoginsViewModel1;
      externalLoginsViewModel2.CurrentLogins = await manageController._userManager.GetLoginsAsync(user);
      ExternalLoginsViewModel model = externalLoginsViewModel1;
      externalLoginsViewModel2 = (ExternalLoginsViewModel) null;
      externalLoginsViewModel1 = (ExternalLoginsViewModel) null;
      externalLoginsViewModel1 = model;
      externalLoginsViewModel1.OtherLogins = (IList<AuthenticationScheme>) (await manageController._signInManager.GetExternalAuthenticationSchemesAsync()).Where<AuthenticationScheme>((Func<AuthenticationScheme, bool>) (auth => model.CurrentLogins.All<UserLoginInfo>((Func<UserLoginInfo, bool>) (ul => auth.Name != ul.LoginProvider)))).ToList<AuthenticationScheme>();
      externalLoginsViewModel1 = (ExternalLoginsViewModel) null;
      externalLoginsViewModel1 = model;
      externalLoginsViewModel1.ShowRemoveButton = await manageController._userManager.HasPasswordAsync(user) || model.CurrentLogins.Count > 1;
      externalLoginsViewModel1 = (ExternalLoginsViewModel) null;
      model.StatusMessage = manageController.StatusMessage;
      IActionResult actionResult = (IActionResult) manageController.View((object) model);
      user = (ApplicationUser) null;
      return actionResult;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkLogin(string provider)
    {
      ManageController manageController = this;
      await AuthenticationHttpContextExtensions.SignOutAsync(manageController.HttpContext, IdentityConstants.ExternalScheme);
      string redirectUrl = manageController.Url.Action("LinkLoginCallback");
      return (IActionResult) new ChallengeResult(provider, manageController._signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, manageController._userManager.GetUserId(manageController.User)));
    }

    [HttpGet]
    public async Task<IActionResult> LinkLoginCallback()
    {
      ManageController manageController = this;
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      ExternalLoginInfo externalLoginInfoAsync = await manageController._signInManager.GetExternalLoginInfoAsync(user.Id);
      if (externalLoginInfoAsync == null)
        throw new ApplicationException("Unexpected error occurred loading external login info for user with ID '" + user.Id + "'.");
      if (!(await manageController._userManager.AddLoginAsync(user, (UserLoginInfo) externalLoginInfoAsync)).Succeeded)
        throw new ApplicationException("Unexpected error occurred adding external login for user with ID '" + user.Id + "'.");
      await AuthenticationHttpContextExtensions.SignOutAsync(manageController.HttpContext, IdentityConstants.ExternalScheme);
      manageController.StatusMessage = "The external login was added.";
      IActionResult action = (IActionResult) manageController.RedirectToAction("ExternalLogins");
      user = (ApplicationUser) null;
      return action;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel model)
    {
      ManageController manageController = this;
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      if (!(await manageController._userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey)).Succeeded)
        throw new ApplicationException("Unexpected error occurred removing external login for user with ID '" + user.Id + "'.");
      await manageController._signInManager.SignInAsync(user, false, (string) null);
      manageController.StatusMessage = "The external login was removed.";
      IActionResult action = (IActionResult) manageController.RedirectToAction("ExternalLogins");
      user = (ApplicationUser) null;
      return action;
    }

    [HttpGet]
    public async Task<IActionResult> TwoFactorAuthentication()
    {
      ManageController manageController = this;
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      TwoFactorAuthenticationViewModel authenticationViewModel1 = new TwoFactorAuthenticationViewModel();
      TwoFactorAuthenticationViewModel authenticationViewModel2 = authenticationViewModel1;
      authenticationViewModel2.HasAuthenticator = await manageController._userManager.GetAuthenticatorKeyAsync(user) != null;
      authenticationViewModel1.Is2faEnabled = user.TwoFactorEnabled;
      TwoFactorAuthenticationViewModel authenticationViewModel3 = authenticationViewModel1;
      authenticationViewModel3.RecoveryCodesLeft = await manageController._userManager.CountRecoveryCodesAsync(user);
      TwoFactorAuthenticationViewModel model = authenticationViewModel1;
      authenticationViewModel2 = (TwoFactorAuthenticationViewModel) null;
      authenticationViewModel3 = (TwoFactorAuthenticationViewModel) null;
      authenticationViewModel1 = (TwoFactorAuthenticationViewModel) null;
      IActionResult actionResult = (IActionResult) manageController.View((object) model);
      user = (ApplicationUser) null;
      return actionResult;
    }

    [HttpGet]
    public async Task<IActionResult> Disable2faWarning()
    {
      ManageController manageController = this;
      ApplicationUser userAsync = await manageController._userManager.GetUserAsync(manageController.User);
      if (userAsync == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      if (!userAsync.TwoFactorEnabled)
        throw new ApplicationException("Unexpected error occured disabling 2FA for user with ID '" + userAsync.Id + "'.");
      return (IActionResult) manageController.View("Disable2fa");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable2fa()
    {
      ManageController manageController = this;
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      if (!(await manageController._userManager.SetTwoFactorEnabledAsync(user, false)).Succeeded)
        throw new ApplicationException("Unexpected error occured disabling 2FA for user with ID '" + user.Id + "'.");
      manageController._logger.LogInformation("User with ID {UserId} has disabled 2fa.", (object) user.Id);
      IActionResult action = (IActionResult) manageController.RedirectToAction("TwoFactorAuthentication");
      user = (ApplicationUser) null;
      return action;
    }

    [HttpGet]
    public async Task<IActionResult> EnableAuthenticator()
    {
      ManageController manageController = this;
      ApplicationUser userAsync = await manageController._userManager.GetUserAsync(manageController.User);
      if (userAsync == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      EnableAuthenticatorViewModel model = new EnableAuthenticatorViewModel();
      await manageController.LoadSharedKeyAndQrCodeUriAsync(userAsync, model);
      IActionResult actionResult = (IActionResult) manageController.View((object) model);
      model = (EnableAuthenticatorViewModel) null;
      return actionResult;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
    {
      ManageController manageController = this;
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      if (!manageController.ModelState.IsValid)
      {
        await manageController.LoadSharedKeyAndQrCodeUriAsync(user, model);
        return (IActionResult) manageController.View((object) model);
      }
      string str = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
      if (!await manageController._userManager.VerifyTwoFactorTokenAsync(user, manageController._userManager.Options.Tokens.AuthenticatorTokenProvider, str))
      {
        manageController.ModelState.AddModelError("Code", "Verification code is invalid.");
        await manageController.LoadSharedKeyAndQrCodeUriAsync(user, model);
        return (IActionResult) manageController.View((object) model);
      }
      IdentityResult identityResult = await manageController._userManager.SetTwoFactorEnabledAsync(user, true);
      manageController._logger.LogInformation("User with ID {UserId} has enabled 2FA with an authenticator app.", (object) user.Id);
      IEnumerable<string> recoveryCodesAsync = await manageController._userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
      manageController.TempData["RecoveryCodesKey"] = (object) recoveryCodesAsync.ToArray<string>();
      return (IActionResult) manageController.RedirectToAction("ShowRecoveryCodes");
    }

    [HttpGet]
    public IActionResult ShowRecoveryCodes()
    {
      string[] strArray = (string[]) this.TempData["RecoveryCodesKey"];
      if (strArray == null)
        return (IActionResult) this.RedirectToAction("TwoFactorAuthentication");
      return (IActionResult) this.View((object) new ShowRecoveryCodesViewModel()
      {
        RecoveryCodes = strArray
      });
    }

    [HttpGet]
    public IActionResult ResetAuthenticatorWarning() => (IActionResult) this.View("ResetAuthenticator");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAuthenticator()
    {
      ManageController manageController = this;
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      IdentityResult identityResult1 = await manageController._userManager.SetTwoFactorEnabledAsync(user, false);
      IdentityResult identityResult2 = await manageController._userManager.ResetAuthenticatorKeyAsync(user);
      manageController._logger.LogInformation("User with id '{UserId}' has reset their authentication app key.", (object) user.Id);
      IActionResult action = (IActionResult) manageController.RedirectToAction("EnableAuthenticator");
      user = (ApplicationUser) null;
      return action;
    }

    [HttpGet]
    public async Task<IActionResult> GenerateRecoveryCodesWarning()
    {
      ManageController manageController = this;
      ApplicationUser userAsync = await manageController._userManager.GetUserAsync(manageController.User);
      if (userAsync == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      if (!userAsync.TwoFactorEnabled)
        throw new ApplicationException("Cannot generate recovery codes for user with ID '" + userAsync.Id + "' because they do not have 2FA enabled.");
      return (IActionResult) manageController.View("GenerateRecoveryCodes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
      ManageController manageController = this;
      ApplicationUser user = await manageController._userManager.GetUserAsync(manageController.User);
      if (user == null)
        throw new ApplicationException("Unable to load user with ID '" + manageController._userManager.GetUserId(manageController.User) + "'.");
      if (!user.TwoFactorEnabled)
        throw new ApplicationException("Cannot generate recovery codes for user with ID '" + user.Id + "' as they do not have 2FA enabled.");
      IEnumerable<string> recoveryCodesAsync = await manageController._userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
      manageController._logger.LogInformation("User with ID {UserId} has generated new 2FA recovery codes.", (object) user.Id);
      ShowRecoveryCodesViewModel model = new ShowRecoveryCodesViewModel()
      {
        RecoveryCodes = recoveryCodesAsync.ToArray<string>()
      };
      IActionResult recoveryCodes = (IActionResult) manageController.View("ShowRecoveryCodes", (object) model);
      user = (ApplicationUser) null;
      return recoveryCodes;
    }

    private void AddErrors(IdentityResult result)
    {
      foreach (IdentityError error in result.Errors)
        this.ModelState.AddModelError(string.Empty, error.Description);
    }

    private string FormatKey(string unformattedKey)
    {
      StringBuilder stringBuilder = new StringBuilder();
      int startIndex;
      for (startIndex = 0; startIndex + 4 < unformattedKey.Length; startIndex += 4)
        stringBuilder.Append(unformattedKey.Substring(startIndex, 4)).Append(" ");
      if (startIndex < unformattedKey.Length)
        stringBuilder.Append(unformattedKey.Substring(startIndex));
      return stringBuilder.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey) => string.Format("otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6", (object) this._urlEncoder.Encode("Traffic"), (object) this._urlEncoder.Encode(email), (object) unformattedKey);

    private async Task LoadSharedKeyAndQrCodeUriAsync(
      ApplicationUser user,
      EnableAuthenticatorViewModel model)
    {
      string authenticatorKeyAsync = await this._userManager.GetAuthenticatorKeyAsync(user);
      if (string.IsNullOrEmpty(authenticatorKeyAsync))
      {
        IdentityResult identityResult = await this._userManager.ResetAuthenticatorKeyAsync(user);
        authenticatorKeyAsync = await this._userManager.GetAuthenticatorKeyAsync(user);
      }
      model.SharedKey = this.FormatKey(authenticatorKeyAsync);
      model.AuthenticatorUri = this.GenerateQrCodeUri(user.Email, authenticatorKeyAsync);
    }
  }
}
