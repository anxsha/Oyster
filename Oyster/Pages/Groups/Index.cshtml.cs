using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Oyster.Areas.Identity.Data;
using Oyster.Data;

namespace Oyster.Pages.Groups {
[Authorize]
public class IndexModel : PageModel {
  private readonly UserManager<OysterUser> _userManager;

  private readonly OysterContext _context;

  // Number of groups to be shown by default
  private readonly int _groupsToBeLoaded = 5;

  // Set to true if user clicked the button to show all
  public bool ViewAllGroups;

  public IndexModel(OysterContext context, UserManager<OysterUser> userManager) {
    _context = context;
    _userManager = userManager;
  }

  public IEnumerable<OysterGroup> Groups { get; private set; }

  public async Task OnGetAsync(bool? viewAllGroups) {
    // Currently logged-in user
    OysterUser user;
    if (viewAllGroups is true) {
      ViewAllGroups = true;
      // Load the user with all groups
      user = await _context.Users
        .AsNoTracking()
        .Include(u => u.Groups.OrderBy(g => g.Name))
        .ThenInclude(g => g.Photo)
        .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
    } else {
      // By default, take a limited number of groups for display
      user = await _context.Users
        .AsNoTracking()
        .Include(u => u.Groups.OrderBy(g => g.Name)
          .Take(_groupsToBeLoaded))
        .ThenInclude(g => g.Photo)
        .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
    }

    Groups = user?.Groups;
  }
}
}