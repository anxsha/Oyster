using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oyster.Areas.Identity.Data;
using Oyster.Data;

namespace Oyster.Areas.Identity.Pages.Account {
[AllowAnonymous]
public class RegisterModel : PageModel {
  private readonly SignInManager<OysterUser> _signInManager;
  private readonly UserManager<OysterUser> _userManager;
  private readonly ILogger<RegisterModel> _logger;
  private readonly IEmailSender _emailSender;
  private readonly OysterContext _context;


  public RegisterModel(
    UserManager<OysterUser> userManager,
    SignInManager<OysterUser> signInManager,
    ILogger<RegisterModel> logger,
    IEmailSender emailSender,
    OysterContext context) {
    _userManager = userManager;
    _signInManager = signInManager;
    _logger = logger;
    _emailSender = emailSender;
    _context = context;
  }

  [BindProperty] public InputModel Input { get; set; }

  public string ReturnUrl { get; set; }

  public IList<AuthenticationScheme> ExternalLogins { get; set; }

  public class InputModel {
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [Display(Name = "Display name")]
    [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
    public string DisplayName { get; set; }

    [Required]
    [Display(Name = "Choose your timezone")]
    public string UserTimeZoneId { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
      MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }
  }

  public async Task OnGetAsync(string returnUrl = null) {
    ReturnUrl = returnUrl;
    ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
  }

  public async Task<IActionResult> OnPostAsync(string returnUrl = null) {
    returnUrl ??= Url.Content("~/");
    ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    if (ModelState.IsValid) {
      
      // Get a unique integer value from a database sequence to generate
      // user's first public username based on his display name
      
      // Create a sql parameter to save the query result
      var parameter = new SqlParameter("@result", System.Data.SqlDbType.Int) {
        Direction = System.Data.ParameterDirection.Output
      };
      // Fetch value from the database
      await _context.Database.ExecuteSqlRawAsync("set @result = next value for dbo.UniqueUserNumber", parameter);
      var uniqueNumber = (int) parameter.Value;
      // Remove white-space from the display name for creating username
      var trimmedDisplayName = string.Concat(Input.DisplayName.Where(c => !char.IsWhiteSpace(c)));

      // Construct username
      var firstUsername = $"{trimmedDisplayName.ToLower()}+{uniqueNumber}";

      // Remove consecutive white-space from user's entered display name
      var displayName = string.Join(" ", Input.DisplayName.Split(new char[] { ' ' },
        StringSplitOptions.RemoveEmptyEntries));
      
      var user = new OysterUser {
        UserName = Input.Email, Email = Input.Email,
        DisplayName = displayName, UserTimeZoneId = Input.UserTimeZoneId, PublicUsername = firstUsername
      };
      var result = await _userManager.CreateAsync(user, Input.Password);
      if (result.Succeeded) {
        // _logger.LogInformation("User created a new account with password.");

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page(
          "/Account/ConfirmEmail",
          pageHandler: null,
          values: new {area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl},
          protocol: Request.Scheme);

        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
          $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

        if (_userManager.Options.SignIn.RequireConfirmedAccount) {
          return RedirectToPage("RegisterConfirmation", new {email = Input.Email, returnUrl = returnUrl});
        } else {
          await _signInManager.SignInAsync(user, isPersistent: false);
          return LocalRedirect(returnUrl);
        }
      }

      foreach (var error in result.Errors) {
        if (error.Code != "DuplicateUserName") {
          ModelState.AddModelError(string.Empty, error.Description);
        }
      }
    }

    // If we got this far, something failed, redisplay form
    return Page();
  }
}
}