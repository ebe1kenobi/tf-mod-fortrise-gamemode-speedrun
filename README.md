# SpeedRun

**Speed Run** game mode: levels are stitched together and the screen scrolls
non-stop. Keep up, grab the chests and reach the goal portal; stragglers fall off
screen and die.

A mod for **FortRise 5** (>= 5.3.3). The FortRise 4 version (`tf-mod-fortrise-gamemode-speedrun`) is no longer maintained: fixes and new features only land in this repository.

## Installation

1. Install FortRise 5 and start the game through `FortRise.exe`.
2. Copy `release/speedrun` (or the shipped folder) into `<TowerFall>/FortRise/Mods/`.

Settings are under **Options > Mods > SpeedRun**.
Data and log files live in `<TowerFall>/FortRise/Saves/SpeedRun/` and `<TowerFall>/FortRise/Logs/`.

## Usage

Pick the **Speed Run** mode on the versus screen, then start the match.

> **Opening the popup**: on the versus screen, with the relevant mode selected,
> press **Y** (the "arrows" button on the controller) on the mode button. A hint is
> shown under the button. The popup locks the menu while it is open (no going back,
> no starting the match); **A** or **B** closes it.

The Speed Run popup tweaks the main settings without going through the options menu:

| Input | Effect |
|-------|--------|
| Up / Down | switch field |
| Left / Right | adjust the value |
| A or B | close |

It edits the module settings directly, so both screens always show the same values.
Some fields hide themselves when they no longer apply — the lap count only shows on
a square course with a goal portal, for instance.

Every change is written to disk immediately. FortRise only saves settings when
leaving the game's Options menu, so a value changed in the popup — or right before
quitting — used to be lost.

## Settings

Scrolling:

| Setting | Purpose |
|---------|---------|
| Speed Run speed (tenths of px/frame) | scrolling speed |
| Speed Run acceleration (+tenths px/frame) | gradual speed-up (0 = none) |
| Speed Run acceleration every (s) | how often that speed-up applies |
| Speed Run shape | horizontal strip or square loop course |
| Speed Run camera | auto-scrolling camera or one that follows the players |

### Playing wide

There is no wide-screen option here any more. Widening the window is
[WiderSet](https://gamebanana.com/mods/608884)'s job: turn its mode on and Speed Run
is played wide like everything else.

This mod used to resize the screen for its own rounds, which meant two mods owning
the same width - each recomputing the image centering over a value the other had just
changed, and the picture ending up off-centre. It reads the width now, it never sets
it.

Course:

| Setting | Purpose |
|---------|---------|
| Speed Run goal portal | goal portal, like the end of a co-op level |
| Speed Run laps before goal (square) | laps before the goal opens (square course) |
| Speed Run number of levels | how many levels are stitched together |
| Speed Run leave players behind | stragglers fall off screen and die |
| Speed Run offscreen death delay (s) | delay before that death |
| Speed Run same spawn (race) | everyone starts from the same spot |
| Speed Run intro zoom | wide shot then zoom in at the start |

Chests and rules:

| Setting | Purpose |
|---------|---------|
| Speed Run treasure count | number of chests |
| Speed Run treasure respawn (s) | chest respawn delay |
| SR treasure: ... | possible chest contents, one toggle per item |
| Speed Run disable arrows | no shooting |
| Speed Run disable head stomp | no killing by jumping on heads |

## Game mode icon

The mode has its own icon, at the size of the game's four (184x82) and in their
style - a silhouette in three shades of one colour, no black: a four-room tower with the screen roaming inside it.

It used to be borrowed from WARLORD's, a horned head with nothing to do with the mode. Two modes sharing
one picture cannot be told apart in the list.

The file is `ModFile/Content/Atlas/gamemode.png`.

## Build / deployment

| Script | Purpose |
|--------|---------|
| `script/release.bat` | build, then assemble into `release/` |
| `script/deploy.bat` | copy `release/` into the TowerFall `Mods` folder |
| `script/release_deploy.bat` | both, one after the other |

Paths (game folder, module name) are set in `script/config.bat`.
