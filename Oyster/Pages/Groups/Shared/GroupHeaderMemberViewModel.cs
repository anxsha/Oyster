using Microsoft.AspNetCore.Mvc.RazorPages;
using Oyster.Areas.Identity.Data;

namespace Oyster.Pages.Groups.Shared {
// Data needed for the group header to display some random group members with their avatars
public class GroupHeaderMemberViewModel {
  public string DisplayName { get; set; }
  public Photo Avatar { get; set; }
}
}