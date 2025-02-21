
using System;
using System.Collections.Generic;
using System.Linq;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate;

internal static class TextMateHelper
{
  internal static readonly RegistryOptions _registryOptions = new(ThemeName.Dark);
  internal static List<Language> AvailableLanguages = _registryOptions.GetAvailableLanguages();
  internal static readonly string[] Extensions;
  internal static readonly string[] Languages;

  static TextMateHelper()
  {
    try
    {
      Extensions = AvailableLanguages
          .Where(x => x.Extensions != null)
          .SelectMany(x => x.Extensions)
          .ToArray();

      Languages = AvailableLanguages
          .Where(x => x.Id != null)
          .Select(x => x.Id)
          .ToArray();
    }
    catch (Exception ex)
    {
      throw new TypeInitializationException(nameof(TextMateHelper), ex);
    }
  }
}
