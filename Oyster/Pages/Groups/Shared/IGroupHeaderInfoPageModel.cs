using System.Collections.Generic;
using Oyster.Areas.Identity.Data;

namespace Oyster.Pages.Groups.Shared;

// To render the group header, the properties below need to be filled
public interface IGroupHeaderInfoPageModel {
  // Group founder sees different links and options (e.g. adding new members)
  public bool IsUserGroupFounder { get; set; }
  public OysterGroup Group { get; set; }
  // List of a few random members to display with avatars on the group header
  public List<GroupHeaderMemberViewModel> MembersToDisplay { get; set; }
}