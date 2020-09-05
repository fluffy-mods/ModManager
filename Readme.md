[![RimWorld 1.2](https://img.shields.io/badge/RimWorld-1.2-brightgreen.svg)](http://rimworldgame.com/)

Managing mods should be easy!

![Features](https://banners.karel-kroeze.nl/title/Features.png)  
A cleaner, better Mod management screen.
 - separate lists for available and active mods
 - create and delete local copies of steam mods
 - create and load mod list backups
 - load mod list from save games
 - (mass) subscribe to steam mods
 - proper search filtering
 - drag and drop activation and reordering
 - keyboard navigation
 - mod and mod list colouring
 - discover other mods by your favourite author(s)

 And, if supported by the mod author;
 - version checking
 - dependency checks
 - incompatibility checks
 - load order hints

![For Modders](https://banners.karel-kroeze.nl/title/For%20Modders.png)  
Allows modders to create a Manifest.xml file, enabling a bunch of features that should have been in vanilla;
 - version checking
 - dependency checking
 - incompatibility checking
 - load order hints

 See [the documentation](https://github.com/FluffierThanThou/ModManager/blob/master/ForModders.md) for details on how to implement this. It's easy!

![Details](https://banners.karel-kroeze.nl/title/Details.png)  
**local mod copies**  
You can make a local copy of any steam mods (or all active steam mods at once) by clicking the corresponding button underneath the mod details (or active mods list). Doing this will make sure any updates to the steam version will not break your game in progress. Local mods are created in the RimWorld/Mods folder, and can be manually deleted, or deleted by clicking the corresponding button underneath the mod details.

*Note: You may want to occasionally delete obsolete local copies, as having many mods in the mod folder will make RimWorld take longer to start, and the Mod Manager window take longer to open.*

**mod list backups**  
You can create mod lists by clicking the mod list button underneath your active mods, and selecting 'save current list'. You will have to choose a unique name, and the mod list will be stored in a folder next to your save games. 
To load a modlist, click the mod list button, then select the save game or mod list you want to load mods from. Any mods that couldn't be matched to your current list of mods will be greyed out. 
To delete a mod list (only the list, not the actual mods), click the mod list button, select the list to be deleted, and then choose the delete option.

**steam subscribing**
If a loaded modlist contains mods you do not have installed, but are available on the workshop, you can quickly install the mod by clicking the button in the mod details. You can also click the 'subscribe to all' button underneath the active mods list to subscribe to all missing mods.

**mod and mod list colours**
You can set colours for individual mods by clicking the palette icon underneath the mod details. To change the colour for all mods in a mod list, click the mod list icon underneat the active mod list, select the list, then 'change colour'.

**keyboard navigation**  
Sometimes, it's easier to navigate large lists with the keyboard.
 - Tab: cycles focus between search inputs and lists of mods.
 - Up/Down: selects the previous/next mod in the list.
 - Page Up/Down: selects the top/bottom mod in the list.
 - Left/Right: switch focus between active and available lists. 

 You can also manipulate the lists;
 - Enter: activates/deactivates a mod, and selects the next mod in the list.
 - Shift+left: deactivates a mod, keeping it selected, and switching focus to the available list.
 - Shift+right: activates a mod, keeping it selected, and switching focus to the active list.
 - Shift+up/Shift+down: move an active mod up/down in the load order.
 - Shift+Page Up/Down: move an active mod to the top/bottom of the load order.

 **mod promotions**
 When a Steam Workshop mod is selected (or a local copy of a steam workshop mod), Mod Manager will automatically look for other mods by the same author, and show you a promotion for any mods you don't already have. These promotions let you easily discover other (new) mods by your favourite author(s), and even quickly subscribe to them!

 *This function can be turned off in Mod Managers' settings*

![Contributors](https://banners.karel-kroeze.nl/title/Contributors.png)
 - erdelf:	invaluable help with Harmony transpilers
 - Brrainz:	multi-version targetting, inspiration for mod promotions
 - b606:	French translation
 - Proxyer:	Japanese translation, with frequent updates
 - 奔跑的梅洛斯:	Simplified Chinese translation
 - Zetrith:	Fix blurry mod descriptions
 - enescaglayan:	Turkish translation
 - Lex1975:	Russian translation
 - Torq:	Russian translation
 - Patrick Schmitt:	German translation
 - Han Yaodong:	Updates to Chinese translations
 - Michaël Marinetti:	Updates to French translation
 - shadowtouch:	better filename sanitizing code
 - Mohas:	fix nullref in online manifests

![Think you found a bug?](https://banners.karel-kroeze.nl/title/Think%20you%20found%20a%20bug%3F.png)  
Please read [this guide](http://steamcommunity.com/sharedfiles/filedetails/?id=725234314) before creating a bug report,
and then create a bug report [here](https://github.com/fluffy-mods/ModManager/issues)

![Older versions](https://banners.karel-kroeze.nl/title/Older%20versions.png)  
All current and past versions of this mod can be downloaded from [GitHub](https://github.com/fluffy-mods/ModManager/releases).

![License](https://banners.karel-kroeze.nl/title/License.png)  
All original code in this mod is licensed under the [MIT license](https://opensource.org/licenses/MIT). Do what you want, but give me credit.
All original content (e.g. text, imagery, sounds) in this mod is licensed under the [CC-BY-SA 4.0 license](http://creativecommons.org/licenses/by-sa/4.0/).

Parts of the code in this mod, and some content may be licensed by their original authors. If this is the case, the original author & license will either be given in the source code, or be in a LICENSE file next to the content. Please do not decompile my mods, but use the original source code available on [GitHub](https://github.com/fluffy-mods/ModManager/), so license information in the source code is preserved.

[![Supporters](https://banners.karel-kroeze.nl/donations.png)](https://ko-fi.com/fluffymods)

![Are you enjoying my mods?](https://banners.karel-kroeze.nl/title/Are%20you%20enjoying%20my%20mods%3F.png)  
Become a supporter and show your appreciation by buying me a coffee (or contribute towards a nice single malt).

[![Buy Me a Coffee](http://i.imgur.com/EjWiUwx.gif)](https://ko-fi.com/fluffymods)

[![I Have a Black Dog](https://i.ibb.co/ss59Rwy/New-Project-2.png)](https://www.youtube.com/watch?v=XiCrniLQGYc)


![Version](https://banners.karel-kroeze.nl/title/Version.png)  
This is version 3.8.1038, for RimWorld 1.2.2723.