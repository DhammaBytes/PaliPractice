# Released history reconstruction

The app embeds `Data/history-v1.1.json.gz` to preserve a recoverable display for
older practice history when a lemma leaves the frequency cutoff. The manifest
pins the actual v1.1 database and application source. The exporter uses the
released repositories, primary-sense selection, ending tables, and inflection
service. Its console adapter supplies only database access and enum dispatch.

Reproduce into a new isolated directory:

```sh
.venv/bin/python scripts/export_history_baseline.py --output /absolute/new/workspace
```

The export contains 57,998 resolvable grammatical combinations, including
theoretical forms. It is a reconstruction of what the released resolver would
display, not proof of which alternative a user saw in an individual attempt.
Migrated rows record `ReconstructedV11`; new attempts record `Practiced` and store
the displayed answer. Rows missing from this baseline remain unknown. In
particular, the v1.0 App Store build has no verified source/binary baseline here.

The export is immutable release evidence. Do not regenerate it with current
grammar code or replace it with a new dictionary's forms. Updating the DPD
candidate must leave this history resource unchanged.

The migration loads this dictionary only when a history row lacks its snapshot.
One migration-local instance serves noun and verb backfill, then becomes eligible
for collection. History is processed in ordered batches of at most 256 missing
snapshots; unresolved rows advance the cursor without being changed. Fresh
installs and histories with captured snapshots do not decompress the resource.
