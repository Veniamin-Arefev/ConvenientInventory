Convenient Inventory

#### Rich text

This mod brings few new features:
- favorite slots. In a vanilla game, once you press quick stack to nearby chests all items from your inventory goes to corresponding chests. This behavior may be very annoying when you collect items for completing quests, making gifts or just play with friend and have space tools in public chest. This mod introduces favorite slots mechanic - items in this slots wont be auto stacked to nearby chests nor auto sorted to different slots. To mark slot as favorite you should click on it with pressed left alt button.
- excluding chest for auto stack mechanic. Chest name should start with "~exc". For example, "~exc chest with berries".
- hotkeys for Inventory auto-sort and auto-stack. By default, auto-sort is mapped on middle mouse button and auto-stack to G. You can press F1 and change mapped button(or disable by supplying invalid code). List of available codes can be seen on [this site](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/KeyCode.html).

#### Installation

1. Install [BepInEx 5 Pack﻿﻿](https://www.nexusmods.com/sunhaven/mods/30)
1. Extract archive file to <sunhaven-home>/BepInEx/plugins folder

#### Configs:

Configs are game wide. That means if you change them playing on one save, new configs will be loaded in second save.

General section:
- Hotbar always marked as favorite: regardless of other setting hotbar items never will be auto-stacked to chests
- Quick stack to nearby chests Key: Unity Keycode to quick stack to nearby chests
- Quick sort current inventory Key: Unity Keycode to quick sort your inventory

Hotbar Slots:
- Slots from 1 to 10: slots from left to right. Enabled means marked af favorite

Inventory Slots:
- Slots from 11 to 50: slots from left to right from top to bottom. First row is from 11 till 20, second is from 21 till 30, etc. Enabled means marked af favorite


#### Source code and report system

You may leave your message on Nexus or create issue/PR on github

[Source code](https://github.com/Veniamin-Arefev/ConvenientInventory).