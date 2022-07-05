using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oyster.Areas.Identity.Data;
using Oyster.Data;
using Oyster.HelperClasses;

namespace Oyster.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<OysterUser> _userManager;
        private readonly SignInManager<OysterUser> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;
        private readonly OysterContext _context;


        public DeletePersonalDataModel(
            OysterContext context,
            UserManager<OysterUser> userManager,
            SignInManager<OysterUser> signInManager,
            ILogger<DeletePersonalDataModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Incorrect password.");
                    return Page();
                }
            }

            // Remove user information, but not the entity with Id
            // so as not to break null constraints with posts, comments
            // (which stay in groups but display an anonymous user).
            // All pages can treat the "lack of a user" (deletion)
            // with no difference compared to active ones.
            
            var userId = await _userManager.GetUserIdAsync(user);
            var userToDelete = await _context.Users
                .Include(u => u.Groups)
                .Include(u => u.GroupsCreated)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (userToDelete is null) {
                return NotFound();
            }
            
            // Get a unique integer value from a database sequence to generate username
            // Create a sql parameter to save the query result
            var parameter = new SqlParameter("@result", System.Data.SqlDbType.Int) {
                Direction = System.Data.ParameterDirection.Output
            };
            // Fetch value from the database
            await _context.Database.ExecuteSqlRawAsync("set @result = next value for dbo.UniqueUserNumber", parameter);
            var uniqueNumber = (int) parameter.Value;
            
            userToDelete.Email = " ";
            userToDelete.NormalizedEmail = " ";
            userToDelete.UserName = null;
            userToDelete.NormalizedUserName = null;
            userToDelete.PasswordHash = null;
            userToDelete.Avatar = new Photo {Url = @DefaultDataProvider.GetDeactivatedUserAvatar()};
            userToDelete.Groups.Clear();
            // userToDelete.Votes Remains the same - votes do not disappear
            userToDelete.GroupsCreated.Clear();
            userToDelete.DisplayName = "Oyster User";
            userToDelete.PublicUsername = $"oysteruser+{uniqueNumber}";
            userToDelete.UserTimeZoneId = TimeZoneInfo.Utc.Id;
            userToDelete.EmailConfirmed = false;
            userToDelete.LockoutEnabled = true;
            userToDelete.IsUserDeactivated = true;

            try {
                await _context.SaveChangesAsync();
            } catch (DbUpdateException) {
                throw new InvalidOperationException($"Unexpected error occurred deleting user with ID '{userId}'.");
            }

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

            return Redirect("~/");
        }
    }
}
