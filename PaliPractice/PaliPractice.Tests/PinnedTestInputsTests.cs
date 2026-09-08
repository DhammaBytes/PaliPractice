using System.Security.Cryptography;
using System.Text.Json;
using Path = System.IO.Path;

namespace PaliPractice.Tests;

[TestFixture]
public class PinnedTestInputsTests
{
    string _root = null!;
    string _locations = null!;
    string _bundle = null!;
    string _candidate = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("pali-pinned-inputs-").FullName;
        _locations = Path.Combine(_root, "locations.json");
        _bundle = Path.Combine(_root, "bundle.json");
        _candidate = Path.Combine(_root, "inputs.json");
        var locations = new Dictionary<string, object>();
        var inputs = new Dictionary<string, object>();
        foreach (var name in new[] { "dpd", "cst", "bjt", "sya", "sc", "adjustments" })
        {
            var path = name + ".fixture";
            File.WriteAllText(Path.Combine(_root, path), name);
            var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(_root, path))));
            locations.Add(name, new { path });
            inputs.Add(name, new { path, sha256 = hash });
        }
        File.WriteAllText(_locations, JsonSerializer.Serialize(new { inputs = locations }));
        File.WriteAllText(_bundle, JsonSerializer.Serialize(new { english = new { inputs } }));
        File.WriteAllText(_candidate, JsonSerializer.Serialize(new { inputs }));
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [Test]
    public void BundleInputsResolveRelativeToLocationManifestAndMatchProvenance()
    {
        var paths = PinnedTestInputs.Load(_locations, _bundle);
        Assert.That(paths["dpd"], Is.EqualTo(Path.Combine(_root, "dpd.fixture")));
        Assert.That(paths["adjustments"], Is.EqualTo(Path.Combine(_root, "adjustments.fixture")));
    }

    [Test]
    public void CandidateUsesItsOwnPinnedInputs()
    {
        File.Delete(_bundle);
        var paths = PinnedTestInputs.Load(_candidate, _candidate);
        Assert.That(paths["dpd"], Is.EqualTo(Path.Combine(_root, "dpd.fixture")));
    }

    [TestCase("dpd")]
    [TestCase("cst")]
    [TestCase("adjustments")]
    public void WrongSourceBytesFailBeforeComparison(string name)
    {
        File.WriteAllText(Path.Combine(_root, name + ".fixture"), "different release");
        Assert.That(() => PinnedTestInputs.Load(_locations, _bundle),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains($"'{name}' checksum mismatch"));
    }

    [Test]
    public void MissingPinnedDpdDoesNotFallBackToCheckout()
    {
        File.Delete(Path.Combine(_root, "dpd.fixture"));
        Directory.CreateDirectory(Path.Combine(_root, "dpd-db"));
        File.WriteAllText(Path.Combine(_root, "dpd-db", "dpd.db"), "dpd");
        Assert.That(() => PinnedTestInputs.Load(_locations, _bundle),
            Throws.TypeOf<FileNotFoundException>().With.Message.Contains("Pinned input 'dpd' is missing"));
    }

    [Test]
    public void ExplicitInputManifestCannotOverrideBundlePin()
    {
        var contents = File.ReadAllText(_candidate);
        var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(_root, "dpd.fixture"))));
        File.WriteAllText(_candidate, contents.Replace(hash, new string('0', 64), StringComparison.Ordinal));
        Assert.That(() => PinnedTestInputs.Load(_candidate, _bundle),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'dpd' disagrees"));
    }

    [Test]
    public void NonemptyDpdWalInvalidatesMatchingDatabaseFile()
    {
        File.WriteAllText(Path.Combine(_root, "dpd.fixture-wal"), "uncheckpointed changes");
        Assert.That(() => PinnedTestInputs.Load(_locations, _bundle),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("nonempty WAL"));
    }
}
