using System.Globalization;
using PaliPractice.Localization;

namespace PaliPractice.Tests.Localization;

[TestFixture]
public class AppTextTests
{
    [Test]
    public void Get_EnglishResourceInHeadlessContext_ReturnsValue()
    {
        using var _ = new CultureScope("en");

        AppText.Get("App.Name").Should().Be("Pāli Practice");
    }

    [Test]
    public void Get_RussianResourceInHeadlessContext_ReturnsValue()
    {
        using var _ = new CultureScope("ru");

        AppText.Get("Settings.Row.Language").Should().Be("Язык перевода");
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
