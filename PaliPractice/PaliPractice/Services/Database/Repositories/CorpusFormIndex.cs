namespace PaliPractice.Services.Database.Repositories;

/// <summary>Corpus membership for forms whose spelling is verified at build time.</summary>
internal sealed class CorpusFormIndex
{
    readonly HashSet<(int HeadwordId, long FormId)> _forms;
    readonly HashSet<long> _primaryIds;

    public CorpusFormIndex(IEnumerable<(int HeadwordId, long FormId)> forms, IReadOnlySet<int> primaryHeadwords)
    {
        // Ending IDs are local to a headword's paradigm; lemma/form ID alone can collide.
        // Build evidence checks spelling before these compact membership keys are shipped.
        _forms = forms.ToHashSet();
        // Queue IDs omit headword identity, so project only the selected practice senses.
        _primaryIds = _forms.Where(f => primaryHeadwords.Contains(f.HeadwordId))
            .Select(f => f.FormId).ToHashSet();
    }

    public bool ContainsPrimary(long formId) => _primaryIds.Contains(formId);

    public bool Contains(int headwordId, long formId) =>
        _forms.Contains((headwordId, formId));
}
