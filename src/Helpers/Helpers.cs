
using System;
using System.Collections.Generic;
using System.Linq;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate;

public static class TextMateHelper
{
    public static readonly string[] Extensions;
    public static readonly string[] Languages;
    public static readonly List<Language> AvailableLanguages;

    static TextMateHelper()
    {
        try
        {
            RegistryOptions _registryOptions = new(ThemeName.Dark);
            AvailableLanguages = _registryOptions.GetAvailableLanguages();

            // Get all the extensions and languages from the available languages
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
