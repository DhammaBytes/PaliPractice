using System.Globalization;
using PaliPractice.Localization;

namespace PaliPractice.Tests.Localization;

[TestFixture]
public class AppTextTests
{
    [TestCase("Settings.Row.Language", "Settings/Row.Language")]
    [TestCase("Grammar.Case.Nominative.Full", "Grammar/Case.Nominative.Full")]
    public void ToResourceLoaderKey_UsesCompiledResourceIdentifierShape(string key, string expected)
    {
        AppText.ToResourceLoaderKey(key).Should().Be(expected);
    }

    [Test]
    public void Get_RussianResourceInHeadlessContext_DoesNotFallbackToDisk()
    {
        using var _ = new CultureScope("ru");

        AppText.Get("Settings.Row.Language").Should().Be("[Settings.Row.Language]");
    }

    sealed class CultureScope : IDisposable
    {
        readonly CultureInfo _originalCulture;
        readonly CultureInfo _originalUICulture;

        public CultureScope(string cultureName)
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUICulture = CultureInfo.CurrentUICulture;

            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUICulture;
        }
    }
}
