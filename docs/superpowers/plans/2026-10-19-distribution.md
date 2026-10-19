# Downloadable alpha and private Workshop preparation

Approved scope: supply a ready Windows mod with its bridge, matching APWorld and simple instructions; prepare and privately test Workshop before public availability.

## Contract
- Keep enrollment, grants, currency, outbox, bindings, bridge ledgers and preferences outside install/Workshop folders.
- Migrate existing files without deleting originals; conflicting files or failed migration must stop startup rather than silently reset delivery state.
- Package only explicit build inputs, never personal settings, saves, credentials or test seeds.
- Preserve the current character and completed seed. No gameplay progression required for distribution tests.
- Workshop remains private until subscription/update and loader acceptance. Do not claim live acceptance from automated tests.

## Execution
1. Add tested stable storage and one-time atomic migration; wire mod and managed bridge to it.
2. Build/test clean release and Workshop payloads, manifest and preview; verify bundled bridge and APWorld.
3. Update player README/setup, release notes and Workshop instructions.
4. Fresh branch review; fix important findings; publish downloadable GitHub alpha.
5. Upload private Workshop candidate using explicit private visibility; test subscription/update if Steam session permits. Record any external blocker precisely.

Review focus: interrupted/conflicting migrations, old plugin folders, duplicate delivery after update, active-file locks, bridge environment inheritance, private data packaging, Workshop visibility and actual loader discovery.
