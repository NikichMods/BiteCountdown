# Fishing Bite Countdown — Project Working Contract

This repository follows the canonical global rules in `NikichMods/DevRules`.

Before substantive technical work, read the current `ENGINEERING_RULES.md`, `CI_POLICY.md`, `GIT_WORKFLOW.md`, `PROJECT_BOOTSTRAP.md`, and `RUNTIME_TEST_HARNESS.md` when runtime evidence is relevant.

## Project identity

- Project: **Fishing Bite Countdown**
- Former public name: **Bite Countdown**; renamed on **2026-10-23** to make the fishing scope explicit and searchable.
- Repository: `NikichMods/BiteCountdown`
- Game: **Graveyard Keeper 1**
- Target version: **1.407**
- Mod stack: **BepInEx / Harmony**
- BepInEx GUID: `nikich.bitecountdown`
- Stable installed DLL: `BiteCountdown.dll`
- Current accepted stable runtime baseline: **1.0.8**

The public-name change is documentation/release metadata only. Keep the repository slug, namespace, GUID, assembly name and installed DLL identity stable. The immutable accepted 1.0.8 binary still reports `Bite Countdown` as its BepInEx plugin name and in diagnostics; that is expected under the public name **Fishing Bite Countdown**. Do not rebuild or replace an accepted binary solely to update its display name.

Do not reintroduce the earlier rod-balance product concept into runtime or release naming.

## Shared Graveyard Keeper research

Reusable Graveyard Keeper 1.407 host/runtime facts are shared through `NikichMods/GraveyardKeeperResearch`.

Before starting fresh host-internals research, check:
- `NikichMods/GraveyardKeeperResearch/docs/RESEARCH_INDEX.md`;
- the relevant canonical shared document, especially `docs/FISHING_RUNTIME.md` for fishing lifecycle/geometry questions;
- accepted local project evidence/history if the fact is Fishing Bite Countdown-specific.

Keep cross-project host/runtime facts in the shared research repository. Keep Fishing Bite Countdown product behavior, visual decisions, acceptance state, build identity, and release evidence in this repository.

## Product scope

Fishing Bite Countdown is a visual-only fishing quality-of-life mod.

It adds a subtle in-world ring around the bobber during the native bite-wait interval. The ring contracts across the already-resolved vanilla wait, then briefly continues shrinking and fades as the native bite begins.

The mod must not change:

- fish selection, quality, rarity, spots, casting distance, time-of-day logic, bait/lure semantics, or rod-specific fish availability;
- bite wait duration or hook reaction duration;
- energy use;
- caught item amount;
- the reeling minigame;
- achievements, quest semantics, reward handling, or save data.

## Architecture constraints

Follow the global host-native-first gate.

- Vanilla owns fish selection and `_waiting_for_bite_delay`.
- Initial countdown starts after verified `FishingThrowingAnim.OnStateExit` enables `can_take_out`.
- After a missed hook, vanilla can return directly to `WaitingForBite`; re-arm only when the resulting state is `WaitingForBite` and `can_take_out == true`.
- The visual coroutine reads the native timer. It never writes or replaces the gameplay timer.
- Anchor to the current bobber sprite tight mesh and apply the residual correction in bobber-sprite local pixels so native left/right mirroring also mirrors that correction.
- No permanent Harmony `Update()` patch, broad polling, or parallel fishing state machine.
- Unsupported runtime states fail closed by disabling only the countdown.

## Current accepted 1.0.8 visual behavior

- start scale: `1.55`;
- end scale at native bite entry: `0.26`;
- continue shrinking at the same speed and linearly fade alpha to zero over `0.40` game seconds after bite entry;
- alpha throughout the native bite wait: constant `0.25`;
- sprite-local visual correction: `X=-2, Y=-2` game pixels. This preserves the accepted right-facing placement and mirrors the horizontal correction for left-facing fishing;
- ring base RGB: `0.60, 0.82, 0.92`;
- after the bite starts, alpha fades linearly from `0.25` to zero over `0.40` game seconds;
- once per native wait, sample final `RenderSettings.ambientLight` at end-of-frame and scale RGB by a bounded factor `0.62 -> 1.00` based on ambient grayscale;
- brightness adaptation must have no direct dependency on Keeper's Lantern or any other lighting mod.

## Accepted 1.0.7 lifecycle

On 2026-10-17 the user accepted the combined 1.0.6 calibration result:
`EndSizeStep=13`, `FadeDurationStep=8`: final scale **0.26**, fade **0.40 s**.
1.0.7 freezes this pair and removes F3–F7, IMGUI, input polling and temporary
config bindings. Preserve 1.0.6's main contraction, same-speed visual continuation,
linear alpha fade, bite-entry snapshot and immediate cleanup on catch/cancel/new
wait/destruction/failure. Native gameplay timing, anchor, pre-bite opacity and
lighting remain unchanged. No clip/audio synchronization is claimed. On 2026-10-17
the user runtime-tested the exact 1.0.7 DLL and explicitly accepted stable release.
Frozen runtime source: `2f513ebc9b527910645db4535372c7a4269142b5` / `v1.0.7`.
Publish the exact accepted DLL; never substitute a later rebuild under this version.

## 1.0.8 acceptance

On 2026-10-27 the user confirmed that the exact installed 1.0.8 candidate works and approved its stable release. The only production delta from 1.0.7 is constant wait-phase alpha `0.25` in place of the prior `0.34 -> 0.58` ramp; the fade continues from `0.25`, with native mechanics, position, size and timing unchanged. Frozen candidate source: `7ad720c272b5a400eb83cffef80661c78ed8b952`. CI run `39517013992`, artifact `12307089744`, accepted DLL SHA-256 `b583e92c76082799fa5323545adc737aab098542de8694ac0e2bfcf15a68bc76`. Publish that exact artifact, never a rebuild.

## Diagnostics

Stable releases must not create a separate diagnostic log file. Production may emit one normal BepInEx error only when a fatal compatibility/runtime failure disables the countdown.

## Save / compatibility

The mod writes no custom save data. Removing `BiteCountdown.dll` restores vanilla behavior without migration.

## Git / acceptance

- `main`: stable accepted state.
- Research: `research/*`.
- Build-bearing development: `dev/X.Y.Z`.
- Numbered binaries are immutable once handed/published.
- Stable installed DLL: `BiteCountdown.dll`.
- GitHub Releases are the stable binary surface.

## Shared Graveyard Keeper research

Cross-project Graveyard Keeper 1.407 host/runtime research is centralized in `NikichMods/GraveyardKeeperResearch`.

Before starting a fresh investigation into vanilla/game-engine/UI/NGUI/data/lifecycle behavior:

1. read this repository's own canonical verified-data / architecture docs first;
2. consult `NikichMods/GraveyardKeeperResearch/docs/RESEARCH_INDEX.md` and the linked shared knowledge documents;
3. search accepted local/shared test evidence and relevant history if the result has not yet been promoted;
4. perform new static/runtime research or a probe only if the question remains open.

Project-specific mechanics, product/UX decisions, release state, and build acceptance remain canonical in this repository. Reusable host/runtime facts that can serve multiple Graveyard Keeper mods should be promoted back into the shared research repository after acceptance rather than left only in chat, commit history, or a test log.

