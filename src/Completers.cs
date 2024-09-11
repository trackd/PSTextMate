
using System;
using System.Linq;
using System.Management.Automation;

namespace PwshSpectreConsole.TextMate;
public class TextMateLanguages : IValidateSetValuesGenerator
{
    public string[] GetValidValues()
    {
        return TextMateHelper.Languages;
    }
    public static bool IsSupportedLanguage(string language)
    {
        return TextMateHelper.Languages.Contains(language);
    }
}
public class TextMateExtensions : IValidateSetValuesGenerator
{
    public string[] GetValidValues()
    {
        return TextMateHelper.Extensions;
    }
    public static bool IsSupportedExtension(string extension)
    {
        return TextMateHelper.Extensions.Contains(extension);
    }
    public static bool IsSupportedFile(string file)
    {
        return TextMateHelper.Extensions.Contains(System.IO.Path.GetExtension(file));
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
