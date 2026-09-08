namespace PaliPractice.Services.Database.Repositories;

/// <summary>Irregular spelling for one headword-scoped ending slot.</summary>
internal readonly record struct StoredHeadwordForm(int HeadwordId, long FormId, string Form);

internal sealed class HeadwordFormIndex
{
    readonly Dictionary<(int HeadwordId, long FormId), string> _forms = new();

    public HeadwordFormIndex(IEnumerable<StoredHeadwordForm> forms)
    {
        foreach (var form in forms)
        {
            var key = (form.HeadwordId, form.FormId);
            if (_forms.TryGetValue(key, out var previous) && previous != form.Form)
                throw new InvalidDataException($"Conflicting headword form {key}");
            _forms[key] = form.Form;
        }
    }

    // Read explicit ending slots: SQLite row order is not a grammatical ordering.
    public List<string> GetForms(int headwordId, long baseFormId, int maximumEndings)
    {
        var forms = new List<string>();
        for (int ending = 1; ending <= maximumEndings; ending++)
        {
            if (_forms.TryGetValue((headwordId, baseFormId + ending), out var form))
                forms.Add(form);
        }
        return forms;
    }
}
