
using System;
using System.Linq;
using System.Management.Automation;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace PwshSpectreConsole.TextMate;

internal class Completors
{
    internal static readonly RegistryOptions _registryOptions = new(ThemeName.Dark);
    internal static readonly string[] Extensions;
    internal static readonly string[] Languages;

    static Completors()
    {
        try
        {
            var availableLanguages = _registryOptions.GetAvailableLanguages();
            if (availableLanguages == null || !availableLanguages.Any())
            {
                throw new Exception("No available languages found.");
            }
            if (availableLanguages.Any(x => x == null))
            {
                throw new Exception("One or more available languages are null.");
            }

            Extensions = availableLanguages
                .Where(x => x.Extensions != null)
                .SelectMany(x => x.Extensions)
                .ToArray();

            Languages = availableLanguages
                .Where(x => x.Id != null)
                .Select(x => x.Id)
                .ToArray();
        }
        catch (Exception ex)
        {
            throw new TypeInitializationException(nameof(Completors), ex);
        }
    }
}

public class TextMateLanguages : IValidateSetValuesGenerator
{
    public string[] GetValidValues()
    {
        return Completors.Languages;
    }
}
public class TextMateExtensions : IValidateSetValuesGenerator
{
    public string[] GetValidValues()
    {
        return Completors.Extensions;
    }
}
public class TextMateExtensionTransform : ArgumentTransformationAttribute
{
    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        if (inputData is string input)
        {
            return input.StartsWith('.') ? input : '.' + input;
        }
        return inputData;
    }

}
