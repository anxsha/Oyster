using System;
using System.Collections.Generic;
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
using Oyster.HelperClasses;
using Oyster.HelperClasses.ValidationAttributes;

namespace Oyster.Pages.Groups {
[Authorize]
public class CreateModel : PageModel {
  private readonly UserManager<OysterUser> _userManager;
  private readonly OysterContext _context;
  private readonly string _contentRoot;

  public CreateModel(OysterContext context, UserManager<OysterUser> userManager, IWebHostEnvironment environment) {
    _context = context;
    _userManager = userManager;
    _contentRoot = environment.ContentRootPath;
  }

  // New group model form
  [BindProperty] public InputModel Input { get; set; }
  
  // Message to show when image file type is invalid
  public readonly string InvalidPhotoFileTypeMessage =
    "The image you uploaded has an invalid type, try a different one or leave the field empty.";

  // True by default in order not to show an error message when
  // the page was just loaded without a POST request
  public bool IsPhotoFileTypeValid = true;

  // To identify when there is a need to show an alert
  public bool IsServerError;

  public void OnGet() { }

  // Create group on form submit
  public async Task<IActionResult> OnPostAsync() {
    var group = new OysterGroup();
    var groupPhoto = new Photo();

    var currentUser = await _context.Users
      .Include(u => u.Groups)
      .Include(u => u.GroupsCreated)
      .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

    if (!ModelState.IsValid) {
      return Page();
    }
    
    // The file is correct
    if (Input.ImageFile is {Length: > 0}) {
      // Validate the file is a proper image of type JPG, PNG or GIF
      using (var reader = new BinaryReader(Input.ImageFile.OpenReadStream())) {
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
          var extension = Path.GetExtension(Input.ImageFile.FileName);
          var fileName = $"group_photo_{uniqueId}{extension}";
          var filePath = Path.Combine(_contentRoot, @"wwwroot/uploads/images", fileName);
          using (var fileStream = new FileStream(filePath, FileMode.Create)) {
            await Input.ImageFile.CopyToAsync(fileStream);
            groupPhoto.Url = $@"/uploads/images/{fileName}";
            group.Photo = groupPhoto;
          }
        } else {
          IsPhotoFileTypeValid = false;
          return Page();
        }
      }
    }
    // Remove consecutive whitespaces in group name
    var formattedName = string.Join(" ", Input.Name.Split(new char[] { ' ' },
      StringSplitOptions.RemoveEmptyEntries));
    group.Name = formattedName;
    group.Description = Input.Description;

    group.Founder = currentUser;

    group.Members = new List<OysterUser> {currentUser};

    // Create group
    _context.OysterGroups.Add(group);

    try {
      await _context.SaveChangesAsync();
    } catch (DbUpdateException) {
      IsServerError = true;
      return Page();
    }

    return RedirectToPage("/Groups/Index");
  }

// Model for creating a new group
  public class InputModel {
    [Required] [StringLength(120)] public string Name { get; set; }
    [StringLength(3000)] public string Description { get; set; }

    [MaxFileSize(2*1024*1024)] public IFormFile ImageFile { get; set; }
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
}