using System.Security.Cryptography;
using System.Text;
using Path = System.IO.Path;

namespace PaliPractice.Tests.Practice.Simulation;

internal static class SrsSourceIdentity
{
    public static string SchedulerHash()
    {
        var directory = Path.Combine(TestPaths.RepositoryRoot, "PaliPractice", "PaliPractice", "Services", "Practice");
        var sources = new[] { "PracticeQueueBuilder.cs", "NewFormSchedule.cs" }
            .Where(name => File.Exists(Path.Combine(directory, name)))
            .Select(name => name + "\n" + File.ReadAllText(Path.Combine(directory, name)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", sources))));
    }
}
