namespace PaliPractice.Services.Database.Repositories;

/// <summary>Corpus membership for forms whose spelling is verified at build time.</summary>
internal sealed class CorpusFormIndex
{
    readonly HashSet<(int HeadwordId, long FormId)> _forms;
    readonly HashSet<long> _primaryIds;

    public CorpusFormIndex(IEnumerable<(int HeadwordId, long FormId)> forms, IReadOnlySet<int> primaryHeadwords)
    {
        _forms = forms.ToHashSet();
        _primaryIds = _forms.Where(f => f.HeadwordId == 0 || primaryHeadwords.Contains(f.HeadwordId))
            .Select(f => f.FormId).ToHashSet();
    }

    public bool ContainsPrimary(long formId) => _primaryIds.Contains(formId);

    public bool Contains(int headwordId, long formId) =>
        _forms.Contains((headwordId, formId)) || _forms.Contains((0, formId));
}
