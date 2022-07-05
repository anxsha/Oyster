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

namespace Oyster.Pages.Groups.Posts {
[Authorize]
public class IndexModel : PageModel, IGroupHeaderInfoPageModel {
  private readonly UserManager<OysterUser> _userManager;
  private readonly OysterContext _context;

  public IndexModel(OysterContext context, UserManager<OysterUser> userManager) {
    _context = context;
    _userManager = userManager;
  }

  public bool IsUserGroupFounder { get; set; }

  // Number of group members to be shown with avatars on the group header
  private readonly int _membersToBeLoaded = 10;

  // User's timezone used for displaying proper posts' times
  public TimeZoneInfo UserTimeZone;

  // Currently browsed group
  public OysterGroup Group { get; set; }

  // Members with loaded avatars to be shown on the group header
  public List<GroupHeaderMemberViewModel> MembersToDisplay { get; set; }

  public PostViewModel Post { get; set; }

  public OysterUser CurrentUser { get; set; }

  // Poll option chosen by the user to be voted on
  [BindProperty] public InputModel Input { get; set; }
  
  // To identify when there is a need to show an alert
  public bool IsServerError;

  public async Task<IActionResult> OnGetAsync(int groupId, int postId) {
    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    if (CurrentUser is null) {
      return NotFound();
    }

    var userTimeZoneId = CurrentUser.UserTimeZoneId;

    // Convert the user's timezone id to the correct timezone
    // If the id was not obtained, use UTC
    UserTimeZone = userTimeZoneId != null ? TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId) : TimeZoneInfo.Utc;

    Group = await _context.OysterGroups
      .Include(g => g.Members)
      .Include(g => g.Photo)
      .Include(g => g.Founder)
      .ThenInclude(f => f.Avatar)
      .FirstOrDefaultAsync(g => g.Id == groupId);

    if (Group is null) {
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

    Post = await _context.Posts
      .Include(p => p.Group)
      .Include(p => p.Poll.Options)
      .Include(p => p.Author)
      .ThenInclude(a => a.Avatar)
      .Select(p => new PostViewModel() {
        Id = p.Id,
        Content = p.Content,
        CreatedAt = p.CreatedAt,
        Group = p.Group,
        Author = p.Author,
        CommentsCount = p.Comments.Count,
        Poll = p.Poll != null
          ? new PollViewModel {
            Id = p.Poll.Id, Title = p.Poll.Title,
            OptionVotedByUser = p.Poll.Options.FirstOrDefault(o => o.Voters.Contains(CurrentUser)),
            Options = p.Poll.Options.Select(o => new PollOptionViewModel {
              Id = o.Id,
              Text = o.Text,
              VotersCount = o.Voters.Count
            }).ToList()
          }
          : null,
      })
      .FirstOrDefaultAsync(p => p.Id == postId);

    // Verify all the information is correct about the group to which the post belongs and 
    // the user is a member
    if (Post is null || Post.Group != Group) {
      return NotFound();
    }

    if (!Group.Members.Contains(CurrentUser)) {
      return NotFound();
    }

    if (Group.Founder == CurrentUser) {
      IsUserGroupFounder = true;
    }

    return Page();
  }

  // Poll voting
  public async Task<IActionResult> OnPostAsync(int groupId, int postId) {
    if (!ModelState.IsValid) {
      return Page();
    }

    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    if (CurrentUser is null) {
      return NotFound();
    }

    var userTimeZoneId = CurrentUser.UserTimeZoneId;

    // Convert the user's timezone id to the correct timezone
    // If the id was not obtained, use UTC
    UserTimeZone = userTimeZoneId != null ? TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId) : TimeZoneInfo.Utc;

    Group = await _context.OysterGroups
      .Include(g => g.Members)
      .Include(g => g.Photo)
      .Include(g => g.Founder)
      .ThenInclude(f => f.Avatar)
      .FirstOrDefaultAsync(g => g.Id == groupId);

    if (Group is null) {
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

    Post = await _context.Posts
      .Include(p => p.Group)
      .Include(p => p.Poll.Options)
      .Select(p => new PostViewModel() {
        Id = p.Id,
        Content = p.Content,
        CreatedAt = p.CreatedAt,
        Group = p.Group,
        Author = p.Author,
        CommentsCount = p.Comments.Count,
        Poll = p.Poll != null
          ? new PollViewModel {
            Id = p.Poll.Id, Title = p.Poll.Title,
            OptionVotedByUser = p.Poll.Options.FirstOrDefault(o => o.Voters.Contains(CurrentUser)),
            Options = p.Poll.Options.Select(o => new PollOptionViewModel {
              Id = o.Id,
              Text = o.Text,
              VotersCount = o.Voters.Count
            }).ToList()
          }
          : null,
      })
      .FirstOrDefaultAsync(p => p.Id == postId);

    if (Post is null || Post.Group != Group) {
      return NotFound();
    }

    if (!Group.Members.Contains(CurrentUser)) {
      return NotFound();
    }

    if (Group.Founder == CurrentUser) {
      IsUserGroupFounder = true;
    }

    // Fetch the poll associated with this post
    var currentPoll = await _context.Polls
      .Where(p => p.Id == Post.Poll.Id)
      .Include(p => p.Options)
      .ThenInclude(o => o.Voters)
      .FirstOrDefaultAsync();
    
    // Fetch the option with id as sent in the request
    var chosenPollOption = currentPoll?.Options
      .FirstOrDefault(o => o.Id == Input.PollOptionId);
    
    if (chosenPollOption?.Voters is not null && Post.Poll.Options.Any(o => o.Id == chosenPollOption.Id)) {
      // If user has already voted, replace the vote
      var previouslyVotedOption = currentPoll.Options
        .FirstOrDefault(o => o.Voters.Contains(CurrentUser));
      previouslyVotedOption?.Voters.Remove(CurrentUser);
      chosenPollOption.Voters.Add(CurrentUser);
      try {
        await _context.SaveChangesAsync();
        // If voting was successfull, update the front-end view model poll options
        Post.Poll.OptionVotedByUser = chosenPollOption;
        Post.Poll.Options.FirstOrDefault(o => o.Id == chosenPollOption.Id)!.VotersCount = chosenPollOption.Voters.Count;
        if (previouslyVotedOption is not null) {
          Post.Poll.Options.FirstOrDefault(o => o.Id == previouslyVotedOption.Id)!.VotersCount = previouslyVotedOption.Voters.Count;
        }
      } catch (DbUpdateException) {
        IsServerError = true;
        return Page();
      }
    }


    return Page();
  }

  // Post model containing only the count of comments
  public class PostViewModel {
    [Key] public int Id { get; set; }
    [Required] [StringLength(5000)] public string Content { get; set; }
    [Required] public DateTimeOffset CreatedAt { get; set; }
    public OysterGroup Group { get; set; }
    public OysterUser Author { get; set; }
    public int CommentsCount { get; set; }
    public PollViewModel Poll { get; set; }
  }

  public class PollViewModel {
    [Key] public int Id { get; set; }
    [StringLength(1000)] public string Title { get; set; }
    public ICollection<PollOptionViewModel> Options { get; set; }
    public PollOption OptionVotedByUser { get; set; }
  }

  public class PollOptionViewModel {
    [Key] public int Id { get; set; }
    [Required] [StringLength(1000)] public string Text { get; set; }
    public int VotersCount { get; set; }
  }

  // User submits a vote
  public class InputModel {
    public int PollOptionId { get; set; }
  }
}
}