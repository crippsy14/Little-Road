# UI design: HUD, pause hub, menus

Written 29 September 2026 for Richard's UI overhaul request. The current UI "looks great but feels muddled and bolted together". This doc covers the research, the rules that come out of it, and the screen map every in-game UI now follows.

## What the research says

The main sources are the Xbox Accessibility Guidelines (XAG 112, UI navigation), the Game Accessibility Guidelines, the Forza Horizon pause and information hub, and menu-design write-ups; the full list is below.

1. **One predictable structure.** Players expect a pause menu to hold the map, collection or progress, and settings, and to quit from there. Group related items, and keep the top level to fewer than seven entries.
2. **Tabs on the shoulder buttons.** LB/RB (Q/E on keyboard) move between hub tabs everywhere. Forza, Sea of Thieves and most console games do this, and it's what players' hands already know.
3. **Back goes up exactly one level.** B, Esc or Circle always returns to the previous screen, never all the way out. Only the top level closes the menu.
4. **Consistent prompts in a consistent place.** A footer in the same corner of every screen lists the buttons that work here ("A Select · B Back · LB/RB Tabs"), in the player's own glyphs.
5. **Focus you can predict.**
   - Every screen has a default selection.
   - Focus moves the way the layout looks.
   - Vertical lists wrap.
   - Mouse, keyboard and pad all work everywhere.
6. **Minimal HUD, progressive disclosure.** Driving needs speed, the map and what you can do right now. Everything else (credits, collection counts, controls) lives in the pause hub, or appears briefly when it changes or when you stop.
7. **Readable.** High contrast (4.5:1 or better), large type, no text that merges into its own outline. The loading screen's navy-on-navy title broke this.
8. **A map with a list alternative.** The map shows where things are; the Collection tab lists the same items as an album.
9. **Immediate feedback.** Selection scales and highlights at once, and tabs slide.

## HUD while driving (free roam)

| Place | What | When |
|---|---|---|
| Bottom centre | **Speed dash**: mph and D/R, the same instrument as in races. | Always while driving. |
| Bottom right | **Minimap**: heading-up, with marks. | Always. |
| Top left | **Objective** pill. | For 8 s after it changes, while you're stopped, and throughout the Castle Climb (with the clock). |
| Top centre | **Area banner** ("HILLCREST · Market town on the hill"). | On entering an area. |
| Above the dash | **Talk to …** prompt. | Near a resident. |
| Bottom left | **Controls** hint. | For the first 15 s and whenever you've been stopped for 3 s. |

Credits, postcards and stamps are no longer on screen all the time. They toast when they change, and live in the pause hub.

## Pause hub (Esc or Start)

The hub is a full-screen card over the dimmed game.

- **Header:**
  - the tab strip, with LB/RB glyphs at its ends;
  - on the right, the current area and credits.
- **Footer:** the prompts.

The tabs, left to right:

1. **MAP.**
   - The world map, with region outlines and the minimap marks.
   - Opens on your current region, highlighted, with its collection counts beside the map (postcards and stamps found out of the total for that region).
   - Moving the cursor (mouse or left stick) over another region outlines that region and switches the counts to it.
   - A or click opens the full-screen map with every place name. Back returns to this tab.
2. **COLLECTION.** An album per region:
   - Bayview postcards (5);
   - Hillcrest and valley stamps (8);
   - the Castle Climb and Hillcrest visit badges.

   Found items show where you found them; missing ones show "?" and which region to search.
3. **CAR.**
   - your car as a picture, its body and paint;
   - fitted parts by slot and its class rating;
   - race record (wins, best place) and class-ladder progress.

   Changing anything is done at a Pit Stop, so this tab is read-only.
4. **SETTINGS.**
   - quality preset, resolution, display mode, each graphics option, and time of day;
   - Quit, which asks for confirmation one level down.

## Conversations and shops

- **Back is one level.** In conversation menus (Mara, Gio, Vito and the rest), B/Esc picks the menu's own Back choice, which goes up a level, for example from a part list to the garage menu to the host's first menu. Only a top-level conversation closes.
- **Shops.** The parts browser, paint shop and body shop use the same footer prompts and back behaviour, and the same LB/RB for their categories where they have them.

## Sources

- [Xbox Accessibility Guideline 112: UI navigation](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/112)
- [Game Accessibility Guidelines (basic)](https://gameaccessibilityguidelines.com/basic/)
- [Provide high contrast between text/UI and background](https://gameaccessibilityguidelines.com/provide-high-contrast-between-text-ui-and-background/)
- [Creating an intuitive in-game menu (Indie Klem)](https://indieklem.substack.com/p/9-creating-an-intuitive-in-game-menu)
- [UX/UI in video games: the pause menu](https://medium.com/design-bootcamp/ux-ui-in-video-games-the-pause-menu-6f07e113e21e)
- [Game UI Database: pause screens](https://www.gameuidatabase.com/index.php?scrn=44)
- [Game UI Database: Forza Horizon 5](https://www.gameuidatabase.com/gameData.php?id=1225)
- [Forza Horizon 5 pause menu UI (William Emery)](https://williamemery.artstation.com/projects/29lJQa)
- [The minimal HUD paradox](https://medium.com/@salamatizm/the-minimal-hud-paradox-how-dreams-of-diegetic-game-interfaces-often-lead-to-cluttered-nightmares-e9cf7fae9d73)
- [Controller buttons guide (bumpers for tabs)](https://www.metal-domes.com/blog/2025/01/03/video-game-controller-buttons-an-deepth-guide/)

## As built (29 September)

- **HUD** (`AdventureUI.Roam`):
  - the race speed dash sits at the bottom centre;
  - the objective pill (top left) fades in for 8 s when it changes, after 1.5 s stopped, and for the whole Castle Climb;
  - the one-line controls hint (bottom left) shows for the first 15 s and after 3 s stopped;
  - the "Talk to …" card now sits above the dash.

  The credits and collection chips are gone from the driving view.
- **Pause hub** (`PauseMenu.cs`): opened with Esc or Start, over the dimmed, paused world (time and audio stop).
  - **Tabs:** Q/E, LB/RB or L1/R1, or a click.
  - **Map tab:**
    - the baked world map with Bayview, Hillcrest and Valley outlines; the one under the cursor is outlined bold and tinted;
    - the cursor follows the mouse, the left stick or WASD and the arrows;
    - postcard and stamp counts for the hovered region;
    - A, Enter or a click on the map opens the full map, and Back returns to the tab.
  - **Collection:** the album, with a place name on each found card and "Somewhere in <region>" on each missing one, plus badges.
  - **Car:** a rendered picture of the car, its fitted parts, performance rating, record and class ladder.
  - **Settings:** every option cycles in place as "Label ‹ value ›" and keeps focus on the row you changed. Quit asks for confirmation one level down.
- **Back is one level.** In conversations, Esc and B pick the menu's own Back choice ("Back", "Back to all parts", "Back to the road"). B no longer opens pause; Start and Esc do.
- **Prompts.** The keyboard's confirm prompt reads "Enter" (menus never used E). The parts browser, paint shop and body shop footers now follow the controller in use instead of hard-coded PlayStation names.
- **Checks.** `InterfaceChecks` (editor) covers:
  - region classification;
  - album place names;
  - footers naming Back and Tabs;
  - both climb prizes as known collectibles;
  - the climb route and its corridor test.

  Reviews: `--pause-review` (HUD, every tab, a region hover, quit confirm, a conversation) and `--climb-review`.

## Parts: shop and fitting bay (29 September, after Richard's feedback)

Richard's feedback on the first parts browser:
- R1/L1 moved up and down a vertical list;
- the fitted, owned and buyable states swapped confusingly;
- buying and fitting were mixed together.

The redesign follows the games that do this well:
- **Buying and installing are separate places.** Gran Turismo 7 sells parts at the Tuning Shop and installs or removes them in Garage > Car Settings.
  - Here, **Sid and Rosa's parts bay only buys**. It lists the town's catalogue, and stock parts aren't for sale.
  - **Mara and Gio's fitting bay only fits**, and shows only parts you own.
- **Categories are icon tiles across the top.** Forza's upgrade categories are icon tiles; here each tile shows the category's stock part. The current tile opens out to show its name, accordion-style.
  - LB/RB (Q/E on keyboard) move left and right along the row, so the bumpers match the direction on screen. [ ] still work too.
- **One status, one place, one action.** The standard advice for inventories is an obvious equipped marker and one clear action per item.
  - **Status badge:** the card's top-right corner holds a single badge: ✓ FITTED, OWNED, a price, or the locked class.
  - **Action:** one button underneath shows the confirm glyph and the only thing you can do (BUY 450 CR or FIT ENGINE). It greys out, with the reason, when there's nothing to do.
  - **Neighbour cards:** a tick marks the one that's fitted.
- **Compare before committing.** The fitted-versus-previewed stats stay, per category.
- **Spinning the part:** right stick, Z/X, or drag it with the mouse.

Checks: `tools/run-parts-review.ps1` runs 34 checks, covering:
- the shop can't fit and the fitting bay can't sell;
- confirm buys, or fits;
- LB/RB, Q/E and the tabs;
- only owned parts show in the fitting bay;
- reload persistence.

The smoke test now buys at Sid's and fits at Mara's.

Sources:
- [GT7 manual: Car Settings](https://www.gran-turismo.com/us/gt7/manual/carsettings/02)
- [GT7 manual: Tuning Shop](https://www.gran-turismo.com/nz/gt7/manual/tuningshop/01)
- [Forza Wiki: Upgrades](https://forza.fandom.com/wiki/Upgrades)
- [Game UI Database: Forza Horizon 5](https://www.gameuidatabase.com/gameData.php?id=1225)
- [Game UI Database: Rocket League](https://www.gameuidatabase.com/gameData.php?id=134)
- [Improving inventory screens (John Burnett)](https://thejohnburnett.com/how-to-improve-inventory-screens-in-your-video-game-ui-design-portfolio/)
- [Inventory UX (The Acagamic)](https://acagamic.com/newsletter/2023/03/21/how-to-unlock-the-secrets-of-video-game-inventory-ux-design/)

## Fog of war, places and toasts (29 September, later)

- **Fog of war** (`ExploredMap`, `LittleRoad/Minimap` shader):
  - **Storage:** a 256×256 explored mask (about 10 m cells) is saved with the adventure. Unexplored ground on the minimap, full map and pause map sits under soft drifting cloud, with a faint outline of the land beneath.
  - **What stays hidden:** people, Pit Stops and zipline marks until explored (the objective always shows). Full-map place names too.
  - **The measuring stick for reveals:** driving reveals a 175 m circle, the size of the minimap's standstill view (150 m) plus a soft edge. At a standstill the corner map is never blotchy, but at speed (330 m) fog shows at its edges, which invites you onward.
  - **Big reveals:**
    - the valley viewpoint and the Silver Falls lookout reveal 520 m;
    - the castle reveals 620 m;
    - arriving in Hillcrest reveals the whole town.
  - **Model:** this follows Ghost of Tsushima (a small reveal as you travel, big reveals for landmarks) and Forza Horizon 6 ("where you've been and what you've seen").
  - **Home:** Bayview island starts revealed.
  - **Saving:** the mask saves every 20 s while it changes, and before races and quitting.
- **Places:** the first time you reach a place, a toast card with a DISCOVERED tag shows its name and tagline (saved in `discovered`, so it happens once). After that, the name just sits under the minimap. The big banner that popped up every time you crossed an area line has gone.
- **Toasts:** the stamped headline over a spinning sunburst is replaced by a compact card that slides down under the objective pill. It has a coloured stripe, the headline, one detail line and an optional tag.
- **Talk prompt:** half the size (500×72) and sits above the speed dash.
- **Controls:** no button hints on screen while driving, in free roam or races. A **CONTROLS** tab in the pause menu lists every action for keyboard and mouse, Xbox and PlayStation, and marks the one in use.
- **Look behind:** the camera is locked 6.5 m (free roam) or 5.8 m (race) ahead of the car. Smoothing had let it lag back into the car at speed.

Sources:
- [Ghost of Tsushima: lifting the fog of war](https://gamerant.com/ghost-of-tsushima-uncover-map-reveal-fog-of-war/)
- [Forza Horizon 6's fog of war](https://www.thegamer.com/forza-horizon-6-fog-of-war-first-time-ever/)
- [Game UI Database: minimaps](https://www.gameuidatabase.com/index.php?scrn=135)
