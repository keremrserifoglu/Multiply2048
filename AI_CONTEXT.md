# AI_CONTEXT.md

This document exists to help AI assistants safely understand and modify the Multiply 2048 project.

Before editing any code in this repository, always read the following files:

PROJECT_CONTEXT.md  
AI_CONTEXT.md  

These documents define the gameplay rules, architecture, and design constraints of the project.

AI systems must respect these constraints when modifying the code.

---

# Project Type

Multiply 2048 is a **grid-based drag-swap merge puzzle game** built with Unity.

Important:  
This game is **NOT a classic 2048 slide game**.

Key difference:

Classic 2048  
→ entire board slides

Multiply 2048  
→ player swaps two adjacent tiles

Do not implement slide mechanics.

---

# Core Gameplay Rule

Players swap adjacent tiles.

A swap is only valid if it creates a merge group.

If no merge group is created:

the swap is reverted.

The board never moves as a whole.

---

# Merge System

Merges are **group based**, not pair based.

Example group:

2 2 2

Group size = 3

Merge formula:

newValue = originalValue << (groupSize - 1)

Examples:

2 + 2 → 4  
2 + 2 + 2 → 8  
2 + 2 + 2 + 2 → 16

Each additional tile doubles the result again.

AI must not change this formula unless explicitly instructed.

---

# Merge Center Rule

Each merge group selects a **center tile**.

Behavior:

non-center tiles  
→ destroyed

center tile  
→ receives merged value

merge VFX  
→ spawned

score  
→ awarded

The center tile remains unless the 2048 milestone rule triggers.

---

# 2048 Milestone Rule

If a merge produces a value greater than or equal to 2048:

special effects trigger.

Effects:

2048 SFX  
firework VFX  
palette progression

The tile is then removed from the board.

Important rule:

2048 tiles do not remain on the board.

They are milestone events.

Do not change this behavior.

---

# Resolve Loop

After every valid swap the board runs a resolve loop.

Order:

FindGroups  
MergeGroups  
ApplyGravity  
RefillBoard

Repeat until no merges remain.

AI must preserve this order.

Breaking this loop will break gameplay.

---

# Board Ownership

Board simulation is controlled by:

Assets/Scripts/BoardController.cs

This script is responsible for:

board grid  
swap logic  
merge detection  
merge resolution  
gravity  
refill  
move validation  
game over detection

AI should not move gameplay logic outside this script unless explicitly requested.

---

# Game Flow Ownership

Game flow is controlled by:

Assets/Scripts/GameManager.cs

Responsibilities:

menu state  
score management  
mode switching  
credits  
save system  
game over UI  
rewarded ad continue system

GameManager should not contain gameplay simulation logic.

---

# Tile Object

Tile script:

Assets/Scripts/CandyTile.cs

Responsibilities:

tile value  
tile visuals  
text display  
tile movement  
theme color updates

CandyTile must remain a lightweight visual and data object.

Do not move board logic into this class.

---

# Game Modes

Two game modes exist.

Solo Mode

single score  
undo enabled  
shuffle enabled  
persistent board state

Versus Mode

two players  
turn based scoring  
shuffle disabled  
separate save state

AI must not mix Solo and Versus save data.

---

# Undo System

Undo uses board snapshots.

Snapshots contain:

board layout  
tile values  
player scores

Undo restores the previous snapshot.

Undo is limited to **one step per move**.

Undo is not a full move history system.

---

# Shuffle System

Shuffle exists only in Solo mode.

Behavior:

randomizes tile values currently on the board

Board must be full.

After shuffle the resolve loop runs again.

Shuffle uses shuffle credits.

---

# Credit System

Two credit types exist.

Undo credits  
Shuffle credits

Credits regenerate over time using UTC timestamps.

PlayerPrefs keys:

UNDO_CREDITS  
SHUFFLE_CREDITS  
CREDITS_LAST_GRANT_UTC

AI should not replace this system with server logic unless explicitly requested.

---

# Persistence System

Active board state is saved using PlayerPrefs JSON.

Keys:

SOLO_BOARD_STATE_JSON  
VERSUS_BOARD_STATE_JSON

Saved data includes:

board size  
tile values  
scores  
active player

Save events occur:

after scoring  
after shuffle  
after undo  
on pause  
on quit

---

# Ads System

Ads are handled by:

Assets/Scripts/MobileAdsManager.cs

Features:

banner ads  
rewarded ads

Rewarded ads are used for:

continue after game over.

Function:

ShowRewarded(Action<bool> onCompleted)

Callback returns true only if the user earns the reward.

AI must preserve this contract.

---

# Theme System

Theme controller:

Assets/Scripts/Theme/ThemeManager.cs

Responsibilities:

tile palette selection  
background color  
palette progression

Palette progression occurs when a value >= 2048 is created.

ThemeManager persists using DontDestroyOnLoad.

---

# Audio System

Audio controller:

Assets/Scripts/AudioManager.cs

Handles:

sound playback  
layered sound effects  
audio toggle settings

Sound types are defined in:

Assets/Scripts/SfxLibrary.cs

---

# Safe Area System

Safe area handling:

Assets/Scripts/SafeAreaFitter.cs

Purpose:

apply Screen.safeArea to UI layout  
support bottom inset for banner ads

AI should not remove safe area logic.

---

# Visual Effects

Merge VFX prefabs:

MergeGhost  
MergeSparkle  
MergeFirework

These are purely visual and do not affect gameplay logic.

---

# Critical Scripts

If gameplay needs modification, start with:

1. BoardController.cs
2. GameManager.cs
3. CandyTile.cs

Other scripts are secondary.

---

# Important Constraints

AI assistants must respect the following rules.

Do not convert the game into slide-based 2048.

Do not remove group merges.

Do not change the merge formula.

Do not allow 2048 tiles to remain on the board.

Do not move board simulation out of BoardController.

Do not move UI flow into BoardController.

---

# AI Editing Guidelines

When implementing new features:

1. Check whether the feature belongs to BoardController or GameManager.
2. Avoid breaking the resolve loop.
3. Preserve merge math.
4. Maintain compatibility with Solo and Versus modes.

Examples of valid AI tasks:

add powerup tiles  
add combo scoring  
add new VFX  
add new themes  
optimize board scanning  
add new UI features

Examples of risky changes:

changing merge math  
changing resolve order  
changing board ownership

These should only be done with explicit instructions.

---

# Purpose

This document ensures AI tools understand the game rules and architecture before modifying the code.

Always read this file before editing gameplay systems.