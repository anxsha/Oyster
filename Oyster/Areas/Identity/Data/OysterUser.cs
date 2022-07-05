using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Oyster.Areas.Identity.Data {
// Add profile data for application users by adding properties to the OysterUser class
public class OysterUser : IdentityUser {
  [Required]
  [Display(Name = "Display name")]
  [StringLength(50)]
  [PersonalData]
  public string DisplayName { get; set; }

  public virtual ICollection<OysterGroup> Groups { get; set; }
  public virtual ICollection<OysterGroup> GroupsCreated { get; set; }
  [PersonalData]
  [Required] [StringLength(100)] public string PublicUsername { get; set; }
  [PersonalData]
  [Required] [StringLength(256)] public string UserTimeZoneId { get; set; }
  [Required] public override string Email { get; set; }
  [Required] public override string NormalizedEmail { get; set; }
  public virtual Photo Avatar { get; set; }
  public virtual ICollection<PollOption> Votes { get; set; }
  public bool IsUserDeactivated { get; set; } = false;

  public override string ToString()
    => DisplayName + " @ " + PublicUsername;
}
}