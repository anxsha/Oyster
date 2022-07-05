using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Oyster.Areas.Identity.Data;
using Oyster.Data;
using Oyster.Pages.Groups.Shared;

namespace Oyster.Pages.Groups {
[Authorize]
public class MembersModel : PageModel, IGroupHeaderInfoPageModel {
  private readonly UserManager<OysterUser> _userManager;
  private readonly OysterContext _context;

  // Number of group members to be shown with avatars on the group header
  private readonly int _membersToBeLoaded = 10;

  public MembersModel(OysterContext context, UserManager<OysterUser> userManager) {
    _context = context;
    _userManager = userManager;
  }


  public bool IsUserGroupFounder { get; set; }

  // Currently browsed group
  public OysterGroup Group { get; set; }

  // Members with loaded avatars to be shown on the group header
  public List<GroupHeaderMemberViewModel> MembersToDisplay { get; set; }

  public async Task<IActionResult> OnGetAsync(int id) {
    // Currently logged-in user
    var currentUser = await _context.Users
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    Group = await _context.OysterGroups
      .Include(g => g.Founder)
      .Include(g => g.Members)
      .ThenInclude(u => u.Avatar)
      .Include(g => g.Photo)
      .FirstOrDefaultAsync(g => g.Id == id);

    // Check if the user and group exist and were fetched correctly
    if (Group is null || currentUser is null) {
      return NotFound();
    }

    // Assert the user is a group member, else info about group's existence
    // and pages is not publicly available – hence 404
    if (!Group.Members.Contains(currentUser)) {
      return NotFound();
    }

    // Populate the group header with sample members 
    MembersToDisplay = Group.Members
      .Take(_membersToBeLoaded)
      .Select(m => new GroupHeaderMemberViewModel() {
        DisplayName = m.DisplayName,
        Avatar = m.Avatar
      })
      .ToList();

    // Group founder can see the option to remove members from group
    if (Group.Founder == currentUser) {
      IsUserGroupFounder = true;
    }

    return Page();
  }

  // Post request on this endpoint handles group member removal for AJAX
  public async Task<IActionResult> OnPost(int id, [FromBody] JsonRequestModel model) {
    // Currently logged-in user
    var currentUser = await _context.Users.FindAsync(_userManager.GetUserId(User));

    Group = await _context.OysterGroups
      .Include(g => g.Members)
      .Include(g => g.Photo)
      .FirstOrDefaultAsync(g => g.Id == id);

    // Check if the user and group exist and were fetched correctly
    if (Group is null || currentUser is null) {
      return NotFound();
    }

    // Only the group founder may remove members
    if (Group.Founder != currentUser) {
      return NotFound();
    }
    IsUserGroupFounder = true;
    
    // Fetch the user to be removed
    var userToRemove = await _context.Users.FindAsync(model.UserId);
    // Group founder should not be visible for membership removal
    if (userToRemove is null || userToRemove == Group.Founder) {
      return NotFound();
    }

    if (Group.Members.Remove(userToRemove)) {
      try {
        await _context.SaveChangesAsync();
        return new JsonResult(new {userWasRemoved = true, username = userToRemove.PublicUsername});
      } catch (DbUpdateException) {
        return new JsonResult(new {userWasRemoved = false});
      }
    } else {
      return new JsonResult(new {userWasRemoved = false});
    }
  }

  // AJAX data passed in request body as JSON
  public class JsonRequestModel {
    public string UserId { get; set; }
  }
}
}