using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Oyster.HelperClasses.ValidationAttributes {
/// <summary>
/// Helpful for file upload forms. Verifies that the file does not
/// exceed maximum specified size in bytes.
/// </summary>
public class MaxFileSize : ValidationAttribute {
  private readonly int _maxFileSize;

  public MaxFileSize(int maxFileSize) {
    _maxFileSize = maxFileSize;
  }

  protected override ValidationResult IsValid(
    object value, ValidationContext validationContext) {
    if (value is IFormFile file) {
      if (file.Length > _maxFileSize) {
        return new ValidationResult(GetErrorMessage());
      }
    }

    return ValidationResult.Success;
  }

  public string GetErrorMessage() {
    return $"Maximum file size is {(float) (_maxFileSize / 1024.0 / 1024.0)} MB.";
  }
}
}