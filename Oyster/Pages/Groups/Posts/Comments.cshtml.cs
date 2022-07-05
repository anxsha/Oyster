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

namespace Oyster.Pages.Groups.Posts {
[Authorize]
public class CommentsModel : PageModel {
  private readonly UserManager<OysterUser> _userManager;
  private readonly OysterContext _context;

  public CommentsModel(OysterContext context, UserManager<OysterUser> userManager) {
    _context = context;
    _userManager = userManager;
  }

  // Fetching a paginated list of comments
  public async Task<IActionResult> OnGetAsync(int groupId, int postId, int commentsPageSize, int? pageIndex) {
    var currentUser = await _context.Users.FindAsync(_userManager.GetUserId(User));

    var userTimeZoneId = currentUser?.UserTimeZoneId;

    // Convert the user's timezone id to the correct timezone
    // If the id was not obtained, use UTC
    var userTimeZone = userTimeZoneId != null ? TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId) : TimeZoneInfo.Utc;

    var group = await _context.OysterGroups
      .Include(g => g.Members)
      .FirstOrDefaultAsync(g => g.Id == groupId);

    if (group is null || !group.Members.Contains(currentUser)) {
      return NotFound();
    }
    
    // Verify the posts exists and belongs to the current group
    var post = await _context.Posts
      .Include(p => p.Group)
      .FirstOrDefaultAsync(p => p.Id == postId);

    if (post is null || post.Group != group) {
      return NotFound();
    }

    // Prepare query for comments as a data source for paginated list
    IQueryable<CommentViewModel> commentsQuery = _context.Comments
      .Where(c => c.Post == post)
      .Include(c => c.Author)
      .ThenInclude(a => a.Avatar)
      .OrderByDescending(c => c.CreatedAt)
      .Select(c => new CommentViewModel() {
        Id = c.Id,
        Content = c.Content,
        CreatedAt = c.CreatedAt.DateTime,
        AuthorId = c.Author.Id,
        AuthorDisplayName = c.Author.DisplayName,
        AuthorUsername = c.Author.PublicUsername,
        AuthorAvatar = c.Author.Avatar
      });

    var comments = await PaginatedList<CommentViewModel>.CreateAsync(commentsQuery.AsNoTracking(),
      pageIndex ?? 1, commentsPageSize);

    foreach (var comment in comments) {
      // Return comments with timezone converted to user's preferred from UTC
      comment.CreatedAtFormatted = DateFormatter.GetCommentFormat(comment.CreatedAt, userTimeZone);
      comment.CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(comment.CreatedAt, userTimeZone);
    }
    // Send to the client time of fetching comments, so that
    // they can use polling for updates (new comments)
    var lastUpdateCheckUtc = DateTimeOffset.UtcNow;

    return new JsonResult(new {comments, hasNextPage = comments.HasNextPage, pageIndex = comments.PageIndex, lastUpdateCheckUtc});
  }

  // Client uses polling to check for updates (new comments)
  public async Task<IActionResult>
    OnGetCheckForNewCommentsAsync(int groupId, int postId, DateTime lastClientChangeUtc) {
    var currentUser = await _context.Users.FindAsync(_userManager.GetUserId(User));

    var userTimeZoneId = currentUser?.UserTimeZoneId;

    // Convert the user's timezone id to the correct timezone
    // If the id was not obtained, use UTC
    var userTimeZone = userTimeZoneId != null ? TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId) : TimeZoneInfo.Utc;

    var group = await _context.OysterGroups
      .Include(g => g.Members)
      .FirstOrDefaultAsync(g => g.Id == groupId);

    if (group is null || !group.Members.Contains(currentUser)) {
      return NotFound();
    }

    // Verify the posts exists and belongs to the current group
    var post = await _context.Posts
      .Include(p => p.Group)
      .FirstOrDefaultAsync(p => p.Id == postId);

    if (post is null || post.Group != group) {
      return NotFound();
    }

    // Check if any new comments have appeared since the client last checked
    var postHasChanged = post.LastChange.DateTime > lastClientChangeUtc;
    
    // Send to the client time of fetching comments, so that
    // they can use polling for updates (new comments)
    var lastUpdateCheckUtc = DateTimeOffset.UtcNow;

    // New comments have appeared, send them to the client
    if (postHasChanged) {
      var newComments = await _context.Comments
        .OrderByDescending(c => c.CreatedAt)
        .Where(c => c.Post.Id == postId && c.CreatedAt > lastClientChangeUtc)
        .Select(c => new CommentViewModel {
          Id = c.Id,
          Content = c.Content,
          CreatedAt = c.CreatedAt.DateTime,
          AuthorId = c.Author.Id,
          AuthorDisplayName = c.Author.DisplayName,
          AuthorUsername = c.Author.PublicUsername,
          AuthorAvatar = c.Author.Avatar
        })
        .ToListAsync();
      foreach (var comment in newComments) {
        // Return comments with timezone converted to user's preferred from UTC
        comment.CreatedAtFormatted = DateFormatter.GetCommentFormat(comment.CreatedAt, userTimeZone);
        comment.CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(comment.CreatedAt, userTimeZone);
      }

      return new JsonResult(new {postHasChanged = true, newComments, lastUpdateCheckUtc});
    }

    return new JsonResult(new {postHasChanged = false, lastUpdateCheckUtc});
  }

  // Creating a new comment
  public async Task<IActionResult> OnPostAsync(int groupId, int postId, DateTime lastClientChangeUtc,
    [FromBody] NewCommentModel comment) {
    var currentUser = await _context.Users.FindAsync(_userManager.GetUserId(User));

    if (currentUser is null) {
      return NotFound();
    }

    var userTimeZoneId = currentUser.UserTimeZoneId;

    // Convert the user's timezone id to the correct timezone
    // If the id was not obtained, use UTC
    var userTimeZone = userTimeZoneId != null ? TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId) : TimeZoneInfo.Utc;

    var group = await _context.OysterGroups
      .Include(g => g.Members)
      .FirstOrDefaultAsync(g => g.Id == groupId);

    if (group is null || !group.Members.Contains(currentUser)) {
      return NotFound();
    }

    // Verify the posts exists and belongs to the current group
    var post = await _context.Posts
      .Include(p => p.Group)
      .FirstOrDefaultAsync(p => p.Id == postId);
    
    if (post is null || post.Group != group) {
      return NotFound();
    }
    // Check if any new comments have appeared since the client last checked.
    // If so, send them, along with the response about adding a new comment,
    // so that they are displayed before the user's new comment
    var postHasChanged = post.LastChange.DateTime > lastClientChangeUtc;

    List<CommentViewModel> newComments = new();

    if (postHasChanged) {
      newComments = await _context.Comments
        .OrderByDescending(c => c.CreatedAt)
        .Where(c => c.Post.Id == postId && c.CreatedAt > lastClientChangeUtc)
        .Select(c => new CommentViewModel {
          Id = c.Id,
          Content = c.Content,
          CreatedAt = c.CreatedAt.DateTime,
          AuthorId = c.Author.Id,
          AuthorDisplayName = c.Author.DisplayName,
          AuthorUsername = c.Author.PublicUsername,
          AuthorAvatar = c.Author.Avatar
        })
        .ToListAsync();
      foreach (var c in newComments) {
        // Return comments with timezone converted to user's preferred from UTC
        c.CreatedAtFormatted = DateFormatter.GetCommentFormat(c.CreatedAt, userTimeZone);
        c.CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(c.CreatedAt, userTimeZone);
      }
    }
    // Load current user's avatar to return the image URL with the new comment data
    await _context.Entry(currentUser).Reference(u => u.Avatar).LoadAsync();

    var newComment = new Comment {
      Content = comment.Content,
      Author = currentUser,
      CreatedAt = DateTimeOffset.UtcNow,
      Post = post
    };

    _context.Comments.Add(newComment);

    // Update post's info (a new comment has appeared)
    post.LastChange = newComment.CreatedAt;
    // The time sent to the client ends up being rounded down due to
    // smaller precision, which results in the new comment being possibly
    // shown twice when the next update happens.
    // Thus, artificially adding 1 ms solves the difference of precision.
    var lastUpdateCheckUtc = newComment.CreatedAt.AddMilliseconds(1);
    
    try {
      await _context.SaveChangesAsync();
      // The view model is sent back to the client (comment was created)
      var newCommentViewModel = new CommentViewModel {
        Id = newComment.Id,
        Content = newComment.Content,
        CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(newComment.CreatedAt.DateTime, userTimeZone),
        CreatedAtFormatted = DateFormatter.GetCommentFormat(newComment.CreatedAt.DateTime, userTimeZone),
        AuthorId = newComment.Author.Id,
        AuthorDisplayName = newComment.Author.DisplayName,
        AuthorUsername = newComment.Author.PublicUsername,
        AuthorAvatar = newComment.Author.Avatar
      };
      return new JsonResult(new
        {commentAdded = true, postHasChanged, newComments, newComment = newCommentViewModel, lastUpdateCheckUtc});
    } catch (DbUpdateException) {
      return new JsonResult(new {commentAdded = false, postHasChanged, newComments, lastUpdateCheckUtc});
    }
  }

  // Comment view model for AJAX requests with necessary info to render on the page
  public class CommentViewModel {
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedAtFormatted { get; set; }
    public string AuthorId { get; set; }
    public string AuthorDisplayName { get; set; }
    public string AuthorUsername { get; set; }
    public Photo AuthorAvatar { get; set; }
  }

  // User only sends the comment's content
  public class NewCommentModel {
    public string Content { get; set; }
  }
}
}