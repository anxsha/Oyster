using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Oyster.Areas.Identity.Data;
using Oyster.Data;

namespace Oyster.Pages;
[Authorize]
public class UserPageModel : PageModel {
  private readonly UserManager<OysterUser> _userManager;
  private readonly OysterContext _context;

  public UserPageModel(OysterContext context, UserManager<OysterUser> userManager) {
    _context = context;
    _userManager = userManager;
  }
  // The user whose profile info the client wants to be displayed
  public OysterUser PageUser { get; set; }

  // A set of groups that are shared by PageUser and the currently logged-in user
  public List<OysterGroup> GroupsInCommon { get; set; } = new();

  public async Task<IActionResult> OnGetAsync(string username) {
    // Currently logged-in user
    var currentUser = await _context.Users
      .Include(u => u.Groups)
      .ThenInclude(g => g.Photo)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
    // Request did not include PageUser's username or fetching the current user failed
    if (username is null || currentUser is null) {
      return NotFound();
    }

    PageUser = await _context.Users
      .Include(u => u.Groups)
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.PublicUsername == username);
    if (PageUser is null) {
      return NotFound();
    }
    // When e.g. the client clicked their own avatar, redirect to profile settings
    if (PageUser == currentUser) {
      return RedirectToPage("/Account/Manage/Index", new {area = "Identity"});
    }

    GroupsInCommon = currentUser.Groups.Intersect(PageUser.Groups).ToList();

    return Page();
  }
}