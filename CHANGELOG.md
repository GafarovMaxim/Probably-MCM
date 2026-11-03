# Changelog

## 1.0.1 — 2026-11-03

### Fixed

- Native mod settings use the game's save implementation instead of being overwritten by MCM's Unity JSON serializer.
- Settings registered through the MCM API restore saved values when their entries register, including settings registered later.
- Native enum dropdown choices are converted to the actual enum type; displayed values follow native validation and clamping.
- Empty strings, double values, numeric ranges and unknown configuration keys survive save/reload.
- Mixed native/MCM-only settings retain custom values across subsequent unrelated native saves.
- MCM-owned configuration writes use the game's existing Newtonsoft.Json library with atomic replacement and bounded backups. Malformed JSON is preserved and reported.

### Validation

- Release build: no warnings or errors.
- Offline restart regressions: 141 assertions passed across separate write/read processes, using the actual modified MCM code and isolated engine adapters.
- Original game mod scanner: no hard flags; existing `files` and `reflection-external` capabilities suffice.
- Live Unity restart testing remains to be confirmed by players; the offline tests do not exercise Unity UI or power loss.

### Updating

Back up the existing mod folder and `ModConfigs` before replacing files. Extract `Mods/ProbablyMCM` into the game's `Mods` directory, then restart the game manually.

Configuration files already reduced to `{}` contain no previous settings. Those values must be configured again after updating; this fix does not reconstruct erased data.
