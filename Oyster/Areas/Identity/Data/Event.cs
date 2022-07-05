using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Oyster.Areas.Identity.Data {
public class Event {
  [Key] public int Id { get; set; }
  [Required] [StringLength(120)] public string Name { get; set; }
  [StringLength(5000)] public string Description { get; set; }
  public DateTimeOffset? StartDate { get; set; }
  public DateTimeOffset? EndDate { get; set; }
  public virtual OysterUser Author { get; set; }
  public virtual ICollection<Comment> Comments { get; set; }
}
}