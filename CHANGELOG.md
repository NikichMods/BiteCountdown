# Changelog

## 1.0.8 — candidate

- Uses a constant 25% opacity throughout the bite wait.
- Preserves final size and the 0.40-second post-bite contraction/fade, starting from the new opacity.

## 1.0.7 — 2026-10-17

- Froze the user-calibrated endpoint at 0.26 and fade duration at 0.40 seconds.
- Removed the temporary F3–F7 controls, panel and configuration bindings.
- Preserved the tested contraction/fade algorithm and native fishing mechanics.

## 1.0.6 — calibration candidate

- Restored temporary F3 calibration: F4/F5 adjust final size, F6/F7 adjust fade duration in 0.05-second steps.
- Starts at 0.22 / 0.15 seconds and saves both values between launches.
- Supports zero fade for immediate disappearance; snapshots fade parameters at each bite.

## 1.0.5 — candidate

- Continues ring contraction at the same speed while fading out over 0.15 seconds after the native bite begins.
- Keeps the calibrated main countdown endpoint at 0.22.
- Cancels the visual tail on a catch, cancellation or new bite wait; fishing mechanics remain unchanged.

## 1.0.4 — candidate

- Increased final ring scale to the player-calibrated 0.22 so the visible contraction better matches the perceived bite timing.
- Removed temporary F3/F4/F5 calibration controls and configuration binding.
- Preserved native bite timing, ring disappearance, alignment, opacity and ambient brightness.

## 1.0.3

- Fixed horizontal countdown-ring alignment when fishing to the left.
- Reduced the ring's base blue RGB brightness while preserving the accepted alpha curve.
- Added lightweight ambient-aware RGB scaling so the ring is calmer in dark scenes and remains readable in brighter scenes.
- Uses the game's final ambient light rather than a direct Keeper's Lantern integration, so the behavior also works with vanilla lighting.
- No fishing balance, timing, catch, bait, energy, save, or minigame behavior changes.

## 1.0.1

- First stable release under the final **Bite Countdown** identity.
- Added a subtle shrinking ring around the fishing bobber.
- The ring follows the native fish wait and reaches the bobber at the bite.
- Missed hooks correctly re-arm the ring when vanilla starts another wait.
- Uses the accepted visual alignment and opacity.
- Does not change fishing balance, catches, bait, energy, or the reeling minigame.
- Uses clean product identity: `Bite Countdown`, `nikich.bitecountdown`, and `BiteCountdown.dll`.
- Creates no separate diagnostic log file.
