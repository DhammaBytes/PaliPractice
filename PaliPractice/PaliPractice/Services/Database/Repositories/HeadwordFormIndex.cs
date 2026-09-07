namespace PaliPractice.Services.Database.Repositories;

/// <summary>Internal form identity includes its source headword and rendered text.</summary>
internal readonly record struct StoredHeadwordForm(int HeadwordId, long FormId, string Form);

internal sealed class HeadwordFormIndex
{
    readonly Dictionary<(int HeadwordId, long FormId), string> _forms = new();
    readonly HashSet<long> _primaryIds = new();

    public HeadwordFormIndex(IEnumerable<StoredHeadwordForm> forms, IReadOnlySet<int> primaryHeadwords)
    {
        foreach (var form in forms)
        {
            var key = (form.HeadwordId, form.FormId);
            if (_forms.TryGetValue(key, out var previous) && previous != form.Form)
                throw new InvalidDataException($"Conflicting headword form {key}");
            _forms[key] = form.Form;
            // HeadwordId=0 exists only in legacy bundles lacking this column.
            if (form.HeadwordId == 0 || primaryHeadwords.Contains(form.HeadwordId))
                _primaryIds.Add(form.FormId);
        }
    }

    public bool ContainsPrimary(long formId) => _primaryIds.Contains(formId);

    public bool Contains(int headwordId, long formId, string renderedForm) =>
        _forms.TryGetValue((headwordId, formId), out var stored) ? stored == renderedForm :
        _forms.ContainsKey((0, formId));

    public List<string> GetForms(int headwordId, long baseFormId, int maximumEndings)
    {
        var forms = new List<string>();
        for (int ending = 1; ending <= maximumEndings; ending++)
        {
            if (_forms.TryGetValue((headwordId, baseFormId + ending), out var form) ||
                _forms.TryGetValue((0, baseFormId + ending), out form))
                forms.Add(form);
        }
        return forms;
    }
}
