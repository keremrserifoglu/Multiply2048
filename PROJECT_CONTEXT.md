# Multiply 2048 — Project Context

This document explains the architecture, gameplay rules, and main systems of the Multiply 2048 project so that developers or AI tools can safely understand and modify the project.

--------------------------------

PROJECT OVERVIEW

Project Name: Multiply 2048  
Engine: Unity  
Main Scene: Assets/Scenes/SampleScene.unity  
Genre: Grid-based merge puzzle

Multiply 2048 is a drag-swap tile puzzle game.

Unlike the original 2048:

• The board is always fully populated  
• Players swap adjacent tiles instead of sliding the whole board  
• Multiple tiles can merge together in groups  

After every valid move the board resolves merges, gravity, and refills until the board stabilizes.

--------------------------------

CORE GAMEPLAY LOOP

1. Player presses a tile
2. Player drags toward a neighboring tile
3. The game attempts a swap
4. If the swap creates a merge group → resolve
5. If not → swap is reverted

Resolve loop:

FindGroups  
MergeGroups  
ApplyGravity  
Refill  
Repeat until stable

If the board has no valid moves → Game Over.

--------------------------------

BOARD SYSTEM

Main controller:
Assets/Scripts/BoardController.cs

The board grid is stored as:

CandyTile[,]

Default board size:

8 x 8

Each tile contains:

• grid coordinates  
• numeric value  
• sprite renderer  
• text label  
• board reference

Tile prefab:

Assets/Scenes/Prefabs/CandyTile.prefab

--------------------------------

INPUT SYSTEM

Input is drag based.

Player action:

press tile  
drag direction  
check neighbor  
attempt swap  

Important parameter:

dragThresholdInCells

If the swap does not create a merge, the tiles animate back.

--------------------------------

MERGE SYSTEM

Merges are group based.

Merge value formula:

newValue = originalValue << (groupSize - 1)

Examples:

2 + 2 = 4  
2 + 2 + 2 = 8  
2 + 2 + 2 + 2 = 16  

Each additional tile doubles the value again.

--------------------------------

MERGE CENTER

Each merge group selects a center tile.

Behavior:

• non-center tiles are destroyed  
• center tile receives the new value  
• merge VFX is spawned  
• score is awarded

--------------------------------

2048 MILESTONE

If a merge creates a value >= 2048:

• special SFX plays  
• firework VFX spawns  
• the tile is removed  
• ThemeManager advances palette

Important:

2048 tiles do NOT stay on the board.  
They are milestone events.

--------------------------------

SCORING SYSTEM

Handled by:

Assets/Scripts/GameManager.cs

SOLO MODE

Merged value is doubled before being added.

Example:

mergedValue = 16  
scoreGain = 32

--------------------------------

VERSUS MODE

Two players exist:

player1Score  
player2Score

The scoring player is tracked by:

BoardController.ScoringPlayer

After each successful resolve the turn switches.

--------------------------------

GAME MODES

SOLO

• single score  
• undo available  
• shuffle available  
• board state saved  

VERSUS

• two scores  
• turn based  
• shuffle disabled  
• separate save state  

--------------------------------

GAMEMANAGER RESPONSIBILITIES

Assets/Scripts/GameManager.cs

Responsibilities:

• menu flow  
• score management  
• undo credits  
• shuffle credits  
• credit regeneration  
• board persistence  
• game over flow  
• rewarded ads

Singleton access:

GameManager.I

--------------------------------

BOARD PERSISTENCE

Board states are saved as JSON in PlayerPrefs.

Keys:

SOLO_BOARD_STATE_JSON  
VERSUS_BOARD_STATE_JSON  

Saved data includes:

• board dimensions  
• tile values  
• player scores  
• active player

Save events:

• after scoring  
• after undo  
• after shuffle  
• on pause  
• on quit

--------------------------------

UNDO SYSTEM

Undo uses board snapshots.

Flow:

export board state  
store snapshot  
restore snapshot on undo

Undo is limited to one step per move.

--------------------------------

SHUFFLE SYSTEM

Shuffle is available only in Solo mode.

Behavior:

• randomizes existing tile values  
• board must be full  
• resolve loop runs afterwards

Shuffle uses separate credits.

--------------------------------

CREDIT SYSTEM

Two credit types exist:

Undo credits  
Shuffle credits

Features:

• starting values  
• maximum cap  
• regeneration over time  

Credits regenerate using UTC timestamps.

PlayerPrefs keys:

UNDO_CREDITS  
SHUFFLE_CREDITS  
CREDITS_LAST_GRANT_UTC  

--------------------------------

GAME OVER FLOW

When the board has no valid moves:

BoardController calls GameManager.GameOver()

Flow:

1. snapshot board state
2. show rewarded ad offer
3. if player watches ad
   → board restored
   → gameplay resumes
4. otherwise
   → show game over UI
   → clear saved run

--------------------------------

THEME SYSTEM

Main script:

Assets/Scripts/Theme/ThemeManager.cs

Responsibilities:

• palette management  
• tile color selection  
• board background color  
• palette progression  

Palette data stored in:

TilePaletteDatabase

ThemeManager persists using DontDestroyOnLoad.

Palette progression occurs when value >= 2048 is created.

--------------------------------

AUDIO SYSTEM

Main script:

Assets/Scripts/AudioManager.cs

Responsibilities:

• SFX playback  
• layered sounds  
• SFX enable/disable  
• persistent settings  

Sound enum:

SfxLibrary.cs

Example sounds:

MergeCrack  
MergeBody  
Merge2048Sparkle  
Merge2048Air  
GameOverClose  
GameOverHope  

--------------------------------

VFX SYSTEM

Merge visual effects:

MergeGhost.prefab  
MergeSparkle.prefab  
MergeFirework.prefab  

Used for:

• tile removal  
• merge feedback  
• 2048 milestone celebration

--------------------------------

ADS SYSTEM

Managed by:

Assets/Scripts/MobileAdsManager.cs

Uses Google Mobile Ads.

Features:

• adaptive banner ads  
• rewarded ads  
• automatic reload  
• safe callbacks

Rewarded ad method:

ShowRewarded(Action<bool> onCompleted)

Callback result:

true  → reward granted  
false → reward not granted  

Used for continuing after Game Over.

--------------------------------

SAFE AREA HANDLING

Handled by:

Assets/Scripts/SafeAreaFitter.cs

Applies Screen.safeArea to UI.

Supports additional bottom inset for banner ads.

Updates automatically on:

• screen size change  
• orientation change  
• safe area change

--------------------------------

ADDITIONAL UI SCRIPTS

BackgroundController  
UIBackgroundController  
ColorThemeManager  
SettingsUIController  

These scripts handle visual and UI behavior and do not affect gameplay logic.

--------------------------------

IMPORTANT SCRIPTS

Primary scripts for gameplay modifications:

1. BoardController.cs
2. GameManager.cs
3. CandyTile.cs
4. ThemeManager.cs
5. AudioManager.cs
6. MobileAdsManager.cs
7. SafeAreaFitter.cs

--------------------------------

DESIGN RULES

When modifying this project always assume:

• the game is swap-based, not slide-based  
• merges can involve multiple tiles  
• merge results grow exponentially  
• 2048 tiles are milestone events  
• undo uses full board snapshots  
• shuffle exists only in Solo mode  
• Solo and Versus saves are separate  
• BoardController controls gameplay  
• GameManager controls UI and persistence  
• rewarded ads allow continuing after Game Over

--------------------------------

REPOSITORY STRUCTURE

Assets/
 ├ Scenes/
 │   ├ SampleScene.unity
 │   └ Prefabs/
 │       ├ CandyTile.prefab
 │       ├ MergeFirework.prefab
 │       ├ MergeGhost.prefab
 │       └ MergeSparkle.prefab
 │
 ├ Scripts/
 │   ├ BoardController.cs
 │   ├ CandyTile.cs
 │   ├ GameManager.cs
 │   ├ AudioManager.cs
 │   ├ MobileAdsManager.cs
 │   ├ SafeAreaFitter.cs
 │   ├ SettingsUIController.cs
 │   ├ BackgroundController.cs
 │   ├ UIBackgroundController.cs
 │   ├ ColorThemeManager.cs
 │   │
 │   └ Theme/
 │       ├ ThemeManager.cs
 │       └ TilePaletteDatabase.cs

--------------------------------

PURPOSE OF THIS DOCUMENT

This file helps:

• AI coding assistants  
• new developers  
• automated tooling  

understand the project quickly before modifying gameplay systems.