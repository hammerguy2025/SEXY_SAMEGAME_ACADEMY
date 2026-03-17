mergeInto(LibraryManager.library, {
  SameGame_GetBrowserLanguage: function () {
    var language = "";

    if (typeof navigator !== "undefined") {
      if (navigator.languages && navigator.languages.length > 0) {
        language = navigator.languages[0] || "";
      } else if (navigator.language) {
        language = navigator.language;
      } else if (navigator.userLanguage) {
        language = navigator.userLanguage;
      }
    }

    return stringToNewUTF8(language || "");
  }
});
