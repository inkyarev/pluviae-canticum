# Pluviae Canticum

## Whatawhy
This is a fork of a RoR2 mod [OriginalSoundTrack](https://github.com/kylepaulsen/RoR2-Original-Sound-Track) by [Kyle Paulsen](https://github.com/kylepaulsen). It, just as the original, allows you to play any tracks in place of the vanilla soundtrack.

### The main changes are:
- Teleporter tracks
  - Runtime filter for when outside the teleporter radius.
  - Play any track after the teleporter finished charging.
- Boss tracks
  - Support for Voidling and False Son
  - Can now set tracks to individual phases (including on-death)
  - Adjustable fade-in, fade-out and silence-before-track (also applies to regular tracks, but mostly used for boss tracks)
- Risk of Options support for changing these options:
  - Music volume
  - Whether the music volume should be affected by the master volume
- Improved config readability by switching from .xml to .toml
- Moved all processing to its own thread, so that it doesn't affect game performance.

I made this because the original mod was made before any DLCs and therefore was outdated af. While just porting it to SotS my perfectionism made me add all other features.

## Setup and use

Install the mod, then open settings.toml. It should contain info needed for using this mod.

## Other Info

This mod uses NAudio.dll https://github.com/naudio/NAudio for loading, playing and processing audio at runtime.
This is done to make it very easy for players to supply their own tracks. The downside is that NAudio doesn't replace the in game audio, it just plays the audio on top of it. To get around this, this mod sets your in game
music volume to 0. It tries to set it back to where it was when the game closes, but i haven't tested whether this actually works.
