using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Oyster.Areas.Identity.Data;
using Oyster.Data;
using Oyster.HelperClasses.ValidationAttributes;
using Oyster.Pages.Groups.Shared;

namespace Oyster.Pages.Groups;

[Authorize]
public class EditModel : PageModel, IGroupHeaderInfoPageModel {
  private readonly UserManager<OysterUser> _userManager;
  private readonly OysterContext _context;
  private readonly string _contentRoot;

  public EditModel(OysterContext context, UserManager<OysterUser> userManager, IWebHostEnvironment environment) {
    _context = context;
    _userManager = userManager;
    _contentRoot = environment.ContentRootPath;
  }
  // File uploaded in form to be new group photo, max. 2 MB and in: PNG, JPG or GIF
  [BindProperty]
  [MaxFileSize(2 * 1024 * 1024)]
  public IFormFile ImageFile { get; set; }

  // Edit group form field property
  [BindProperty] [StringLength(120)] public string NewGroupName { get; set; }

  // Edit group form field property
  [BindProperty]
  [StringLength(3000, ErrorMessage = "Description can have at max 3000 characters.")]
  public string NewDescription { get; set; }

  // Group deletion form confirmation
  [BindProperty]
  [DisplayName("Enter group name: ")]
  public string GroupNameConfirmation { get; set; }

  // Message to show when image file type is invalid
  public readonly string InvalidPhotoFileTypeMessage =
    "The image you uploaded has an invalid type, try a different one or leave the field empty.";

  // True by default in order not to show an error message when
  // the page was just loaded without a POST request
  public bool IsPhotoFileTypeValid = true;

  // To identify when there is a need to show an alert
  public bool IsServerError;

  // Number of group members to be shown with avatars on the group header
  private readonly int _membersToBeLoaded = 10;

  // Currently browsed group
  public OysterGroup Group { get; set; }

  // Members with loaded avatars to be shown on the group header
  public List<GroupHeaderMemberViewModel> MembersToDisplay { get; set; }
  public bool IsUserGroupFounder { get; set; }
  
  // Currently logged-in user
  public OysterUser CurrentUser { get; set; }

  public async Task<IActionResult> OnGetAsync(int id) {
    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    Group = await _context.OysterGroups
      .Include(g => g.Photo)
      .Include(g => g.Founder)
      .ThenInclude(f => f.Avatar)
      .FirstOrDefaultAsync(g => g.Id == id);

    // Check if the user and group exist and were fetched correctly
    if (Group is null || CurrentUser is null) {
      return NotFound();
    }

    if (Group.Founder == CurrentUser) {
      IsUserGroupFounder = true;
    } else {
      // Only the group founder is allowed to edit group info
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
    
    // Fill the form with existing description
    NewDescription = Group.Description;

    return Page();
  }

  // Submitted form for changing group photo 
  public async Task<IActionResult> OnPostAsync(int id) {
    CurrentUser = await _context.Users
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    Group = await _context.OysterGroups
      .Include(g => g.Photo)
      .Include(g => g.Founder)
      .FirstOrDefaultAsync(g => g.Id == id);

    // Check if the user and group exist and were fetched correctly
    if (Group is null || CurrentUser is null) {
      return NotFound();
    }

    if (Group.Founder == CurrentUser) {
      IsUserGroupFounder = true;
    } else {
      // Only the group founder is allowed to edit group info
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

    // Fill the form with existing description
    NewDescription = Group.Description;

    // Validate form
    if (!ModelState.IsValid) {
      return Page();
    }

    // The file is correct
    if (ImageFile is {Length: > 0}) {
      // Validate the file is a proper image of type JPG, PNG or GIF
      using (var reader = new BinaryReader(ImageFile.OpenReadStream())) {
        var signatures = _imageFileSignatures.Values.SelectMany(x => x).ToList();
        var headerBytes = reader.ReadBytes(_imageFileSignatures.Max(m => m.Value.Max(n => n.Length)));
        bool result = signatures.Any(signature => headerBytes.Take(signature.Length).SequenceEqual(signature));
        // The uploaded file has an accepted image signature
        if (result) {
          // Get a unique integer value from a database sequence to generate a filename
          // Create a sql parameter to save the query result
          var parameter = new SqlParameter("@result", System.Data.SqlDbType.Int) {
            Direction = System.Data.ParameterDirection.Output
          };
          // Fetch value from the database
          await _context.Database.ExecuteSqlRawAsync("set @result = next value for dbo.UniquePhotoId", parameter);
          var uniqueId = (int) parameter.Value;
          var extension = Path.GetExtension(ImageFile.FileName);
          var fileName = $"group_photo_{uniqueId}{extension}";
          var filePath = Path.Combine(_contentRoot, @"wwwroot/uploads/images", fileName);

          using (var fileStream = new FileStream(filePath, FileMode.Create)) {
            await ImageFile.CopyToAsync(fileStream);
            // Delete old one
            if (Group.Photo is not null) {
              var oldFileName = Group.Photo.Url.Split("/")[3];
              var oldFilePath = Path.Combine(_contentRoot, @"wwwroot/uploads/images", oldFileName);
              System.IO.File.Delete(oldFilePath);
            }

            Group.Photo = new Photo {Url = $@"/uploads/images/{fileName}"};
          }
        } else {
          IsPhotoFileTypeValid = false;
          return Page();
        }
      }
    }

    try {
      await _context.SaveChangesAsync();
    } catch (DbUpdateException) {
      IsServerError = true;
      return Page();
    }

    return Page();
  }

  public async Task<IActionResult> OnPostChangeGroupNameAsync(int id) {
    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    Group = await _context.OysterGroups
      .Include(g => g.Photo)
      .Include(g => g.Founder)
      .FirstOrDefaultAsync(g => g.Id == id);

    // Check if the user and group exist and were fetched correctly
    if (Group is null || CurrentUser is null) {
      return NotFound();
    }

    // Tracked entities are equatable
    if (Group.Founder == CurrentUser) {
      IsUserGroupFounder = true;
    } else {
      // Only the group founder is allowed to edit group info
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

    // Fill the form with existing description
    NewDescription = Group.Description;

    // Validate form and proceed if new group name was entered
    if (!ModelState.IsValid || string.IsNullOrWhiteSpace(NewGroupName)) {
      return Page();
    }

    // Remove consecutive whitespaces in group name
    var formattedName = string.Join(" ", NewGroupName.Split(new char[] {' '},
      StringSplitOptions.RemoveEmptyEntries));
    // Update group name
    Group.Name = formattedName;

    try {
      await _context.SaveChangesAsync();
    } catch (DbUpdateException) {
      IsServerError = true;
      return Page();
    }

    return Page();
  }

  public async Task<IActionResult> OnPostChangeGroupDescriptionAsync(int id) {
    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    Group = await _context.OysterGroups
      .Include(g => g.Photo)
      .Include(g => g.Founder)
      .FirstOrDefaultAsync(g => g.Id == id);

    // Check if the user and group exist and were fetched correctly
    if (Group is null || CurrentUser is null) {
      return NotFound();
    }

    // Tracked entities are equatable
    if (Group.Founder == CurrentUser) {
      IsUserGroupFounder = true;
    } else {
      // Only the group founder is allowed to edit group info
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

    // Validate form
    if (!ModelState.IsValid) {
      return Page();
    }
    // Update group description
    Group.Description = NewDescription;

    try {
      await _context.SaveChangesAsync();
    } catch (DbUpdateException) {
      IsServerError = true;
      return Page();
    }

    return Page();
  }

  public async Task<IActionResult> OnPostRemoveGroupAsync(int id) {
    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    Group = await _context.OysterGroups
      .Include(g => g.Photo)
      .Include(g => g.Founder)
      .FirstOrDefaultAsync(g => g.Id == id);

    // Check if the user and group exist and were fetched correctly
    if (Group is null || CurrentUser is null) {
      return NotFound();
    }

    // Tracked entities are equatable
    if (Group.Founder == CurrentUser) {
      IsUserGroupFounder = true;
    } else {
      // Only the group founder is allowed to edit group info
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

    // Validate form
    if (!ModelState.IsValid) {
      return Page();
    }

    // Group founder failed to enter the correct name as an action cofirmation
    if (GroupNameConfirmation != Group.Name) {
      ModelState.AddModelError("ConfirmGroupName", "Incorrect group name.");
      return Page();
    }

    // Include all related collections to cascade delete
    await _context.Entry(Group).Collection(g => g.Events).LoadAsync();
    await _context.Entry(Group).Collection(g => g.Members).LoadAsync();
    await _context.Entry(Group).Collection(g => g.Posts)
      .Query()
      .Include(p => p.Comments)
      .LoadAsync();
    foreach (var post in Group.Posts) {
      _context.Comments.RemoveRange(post.Comments);
    }

    // Remove group
    _context.Posts.RemoveRange(Group.Posts);
    _context.Events.RemoveRange(Group.Events);
    _context.OysterGroups.Remove(Group);

    try {
      await _context.SaveChangesAsync();
    } catch (DbUpdateException) {
      IsServerError = true;
      return Page();
    }

    return RedirectToPage("/Groups/Index");
  }

  // Proper file signatures for accepted images
  private readonly Dictionary<string, List<byte[]>> _imageFileSignatures = new() {
    {".gif", new List<byte[]> {new byte[] {0x47, 0x49, 0x46, 0x38}}},
    {".png", new List<byte[]> {new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A}}}, {
      ".jpeg", new List<byte[]> {
        new byte[] {0xFF, 0xD8, 0xFF, 0xE0},
        new byte[] {0xFF, 0xD8, 0xFF, 0xE2},
        new byte[] {0xFF, 0xD8, 0xFF, 0xE3},
        new byte[] {0xFF, 0xD8, 0xFF, 0xEE},
        new byte[] {0xFF, 0xD8, 0xFF, 0xDB},
      }
    }, {
      ".jpg", new List<byte[]> {
        new byte[] {0xFF, 0xD8, 0xFF, 0xE0},
        new byte[] {0xFF, 0xD8, 0xFF, 0xE1},
        new byte[] {0xFF, 0xD8, 0xFF, 0xE8},
        new byte[] {0xFF, 0xD8, 0xFF, 0xEE},
        new byte[] {0xFF, 0xD8, 0xFF, 0xDB},
      }
    },
  };
}