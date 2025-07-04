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
        return TextMateHelper.Extensions?.Contains(extension) == true;
    }
    public static bool IsSupportedFile(string file)
    {
        var ext = Path.GetExtension(file);
        return TextMateHelper.Extensions?.Contains(ext) == true;
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
        throw new ArgumentException("Input must be a string representing a file extension., '.ext' format expected.", nameof(inputData));
    }

}
