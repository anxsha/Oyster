using System;
using System.ComponentModel.DataAnnotations;

namespace Oyster.Areas.Identity.Data {
public class Comment {
  [Key] public int Id { get; set; }
  [Required] [StringLength(3000)] public string Content { get; set; }
  [Required] public DateTimeOffset CreatedAt { get; set; }
  public virtual OysterUser Author { get; set; }
  public virtual Post Post { get; set; }
}
}