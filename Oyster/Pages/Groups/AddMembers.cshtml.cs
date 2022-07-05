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
using Oyster.HelperClasses.Pagination;
using Oyster.Pages.Groups.Shared;

namespace Oyster.Pages.Groups {
[Authorize]
public class AddMembersModel : PageModel, IGroupHeaderInfoPageModel {
  private readonly UserManager<OysterUser> _userManager;
  private readonly OysterContext _context;

  // Maximum number of filtered users shown on the page (for paginated list)
  private readonly int _usersFoundPageSize = 10;

  // Number of group members to be shown with avatars on the group header
  private readonly int _membersToBeLoaded = 10;

  public AddMembersModel(OysterContext context, UserManager<OysterUser> userManager) {
    _context = context;
    _userManager = userManager;
  }

  // To identify when there is a need to show an alert
  public bool IsServerError;

  // Set to true if a user was successfully added to the group
  public bool IsUserAdded;

  public bool IsUserGroupFounder { get; set; }

  // Currently browsed group
  public OysterGroup Group { get; set; }

  // Members with loaded avatars to be shown on the group header
  public List<GroupHeaderMemberViewModel> MembersToDisplay { get; set; }

  // Stores a maximum of usersFoundPageSize users that were filtered with the current search input
  public PaginatedList<OysterUser> UsersFound { get; set; } = new();

  // Search input entered by the user to filter users and saved between requests
  // in prev/next links to allow page navigation
  public string CurrentFilter { get; set; }


  public async Task<IActionResult> OnGetAsync(int id, string searchInput, string currentFilter, int? pageIndex) {
    var currentUser = await _context.Users.FindAsync(_userManager.GetUserId(User));

    Group = await _context.OysterGroups
      .Include(g => g.Members)
      .Include(g => g.Photo)
      .FirstOrDefaultAsync(g => g.Id == id);

    if (Group is null || currentUser is null) {
      return NotFound();
    }

    // Populate the group header with sample members 
    MembersToDisplay = await _context.Entry(Group)
      .Collection(g => g.Members)
      .Query()
      .Take(_membersToBeLoaded)
      .Include(m => m.Avatar)
      .Select(m => new GroupHeaderMemberViewModel() {
        DisplayName = m.DisplayName,
        Avatar = m.Avatar
      })
      .ToListAsync();

    if (Group.Founder == currentUser) {
      IsUserGroupFounder = true;
      // A new (different) search input was sent by the user
      if (searchInput is not null) {
        // Thus, paginated list starts with the first page
        pageIndex = 1;
      } else {
        // User navigated to the previous or next page (the filter remains the same)
        searchInput = currentFilter;
      }

      // Verify bad requests
      if (pageIndex < 1) {
        pageIndex = 1;
      }

      // Remove leading white-space
      searchInput = searchInput?.TrimStart();

      // save the current filter
      CurrentFilter = searchInput;

      if (!string.IsNullOrWhiteSpace(searchInput)) {
        // Prepare the users query as a data source
        var usersQuery = _context.Users
          // Users' display names and usernames are checked for matches with search input
          .Where(u =>
            (u.DisplayName.Contains(searchInput) ||
             u.PublicUsername.Contains(searchInput)) && !u.IsUserDeactivated
          )
          .OrderBy(u => u.DisplayName);
        // Pass the IQueryable as the source of data to be fetched
        // to the paginated list
        UsersFound =
          await PaginatedList<OysterUser>.CreateAsync(usersQuery
              .AsNoTracking()
              .Include(u => u.Groups)
              .Include(u => u.Avatar)
            , pageIndex ?? 1, _usersFoundPageSize);
      }

      // If the requested index was too large, reset the query (go to page 1)
      // However, assert that the query returns at least one entry (a page 1 exists)
      // to avoid infinite redirects
      if (pageIndex > UsersFound.TotalPages && UsersFound.TotalPages != 0) {
        return RedirectToPage("AddMembers", new {id = Group.Id, searchInput = searchInput});
      }

      return Page();
    }

    return NotFound();
  }

// Adding a new group member
  public async Task<IActionResult> OnPost(int id, string userId) {
    var currentUser = await _context.Users.FindAsync(_userManager.GetUserId(User));

    Group = await _context.OysterGroups
      .Include(g => g.Members)
      .Include(g => g.Photo)
      .FirstOrDefaultAsync(g => g.Id == id);

    if (Group is null || currentUser is null) {
      return NotFound();
    }

    if (Group.Founder == currentUser) {
      IsUserGroupFounder = true;
      var userToAdd = await _context.Users.FindAsync(userId);
      // Assure not adding a non-active user
      if (userToAdd is {IsUserDeactivated: false}) {
        Group.Members.Add(userToAdd);
      }

      try {
        await _context.SaveChangesAsync();
        IsUserAdded = true;
      } catch (DbUpdateException) {
        IsServerError = true;
      }

      // Populate the group header with sample members 
      MembersToDisplay = await _context.Entry(Group)
        .Collection(g => g.Members)
        .Query()
        .Take(_membersToBeLoaded)
        .Include(m => m.Avatar)
        .Select(m => new GroupHeaderMemberViewModel() {
          DisplayName = m.DisplayName,
          Avatar = m.Avatar
        })
        .ToListAsync();

      return Page();
    }

    return NotFound();
  }
}
}