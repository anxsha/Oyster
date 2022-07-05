using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Oyster.Areas.Identity.Data;
using Oyster.Data;

namespace Oyster.Pages {
[Authorize]
public class HomeModel : PageModel {
  private readonly UserManager<OysterUser> _userManager;
  private readonly OysterContext _context;

  public OysterUser CurrentUser;

  public HomeModel(OysterContext context, UserManager<OysterUser> userManager) {
    _context = context;
    _userManager = userManager;
  }
  public void OnGet() {
    CurrentUser = _userManager.GetUserAsync(User).Result;

  }
}
}