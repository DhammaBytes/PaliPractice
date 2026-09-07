using PaliPractice.Services.Database;
using SQLite;
using Path = System.IO.Path;

namespace PaliPractice.Tests.UserData;

[TestFixture]
public class BundledDatabaseCopyTests
{
    string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"pali-copy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, recursive: true);

    string CreateDatabase(string name, int version)
    {
        var path = Path.Combine(_directory, name);
        using var connection = new SQLiteConnection(path);
        foreach (var table in new[] { "nouns", "verbs", "nouns_details", "verbs_details" })
            connection.Execute($"CREATE TABLE {table} (id INTEGER PRIMARY KEY)");
        connection.Execute($"PRAGMA user_version={version}");
        return path;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CompleteCopyReplacesStaleOrCorruptDatabase(bool corrupt)
    {
        var destination = CreateDatabase("installed.db", 1);
        if (corrupt) File.WriteAllText(destination, "corrupt database");
        var sourcePath = CreateDatabase("bundled.db", 2);
        File.WriteAllText(destination + ".tmp", "interrupted old attempt");
        using var source = File.OpenRead(sourcePath);
        BundledDatabaseCopy.Replace(source, destination, 2);
        File.ReadAllBytes(destination).Should().Equal(File.ReadAllBytes(sourcePath));
        File.Exists(destination + ".tmp").Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InvalidReplacementPreservesInstalledBytes(bool wrongVersion)
    {
        var destination = CreateDatabase("installed.db", 1);
        var before = File.ReadAllBytes(destination);
        using Stream source = wrongVersion
            ? File.OpenRead(CreateDatabase("wrong-version.db", 9))
            : new MemoryStream("incomplete SQLite"u8.ToArray());
        Action replace = () => BundledDatabaseCopy.Replace(source, destination, 2);
        replace.Should().Throw<Exception>();
        File.ReadAllBytes(destination).Should().Equal(before);
        File.Exists(destination + ".tmp").Should().BeFalse();
    }

    [Test]
    public void InterruptedStreamPreservesInstalledDatabase()
    {
        var destination = CreateDatabase("installed.db", 1);
        var before = File.ReadAllBytes(destination);
        using var source = new InterruptedStream(File.ReadAllBytes(CreateDatabase("bundle.db", 2)));
        Action replace = () => BundledDatabaseCopy.Replace(source, destination, 2);
        replace.Should().Throw<IOException>();
        File.ReadAllBytes(destination).Should().Equal(before);
        File.Exists(destination + ".tmp").Should().BeFalse();
    }

    [Test]
    public void RuntimeUsesPatchedNativeSqlite()
    {
        using var connection = new SQLiteConnection(":memory:");
        var version = Version.Parse(connection.ExecuteScalar<string>("SELECT sqlite_version()"));
        version.Should().BeGreaterThanOrEqualTo(new Version(3, 50, 2));
        TestContext.Out.WriteLine($"Loaded native SQLite {version}");
    }

    sealed class InterruptedStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override void CopyTo(Stream destination, int bufferSize)
        {
            destination.Write(bytes, 0, 100);
            throw new IOException("Injected interrupted asset stream");
        }
    }
}
