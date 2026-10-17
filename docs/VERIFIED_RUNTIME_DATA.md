# Verified Runtime Data

## Current stable runtime — 1.0.7

Accepted 2026-10-17: the user tested the exact replacement DLL without calibration,
confirmed correct behavior/feel and explicitly approved stable release. The normal
loader log identifies 1.0.7, no Bite Countdown disable message was found, and the
installed digest matches the accepted CI artifact. Production retains only the
ordinary fatal-disable diagnostic and has no per-frame or separate-file logging.

- frozen runtime source/tag: `2f513ebc9b527910645db4535372c7a4269142b5` / `v1.0.7`;
- clean Release CI: `39026965766`, artifact `12098613942`;
- DLL: `BiteCountdown.dll`, assembly/file version `1.0.7.0`;
- SHA-256: `a2021039c48e629824c4cbb50088e1ccff39ced7d32e8d3b19f9bf52f930c8b9`;
- main-wait endpoint **0.26**, then same-speed contraction and linear alpha fade
  over **0.40 game seconds** at native bite entry;
- no F3–F7 controls, IMGUI, key polling or calibration config binding;
- native gameplay timing, hook window, alignment, pre-bite opacity and lighting unchanged.

Stable publication uses the exact accepted artifact, not a rebuild. Documentation
or workflow commits on main may follow the frozen runtime source.

### Final calibration and implementation evidence

On 2026-10-17 the user confirmed the combined motion now feels correct and asked
to freeze the saved values and remove calibration. Config from the exercised
1.0.6 build contains `EndSizeStep=13` and `FadeDurationStep=8`: endpoint **0.26**,
fade **0.40 game seconds**. The loader log identifies 1.0.6 and its installed DLL
digest matches that immutable candidate. This supersedes the earlier size-only
0.22 acceptance as the current adopted endpoint/fade pair.

**READY evidence gate:** replace live config accessors in `ApplyRingVisual` and
`BeginBiteFade` with the accepted constants, then remove the calibration-only
ConfigEntry fields/bindings, input polling, IMGUI panel and formatting helpers.
Keep 1.0.6's same fade coroutine, initial-wait storage, bite-entry parameter
snapshot and last renderer writes. No lifecycle/stop, gameplay timer/input,
anchor, initial scale, pre-bite alpha or ambient-light change is included.
Source review plus exact-source clean Release CI and installed hash/version
verification are required. The chosen pair is user-accepted; the replacement
binary's loading, feel and removal of calibration were subsequently accepted as recorded above.

1.0.7 hardcodes 0.26 / 0.40 s. Back up and remove the dedicated config containing
only TemporaryCalibration during installation. That backup/removal was completed;
the user subsequently approved stable 1.0.7 after testing the replacement DLL.

## Historical calibration evidence

The following sections preserve decisions and gates at their original candidate
boundaries. Earlier values and pending states are historical, not current defaults.

## Combined live calibration — 1.0.6 test candidate

The user reports the early-completion feeling returned with the 1.0.5 fade and
requests retuning final size together with fade duration. 0.22 / 0.15 s were the
starting values, not acceptance of the combined motion. The resulting acceptance
and final 0.26 / 0.40 s pair are recorded above.

Adopted controls: F3 toggles a read-only panel; while open F4/F5 change end scale
by 0.02 in 0.08–0.60, F6/F7 change fade duration by 0.05 game seconds in 0–0.50.
Defaults are size step 11 (0.22), fade step 3 (0.15 s). Both bounded integer steps
persist in BepInEx config under TemporaryCalibration. No mouse/game input is used.

**READY evidence gate:** reuse the previously exercised 0.4.0 keyboard/IMGUI
calibration pattern and the 1.0.5 visual coroutine owner. Main-wait
`ApplyRingVisual` reads the live endpoint. Store the native initial wait rather
than a stale shrink speed, then snapshot endpoint, matching shrink speed and fade
duration at native bite entry. These snapshots own the fade's last renderer writes;
edits during the tail apply to the next bite. Zero duration calls `StopRing` before
coroutine creation/division. Nonzero fade, cancellation, repeated-state guard,
generation invalidation, anchor/lighting and gameplay isolation remain unchanged.
No new game hook or native state/timer write is needed.

Verification: clean exact-source CI; review zero/max duration, endpoint bounds,
snapshot consistency and cleanup paths. Runtime tuning must establish the final
pair of values. After acceptance remove both config bindings, UI and key polling,
then freeze the accepted constants. This candidate is not stable publication.

## Bite fade — 1.0.5 candidate

Adopted for a test candidate on 2026-10-17: keep the calibrated main-wait endpoint
0.22, then continue contraction at the same scale-per-game-second speed while
linearly fading alpha to zero over 0.15 game seconds. This is a visual finish,
not a measured correction to native bite timing or synchronization to a clip event.

**READY evidence gate:** the existing `ChangeStatePostfix` observes the resulting
`WaitingForPulling` state and currently calls `StopRing`; `AnimateRing` separately
stops on exit from `WaitingForBite`. Both stop paths must be coordinated: stop the
wait coroutine before starting the fade, retaining one tracked visual coroutine
and the existing generation token. `ApplyRingVisual` owns the main-wait scale and
colour; the new fade owns those same renderer fields only after that handoff.
Speed derives from `(RingStartScale - RingEndScale) / initialWait`, so no second
gameplay countdown is introduced. The last visual state at the native bite is the
existing endpoint 0.22/alpha 0.58. Scale is clamped nonnegative during the fade.

Scope: only the mod-owned ring during the first 0.15 seconds of
`WaitingForPulling`. All other native states, new-wait re-arm, destruction and
failure call `StopRing`, canceling any tail and invalidating its generation.
Repeated notifications of the same bite must not restart the fade. The fade uses
Unity scaled time, matching the native countdown's deltaTime. Native wait/hook
duration, input, fish data, anchor, pre-bite alpha and lighting remain unchanged.

Verification: clean exact-source Release CI; review both stop paths, single
coroutine ownership, repeat-state guard and no negative scale. Installed-runtime
acceptance must judge the fade on normal casts plus immediate cancellation/catch
and one missed-hook re-arm. No exact clip/audio synchronization is claimed.
1.0.4 remains the previous immutable candidate; stable baseline is still 1.0.3.

## Final-size calibration — accepted value, 1.0.4 candidate

User/player feedback reports apparent completion before the native bite: the ring
reaches the visible bobber before reaching its very small accepted final scale.
Source confirms the ring renders behind the bobber. This supports, but does not
yet runtime-prove, occlusion as the explanation of the perceived early signal.

Adopted direction (2026-10-16): tune the final ring scale rather than introduce a
fixed time offset. A spatial endpoint remains independent of native wait length.
Keep the native timer, lifecycle, anchor, initial size, alpha and lighting intact.

**READY evidence gate:** `ApplyRingVisual` is the sole writer of ring local scale;
its only inputs come from initial display and `AnimateRing`'s read-only native
remaining/initial wait ratio. Only final scale changes. The new research-only
plugin `Update` reads F3/F4/F5, and `OnGUI` draws a read-only panel; neither patches
host Update nor consumes fishing input. Existing state-exit cleanup and missed-hook
re-arm remain unchanged. Clean compilation and source-diff review precede handoff;
real-game acceptance must confirm F3 UI, live adjustment, short/long waits and both
facings. No perceptual acceptance is claimed by compilation.

Research controls: F3 shows/hides the panel; while visible F4 decreases and F5
increases final scale by 0.02. Range is 0.08–0.60, initial value 0.08. The bounded
BepInEx config entry persists the value between launches; the panel shows the
resulting scale. No mouse controls, time changes or game-save writes are added.
On 2026-10-17 the user accepted the perceived timing at the saved setting
`EndSizeStep = 11`, corresponding to scale **0.22**, and requested freezing that
value and removing calibration. The local loader log confirms 0.4.0 was loaded;
the installed DLL digest matches the immutable 0.4.0 candidate record.

**READY for 1.0.4:** replace only the stable renderer's final scale constant with
0.22 and remove the research-only ConfigEntry, F3/F4/F5, Update and OnGUI code.
`ApplyRingVisual` still owns the last write to localScale; initialization and the
native-wait coroutine remain its only callers. Native state, stop/re-arm, anchor,
initial size, alpha and lighting remain identical to 1.0.3. Verify the complete
runtime-source diff against 1.0.3 contains only endpoint and version changes,
then clean-build the exact candidate and check artifact identity. The user's
0.4.0 endpoint acceptance supports the selected value; loading the replacement
binary and absence of the temporary panel are not yet runtime-verified.

Known visual limitation, user-observed: the ring disappears on the native state
transition before the visible bobber dip/sound, estimated by the user as about
0.1–0.2 seconds. At 0.22 a small gap remains when it disappears, which the user
perceives as an implicit continuation into the dip. This is accepted for perceived
timing but still looks abrupt. The delay is not instrumentally measured; a native
animation/audio trigger has not been identified. 1.0.4 preserved the stop boundary.
The subsequently adopted 1.0.5 test above adds a bounded aesthetic fade rather than
treating the user's estimate as an exact native animation delay.

1.0.4 freezes 0.22 and removes the panel, hotkeys and temporary config binding.
An existing `[TemporaryCalibration]` config entry is ignored by that candidate.
Stable accepted baseline remains 1.0.3 pending candidate acceptance/promotion.

Target: **Graveyard Keeper 1.407**.

Verified Assembly-CSharp identity used during research:

- version: `11.0.0.0`;
- MVID: `6f50b8e7-156b-49ac-bbe8-7505894b2364`.

## Native countdown ownership

Current `FishingGUI.ChangeState(WaitingForBite)` resolves the selected fish and stores the native wait in `_waiting_for_bite_delay`.

`FishingGUI.UpdateWaitingForBite()` does not consume that timer while `can_take_out == false`. Once `can_take_out == true`, vanilla subtracts `Time.deltaTime` until the timer reaches zero and transitions to `WaitingForPulling`.

Runtime research verified that `FishingThrowingAnim.OnStateExit` is the initial-cast transition that flips `can_take_out` from false to true without replacing the resolved wait.

## Missed-hook lifecycle

Runtime testing verified that a missed hook can make vanilla transition directly from `WaitingForPulling` back to `WaitingForBite` without another throwing animation. Vanilla resolves another fish/wait and may repeat this cycle until the player ends fishing.

Bite Countdown therefore re-arms from the resulting native `WaitingForBite` state only when `can_take_out == true`.

## Bobber anchor

Runtime probing at all three tested cast distances verified that cast distance is already represented by the native bobber transform.

The visual float is offset inside that transform by the current bobber sprite geometry. Production therefore derives the anchor from the current waiting sprite's tight mesh rather than using a location/distance table.

Accepted initial runtime visual calibration (right-facing reference):

- screen-relative offset X: **+2 game pixels**;
- screen-relative offset Y: **-2 game pixels**;
- start scale: **1.55**;
- end scale: **0.08**;
- alpha at full wait: **0.34**;
- alpha near bite: **0.58**.

### Left/right orientation correction research

Cross-spot Anchor Audit Probe 0.3.0 sampled all four fishing spots. Sea and waterfall were right-facing; village lake and swamp were left-facing. All sampled waits used the same bobber waiting sprite, PPU, tight-mesh geometry and no SpriteRenderer flip. The left-facing spots shared the same calculated 1.0.1 anchor formula as each other.

Pixel Snap Prototype 0.3.1 compared the existing position against nearest integer- and half-screen-pixel X snapping. The candidate shifts were all below half of one physical screen pixel and produced no visible correction. Runtime feedback instead showed a consistent pattern: right-facing fishing remained correctly centered, while both left-facing spots were visibly shifted to the right.

Source inspection explains that pattern. Version 1.0.1 converts the calibrated X correction through `bobber.lossyScale`, which forces the residual +2-pixel correction to remain screen-right on both facings. The calibration was performed on a right-facing reference, where that screen-right +2 correction corresponds to **-2 pixels in bobber-sprite local X**. The native player/bobber transform should own the left/right mirroring.

The 1.0.2 candidate therefore stores the residual correction as sprite-local **X=-2, Y=-2 game pixels** (respecting SpriteRenderer flip if it is ever used) and lets the native transform mirror it. This preserves the already-correct right-facing placement while mirroring the correction for left-facing fishing.

## Gameplay isolation

Accepted runtime/source evidence confirms the countdown implementation reads native state/timing and manages only its own visual object.

It does not write:

- fishing state;
- wait duration;
- hook duration;
- fish selection or quality;
- bait/lure data;
- energy;
- catch amount;
- achievement/quest state;
- save data.

Stable production builds create no separate Bite Countdown diagnostic log file.


## Historical stable release 1.0.1

Stable runtime source is tagged `v1.0.1` at commit `d7d63dc6c69109e2b0011f5eb77c2a5d206e9a67`.

Build identity:

- clean Release run: `35543919057`;
- Actions artifact: `10616015471`;
- installed DLL: `BiteCountdown.dll`;
- plugin GUID: `nikich.bitecountdown`;
- SHA-256: `a84b47e72e8f5f6dd3f7028b45c390a57bed5a236ead0951fc5b4c94f52d581c`;
- GitHub Release: `v1.0.1`.

The runtime behavior is the accepted countdown implementation. The 1.0.1 production delta is product identity only: Bite Countdown naming, fresh GUID/assembly/DLL identity, release metadata, and removal of the temporary 1.0.0 stable release surface.


## 1.0.2 candidate orientation fix

The 1.0.2 candidate changes only residual visual-anchor coordinate semantics. Fishing lifecycle, timer ownership, ring scale/alpha, missed-hook re-arm behavior and gameplay isolation are unchanged from 1.0.1.

It was handed as an immutable candidate and then superseded before acceptance by 1.0.3, which carries the same orientation fix plus the requested brightness work.


## Ambient-aware ring brightness — accepted 1.0.3

Keeper's Lantern 1.0.12 source verifies that its accepted outdoor-night **Night Brightness** value is an ambient scale of **0.70**. Its world-lighting pass applies the final transformed colour to Unity's global `RenderSettings.ambientLight` in `LateUpdate`.

That gives Bite Countdown a generic host-level signal instead of a mod-specific integration:

- with vanilla lighting, `RenderSettings.ambientLight` contains the vanilla final ambient colour;
- with Keeper's Lantern, the same value already contains its final darker/tinted ambient result;
- Bite Countdown does not reference Keeper's Lantern assemblies, GUIDs, config, or state.

The accepted 1.0.3 implementation deliberately skipped a separate runtime-measurement research step at the user's request and uses a bounded visual rule:

- base ring RGB: **0.60 / 0.82 / 0.92** (reduced from 0.72 / 0.93 / 1.00);
- existing alpha curve remains **0.34 -> 0.58**;
- once per native bite wait, after one `WaitForEndOfFrame`, read `RenderSettings.ambientLight.grayscale`;
- compute RGB multiplier with `Lerp(0.62, 1.00, ambientGrayscale)`;
- apply that multiplier only to ring RGB.

The end-of-frame sample is intentional: it observes the final ambient value after ordinary `LateUpdate` lighting work such as Keeper's Lantern's ambient pass. The value is sampled once per wait rather than polled continuously; a missed-hook re-arm naturally samples again.

This is presentation-only. It does not write global lighting state, does not change alpha/timing/gameplay, and does not require a lighting mod.


### 1.0.3 runtime acceptance

The exact 1.0.3 candidate from source `2b5c3cfec638098328eaf5707d0e79effb9a1bf6` was runtime-tested on Graveyard Keeper 1.407 with Keeper's Lantern 1.0.12 active.

User acceptance on 2026-09-21 confirmed:
- daytime ring brightness is good;
- during the darker Keeper's Lantern night the ring is no longer excessively bright;
- ring centering is correct;
- the candidate is approved for stable promotion.

The fishing lifecycle, timer ownership, missed-hook re-arm path and gameplay isolation are unchanged from the previously accepted implementation.

Accepted build identity:
- clean Release run: `35623960905`;
- Actions artifact: `10651360220`;
- frozen candidate ref: `candidate/1.0.3`;
- installed DLL: `BiteCountdown.dll`;
- plugin GUID: `nikich.bitecountdown`;
- SHA-256: `77be2d8e0a03197df8e9b82ffb4b6f8971c137f585f44fba50cafd47dc575f02`.


## Historical stable release 1.0.3

Stable accepted runtime source is frozen at `candidate/1.0.3` and tagged `v1.0.3` at commit `2b5c3cfec638098328eaf5707d0e79effb9a1bf6`.

Build and release identity:

- clean Release run: `35623960905`;
- Actions artifact: `10651360220`;
- installed DLL: `BiteCountdown.dll`;
- plugin GUID: `nikich.bitecountdown`;
- SHA-256: `77be2d8e0a03197df8e9b82ffb4b6f8971c137f585f44fba50cafd47dc575f02`;
- stable publication workflow run: `35625713142` (successful retry / attempt 2 after a transient GitHub HTTP 500 on the first publication attempt);
- GitHub Release ID: `393117260`;
- GitHub Release: `v1.0.3`;
- release asset `BiteCountdown.dll` digest matches the accepted candidate SHA-256 exactly.

The stable `main` branch may contain documentation/workflow commits after the accepted runtime source, but `src/Plugin.cs` and `BiteCountdown.csproj` were verified by the publication gate to match the accepted candidate before release.
