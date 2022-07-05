using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oyster.Areas.Identity.Data {
public class OysterGroup {
  [Key] public int Id { get; set; }
  [Required] [StringLength(120)] public string Name { get; set; }
  [StringLength(3000)] public string Description { get; set; }
  public virtual ICollection<OysterUser> Members { get; set; }
  public virtual OysterUser Founder { get; set; }
  public virtual Photo Photo { get; set; }
  public virtual ICollection<Event> Events { get; set; }
  public virtual ICollection<Post> Posts { get; set; }
  public override string ToString()
    => Name;
}
}