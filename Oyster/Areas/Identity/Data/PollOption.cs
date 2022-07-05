using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Oyster.Areas.Identity.Data {
public class PollOption {
  [Key] public int Id { get; set; }
  [Required] [StringLength(1000)] public string Text { get; set; }
  public virtual ICollection<OysterUser> Voters { get; set; }
}
}