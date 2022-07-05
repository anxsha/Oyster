using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Oyster.Areas.Identity.Data {
public class Poll {
  [Key] public int Id { get; set; }
  [StringLength(1000)] public string Title { get; set; }
  public virtual ICollection<PollOption> Options { get; set; }
  public virtual Post Post { get; set; }
}
}