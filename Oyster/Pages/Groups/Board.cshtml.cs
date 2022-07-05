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
using Oyster.HelperClasses;
using Oyster.HelperClasses.Pagination;
using Oyster.Pages.Groups.Shared;

namespace Oyster.Pages.Groups {
[Authorize]
public class BoardModel : PageModel, IGroupHeaderInfoPageModel {
  private readonly UserManager<OysterUser> _userManager;
  private readonly OysterContext _context;

  public BoardModel(OysterContext context, UserManager<OysterUser> userManager) {
    _context = context;
    _userManager = userManager;
  }
  // To identify when there is a need to show an alert
  public bool IsServerError;

  // Initial number of posts to be shown on the group page
  public readonly int PostsToBeLoadedCount = 10;

  // Number of group members to be shown with avatars on the group header
  private readonly int _membersToBeLoaded = 10;

  public bool IsUserGroupFounder { get; set; }

  // User's timezone used for displaying proper posts' times
  public TimeZoneInfo UserTimeZone;

  // Currently browsed group
  public OysterGroup Group { get; set; }

  // Loaded posts
  public PaginatedList<PostViewModel> Posts;

  // Members with loaded avatars to be shown on the group header
  public List<GroupHeaderMemberViewModel> MembersToDisplay { get; set; }

  public OysterUser CurrentUser { get; set; }

  [BindProperty] public InputModel Input { get; set; }

  public async Task<IActionResult> OnGetAsync(int id) {
    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    var userTimeZoneId = CurrentUser?.UserTimeZoneId;

    // Convert the user's timezone id to the correct timezone
    // If the id was not obtained, use UTC
    UserTimeZone = userTimeZoneId != null ? TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId) : TimeZoneInfo.Utc;

    Group = await _context.OysterGroups
      .Include(g => g.Members)
      .Include(g => g.Photo)
      .Include(g => g.Founder)
      .ThenInclude(f => f.Avatar)
      .FirstOrDefaultAsync(g => g.Id == id);

    if (Group is null || CurrentUser is null) {
      return NotFound();
    }

    if (!Group.Members.Contains(CurrentUser)) {
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

    // Prepare query to fetch the most recent posts in group
    var postsQuery = _context.Posts
      .Where(p => p.Group == Group)
      .OrderByDescending(p => p.CreatedAt)
      .Include(p => p.Author.Avatar)
      .Select(p => new PostViewModel() {
        Id = p.Id,
        Content = p.Content,
        CreatedAt = p.CreatedAt,
        Author = p.Author,
        CommentsCount = p.Comments.Count,
        IsPollIncluded = p.Poll != null
      });

    // Get the first page of recent posts
    Posts = await PaginatedList<PostViewModel>.CreateAsync(postsQuery
        .AsNoTracking()
      , 1, PostsToBeLoadedCount);

    if (Group.Founder == CurrentUser) {
      IsUserGroupFounder = true;
    }

    return Page();
  }

  // Creating a new post on submit
  public async Task<IActionResult> OnPostAsync(int id) {
    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    var userTimeZoneId = CurrentUser?.UserTimeZoneId;

    // Convert the user's timezone id to the correct timezone
    // If the id was not obtained, use UTC
    UserTimeZone = userTimeZoneId != null ? TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId) : TimeZoneInfo.Utc;

    Group = await _context.OysterGroups
      .Include(g => g.Members)
      .Include(g => g.Photo)
      .Include(g => g.Founder)
      .ThenInclude(f => f.Avatar)
      .FirstOrDefaultAsync(g => g.Id == id);

    if (Group is null || CurrentUser is null) {
      return NotFound();
    }

    if (!Group.Members.Contains(CurrentUser)) {
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

    // Prepare query to fetch the most recent posts in group
    var postsQuery = _context.Posts
      .Where(p => p.Group == Group)
      .OrderByDescending(p => p.CreatedAt)
      .Include(p => p.Author.Avatar)
      .Select(p => new PostViewModel() {
        Id = p.Id,
        Content = p.Content,
        CreatedAt = p.CreatedAt,
        Author = p.Author,
        CommentsCount = p.Comments.Count,
        IsPollIncluded = p.Poll != null
      });

    // Get the first page of recent posts
    Posts = await PaginatedList<PostViewModel>.CreateAsync(postsQuery
        .AsNoTracking()
      , 1, PostsToBeLoadedCount);

    if (Group.Founder == CurrentUser) {
      IsUserGroupFounder = true;
    }

    if (!ModelState.IsValid) {
      return Page();
    }
    // New post from user input
    var newPost = new Post {
      Content = Input.Content,
      Author = CurrentUser,
      // Store in database in UTC
      CreatedAt = DateTimeOffset.Now.ToUniversalTime(),
      LastChange = DateTimeOffset.Now.ToUniversalTime(),
      Group = Group
    };
    // If user enabled the option and included a voting poll
    if (Input.PollIsIncluded) {
      var newPoll = new Poll {
        Title = Input.PollTitle,
        Options = Input.PollOptions.Select(o => new PollOption {Text = o}).ToList(),
      };
      newPost.Poll = newPoll;
    }

    _context.Posts.Add(newPost);

    try {
      await _context.SaveChangesAsync();
    } catch (DbUpdateException) {
      IsServerError = true;
      return Page();
    }

    return RedirectToPage("/Groups/Board", new {id = Group.Id});
  }

  // Endpoint for AJAX requests trying to fetch older posts
  public async Task<IActionResult> OnGetMorePostsAsync(int id, int postsPageSize, int pageIndex) {
    var currentUser = await _context.Users
      .AsNoTracking()
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    var userTimeZoneId = currentUser?.UserTimeZoneId;

    // Convert the user's timezone id to the correct timezone
    // If the id was not obtained, use UTC
    var userTimeZone = userTimeZoneId != null ? TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId) : TimeZoneInfo.Utc;

    var group = await _context.OysterGroups
      .AsNoTracking()
      .Include(g => g.Members)
      .FirstOrDefaultAsync(g => g.Id == id);

    // Check if the user and group exist and were fetched correctly
    // As well as the user is a group member
    if (group is null || currentUser is null || !group.Members.Any(m => m.Id == currentUser.Id)) {
      return NotFound();
    }

    // Prepare query for fetching older posts with an appropriate model projection
    var postsQuery = _context.Posts
      .Where(p => p.Group == group)
      .OrderByDescending(p => p.CreatedAt)
      .Select(p => new PostViewModelForAjax() {
        Id = p.Id,
        Content = p.Content,
        CreatedAt = p.CreatedAt.DateTime,
        AuthorId = p.Author.Id,
        AuthorDisplayName = p.Author.DisplayName,
        AuthorUsername = p.Author.PublicUsername,
        AuthorAvatar = p.Author.Avatar,
        CommentsCount = p.Comments.Count,
        IsPollIncluded = p.Poll != null
      });

    // Skip posts based on provided page index
    var posts = await PaginatedList<PostViewModelForAjax>.CreateAsync(postsQuery
        .AsNoTracking()
      , pageIndex, postsPageSize);

    // Return these posts with time converted to user's timezone
    foreach (var post in posts) {
      post.CreatedAtFormatted = DateFormatter.GetDefaultPostFormat(post.CreatedAt, userTimeZone);
      post.CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(post.CreatedAt, userTimeZone);
    }

    return new JsonResult(new {posts, hasNextPage = posts.HasNextPage, pageIndex = posts.PageIndex});
  }

  public async Task<IActionResult> OnPostLeaveGroupAsync(int id) {
    var currentUser = await _context.Users.FindAsync(_userManager.GetUserId(User));

    var group = await _context.OysterGroups
      .Include(g => g.Members)
      .FirstOrDefaultAsync(g => g.Id == id);

    // Check if user is a group member, group founder cannot leave its group
    if (group is null || !group.Members.Contains(currentUser) || group.Founder == currentUser) {
      return NotFound();
    }

    group.Members.Remove(currentUser);

    try {
      await _context.SaveChangesAsync();
    } catch (DbUpdateException) {
      IsServerError = true;
      return Page();
    }

    return RedirectToPage("./Index");
  }

  // Post model containing only the count of comments
  public class PostViewModel {
    [Key] public int Id { get; set; }
    [Required] [StringLength(5000)] public string Content { get; set; }
    [Required] public DateTimeOffset CreatedAt { get; set; }
    public OysterUser Author { get; set; }
    public int CommentsCount { get; set; }
    public bool IsPollIncluded { get; set; }
  }

  // Post model for AJAX requests to return
  public class PostViewModelForAjax {
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedAtFormatted { get; set; }
    public string AuthorId { get; set; }
    public string AuthorDisplayName { get; set; }
    public string AuthorUsername { get; set; }
    public Photo AuthorAvatar { get; set; }
    public int CommentsCount { get; set; }
    public bool IsPollIncluded { get; set; }
  }

  // Model for adding a new post
  public class InputModel {
    [Required] [StringLength(5000)] public string Content { get; set; }
    [StringLength(1000)] public string PollTitle { get; set; }
    public List<string> PollOptions { get; set; }
    public bool PollIsIncluded { get; set; }
  }
}
}