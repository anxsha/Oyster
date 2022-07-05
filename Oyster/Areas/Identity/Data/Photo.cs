using System.ComponentModel.DataAnnotations;

namespace Oyster.Areas.Identity.Data {
public class Photo {
  [Key] public int Id { get; set; }
  [StringLength(10000)]public string Url { get; set; }
}
}