namespace Oyster.HelperClasses {
public static class DefaultDataProvider {
  /// <summary>
  /// Used when an Oyster group has no photo provided or the photo turns out
  /// to be invalid / corrupted.
  /// </summary>
  /// <returns>A string with a URL to the default photo.</returns>
  public static string GetDefaultGroupPhoto() {
    return
      @"/uploads/images/default_group_photo.svg";
  }
  public static string GetDefaultUserAvatar() {
    return
      @"/uploads/images/default_avatar.svg";
  }
  public static string GetDeactivatedUserAvatar() {
    return
      @"/uploads/images/default_deactivated_avatar.svg";
  }
}
}