using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Oyster.Areas.Identity.Data {
public class Post {
  [Key] public int Id { get; set; }
  [Required] [StringLength(5000)] public string Content { get; set; }
  [Required] public DateTimeOffset CreatedAt { get; set; }
  [Required] public DateTimeOffset LastChange { get; set; }
  public virtual OysterUser Author { get; set; }
  public virtual OysterGroup Group { get; set; }
  public virtual ICollection<Comment> Comments { get; set; }
  public virtual Poll Poll { get; set; }
}
}