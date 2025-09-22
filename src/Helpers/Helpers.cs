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
            RegistryOptions _registryOptions = new(ThemeName.DarkPlus);
            AvailableLanguages = _registryOptions.GetAvailableLanguages();

            // Get all the extensions and languages from the available languages
            Extensions = [.. AvailableLanguages
                .Where(x => x.Extensions is not null)
                .SelectMany(x => x.Extensions)];

            Languages = [.. AvailableLanguages
                .Where(x => x.Id is not null)
                .Select(x => x.Id)];
        }
        catch (Exception ex)
        {
            throw new TypeInitializationException(nameof(TextMateHelper), ex);
        }
    }
}
