# HSPlugins

## Description
A random bunch of plugin mods for Honey Select.  
Use ModMenuManager to change your current hotkeys/settings for the mods.  
Feel free to remove the xml files related to mods you don't use.

If you've found a bug or think something could be improved with any of my mods please make a github issue or a hongfire comment about it or message me on the HS discord.
<br>

![Image](examplepic.jpg)

## Installation
1. Install Illusion Plugin Architecture (IPA).
2. Throw the files into the Honey Select root folder.

## Plugins

#### BetterSceneLoader - [Download](https://github.com/Keelhauled/HSPlugins/releases/download/second/BetterSceneLoader.v1.1.1.zip)
Faster to use scene loader for Studio Neo.  
Scenes are loaded from `\UserData\studioneo\BetterSceneLoader`.  
The subfolders of this folder will act as categories for your scenes.  
`order.txt` in the aforementioned folder can be used to customize the order of the categories in the dropdown menu.  
All the necessary folders and files are created after running the game once.  
A scene with the filename `defaultscene.png` in the `BetterSceneLoader` folder will be loaded when starting the game.  
*ModMenuManager is required to use this*.

<details><summary>Changelog</summary>

```
v1.1.1
- Switched to a hopefully more reliable way to determine paths for scenes
```
```
v1.1.0  
- Added a feature to set a default scene that is loaded when starting the game
```
</details>

#### HideUI - [Download](https://github.com/Keelhauled/HSPlugins/releases/download/v1.0.0/HideUI.zip)
Hide all the UI with one click, hotkey is `M` by default.  
More menus can be added in `\Plugins\InterfaceSuite\HideUI.txt`.

#### LightManager - [Download](https://github.com/Keelhauled/HSPlugins/releases/download/v1.0.0/LightManager.zip)
Currently the only use is making spotlights track characters.  
To use, first select the lights you want to target and then select a character and press apply in the anim menu.  
Importing a scene doesn't do anything at the moment.  
*[HSExtSave](http://www.hongfire.com/forum/forum/hentai-lair/hf-modding-translation/honey-select-mods/5747804) is required to use this*.

#### ModMenuManager - [Download](https://github.com/Keelhauled/HSPlugins/releases/download/v1.0.0/ModMenuManager.zip)
A menu to manage all other mod menus.  
Menus can be added or removed in `\Plugins\InterfaceSuite\ModMenuManager\ModMenuManager.xml`.

#### ModSettingsMenu - [Download](https://github.com/Keelhauled/HSPlugins/releases/download/v1.0.0/ModSettingsMenu.zip)
Easily customizable in-game settings menu for mods.  
Settings can be added or removed in `\Plugins\InterfaceSuite\ModSettingsMenu`.  
*ModMenuManager is required to use this*.

#### TogglePOV - [Download](https://github.com/Keelhauled/HSPlugins/releases/download/second/TogglePOV.v1.0.1.zip)
The original and the best POV mod for Honey Select improved and updated for the latest version.  
In an hscene the selected chara is the one that is closest to the cameratarget when pressing the button.  
Right mouse button can be used to change the FOV while in first person mode.  
Default hotkey is `Backspace`.  
Core code not written by me.

<details><summary>Changelog</summary>

```
v1.0.1
- Fixed the default hotkey
```
</details>

#### Harmony4KPatch - [Download](https://github.com/Keelhauled/HSPlugins/releases/download/second/Harmony4KPatch.v1.0.0.zip)
A harmony version of Plasticmind's 4k patch. This means that it doesn't replace any dll files.  
Some small features like bloom are not edited in this version yet because who uses bloom right. :)  
To install, download the 4k patch normally, delete the Data folders from the mod folder, install it and then install Harmony4KPatch like a normal plugin.

#### NeckSettings - [Download](https://github.com/Keelhauled/HSPlugins/releases/download/second/NeckSettings.v1.0.0.zip)
This was supposed to be a fully featured settings menu for necklook but at the moment it is just a small tweak to make the character look at the camera similar to how it is in PlayHome.  
Still a work in progress so there may be a few neck twisting glitches remaining.


## Credits
Keelhauled  
Joan6694 for HSExtSave and UIUtility  
Original maker of TogglePOV  
Plasticmind for the 4k patch
