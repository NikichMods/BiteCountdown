# Fishing Bite Countdown 1.0.8

The bite countdown ring is now subtler and more consistent during the wait.

- Keeps the ring at a constant 25% opacity instead of the previous rising 34%–58% opacity.
- Continues the same smooth post-bite contraction and linear fade from 25% to zero over 0.40 seconds.
- Preserves the accepted endpoint, ambient-light adaptation, ring placement and native fishing behavior.
- Does not change bite timing, the hook window, fish selection, bait, energy, saves or the reeling minigame.

Target: Graveyard Keeper 1.407. Install `BiteCountdown.dll` in `BepInEx/plugins`, replacing the previous copy.
