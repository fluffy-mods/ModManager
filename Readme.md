[![RimWorld Alpha 18](https://img.shields.io/badge/RimWorld-Alpha%2018-brightgreen.svg)](http://rimworldgame.com/)

Managing mods should be easy!

# Features
A cleaner, better Mod management screen.
 - separate lists for available and active mods
 - create and delete local copies of steam mods
 - create and load mod list backups
 - load mod list from save games
 - proper search filtering
 - drag and drop activation and reordering
 - keyboard navigation

 And, if supported by the mod author;
 - version checking
 - dependency checks
 - incompatibility checks
 - load order hints

# For Modders
Allows modders to create a Manifest.xml file, enabling a bunch of features that should have been in vanilla;
 - version checking
 - dependency checking
 - incompatibility checking
 - load order hints

 See [the documentation](https://github.com/FluffierThanThou/ModManager/blob/master/ForModders.md) for details on how to implement this. It's easy!

# Details
**local mod copies**  
You can make a local copy of any steam mods (or all active steam mods at once) by clicking the corresponding button underneat the mod details (or active mods list). Doing this will make sure any updates to the steam version will not break your game in progress. Local mods are created in the RimWorld/Mods folder, and can be manually deleted, or deleted by clicking the corresponding button underneath the mod details.

**mod list backups**  
You can create mod lists by clicking the button underneath your active mods. You will have to choose a unique name, and the mod list will be stored in a folder next to your save games. 
To load a modlist, click the corresponding button underneath your active mods, then select the save game or mod list you want to load mods from. Any mods that couldn't be matched to your current list of mods will be greyed out. 
To delete a mod list (only the list, not the actual mods), click the corresponding button and select the list to be deleted.

**keyboard navigation**  
Sometimes, it's easier to navigate large lists with the keyboard.
 - Tab: cycles focus between search inputs and lists of mods.
 - Up/Down: selects the previous/next mod in the list.
 - Left/Right: switch focus between active and available lists. 

 You can also manipulate the lists;
 - Enter: activates/deactivates a mod, and selects the next mod in the list.
 - Shift+left: deactivates a mod, keeping it selected, and switching focus to the available list.
 - Shift+right: activates a mod, keeping it selected, and switching focus to the active list.
 - Shift+up/Shift+down: move an active mod up/down in the load order.

# Contributors
 - erdelf:	invaluable help with Harmony transpilers
 - Brrainz:	multi-version targetting
 - b606:	French translation

# Think you found a bug? 
Please read [this guide](http://steamcommunity.com/sharedfiles/filedetails/?id=725234314) before creating a bug report,
 and then create a bug report [here](https://github.com/FluffierThanThou/ModManager/issues)

# Older versions
All current and past versions of this mod can be downloaded from [GitHub](https://github.com/FluffierThanThou/ModManager/releases).

# License
All original code in this mod is licensed under the [MIT license](https://opensource.org/licenses/MIT). Do what you want, but give me credit. 
All original content (e.g. text, imagery, sounds) in this mod is licensed under the [CC-BY-SA 4.0 license](http://creativecommons.org/licenses/by-sa/4.0/).

Parts of the code in this mod, and some content may be licensed by their original authors. If this is the case, the original author & license will either be given in the source code, or be in a LICENSE file next to the content. Please do not decompile my mods, but use the original source code available on [GitHub](https://github.com/FluffierThanThou/ModManager/), so license information in the source code is preserved.

# Are you enjoying my mods?
Show your appreciation by buying me a coffee (or contribute towards a nice single malt).

[![Buy Me a Coffee](http://i.imgur.com/EjWiUwx.gif)](https://ko-fi.com/fluffymods)

# Version
This is version 0.17.586, for RimWorld 0.19.2009.