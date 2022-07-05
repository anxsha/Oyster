using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

namespace Oyster.Areas.Identity.Pages.Account.Manage {
public partial class IndexModel : PageModel {
  private readonly UserManager<OysterUser> _userManager;
  private readonly SignInManager<OysterUser> _signInManager;
  private readonly OysterContext _context;
  private readonly string _contentRoot;

  public IndexModel(
    OysterContext context,
    UserManager<OysterUser> userManager,
    SignInManager<OysterUser> signInManager, IWebHostEnvironment environment) {
    _context = context;
    _userManager = userManager;
    _signInManager = signInManager;
    _contentRoot = environment.ContentRootPath;
  }

  [TempData] public string StatusMessage { get; set; }

  public bool IsServerError;

  public OysterUser CurrentUser { get; set; }

  [BindProperty]
  [MaxFileSize(2 * 1024 * 1024)]
  public IFormFile ImageFile { get; set; }

  [BindProperty]
  [Display(Name = "Display name")]
  [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
  public string DisplayName { get; set; }

  public readonly string InvalidPhotoFileTypeMessage =
    "The image you uploaded has an invalid type, try a different one or leave the field empty.";

  // True by default in order not to show an error message when
  // the page was just loaded without a POST request
  public bool IsPhotoFileTypeValid = true;

  public async Task<IActionResult> OnGetAsync() {
    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
    if (CurrentUser is null) {
      return NotFound();
    }

    return Page();
  }

  public async Task<IActionResult> OnPostAsync() {
    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
    if (CurrentUser is null) {
      return NotFound();
    }
    
    if (!ModelState.IsValid) {
      return Page();
    }

    // The file is correct
    if (ImageFile is {Length: > 0}) {
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
            var fileName = $"user_avatar_{uniqueId}{extension}";
            var filePath = Path.Combine(_contentRoot, @"wwwroot/uploads/images", fileName);
            
          using (var fileStream = new FileStream(filePath, FileMode.Create)) {
            await ImageFile.CopyToAsync(fileStream);
            // Delete old one
            if (CurrentUser.Avatar is not null) {
              var oldFileName = CurrentUser.Avatar.Url.Split("/")[3];
              var oldFilePath = Path.Combine(_contentRoot, @"wwwroot/uploads/images", oldFileName);
              System.IO.File.Delete(oldFilePath);
            }
            CurrentUser.Avatar = new Photo {Url = $@"/uploads/images/{fileName}"};
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

  public async Task<IActionResult> OnPostChangeDisplayNameAsync() {
    CurrentUser = await _context.Users
      .Include(u => u.Avatar)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
    if (CurrentUser is null) {
      return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
    }
    if (!ModelState.IsValid || string.IsNullOrWhiteSpace(DisplayName)) {
      return Page();
    }
    
    // Remove consecutive white-space from user's entered display name
    var displayName = string.Join(" ", DisplayName.Split(new char[] { ' ' },
      StringSplitOptions.RemoveEmptyEntries));
    CurrentUser.DisplayName = displayName;
    try {
      await _context.SaveChangesAsync();
    } catch (DbUpdateException) {
      IsServerError = true;
      return Page();
    }
    return Page();
  }

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
}