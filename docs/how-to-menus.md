# How to add and change menus (Last Hope)

This project uses **two UI layers** for menus:

| Layer | Used for | Typical files |
|-------|-----------|----------------|
| **MonoGame Gum** (`GumService`) | Title hub main entries, pause overlay buttons | `MainMenuScreen.cs`, `PausedMenu.cs`, `MenuAnimatedButton.cs`, `GumBootstrap.cs` |
| **SpriteBatch + `MenuBase`** | Full-screen menus (settings box, items index, roster, game over, etc.) | `SettingsMenu.cs`, `ItemsIndexMenu.cs`, … |

Gum is initialized once in `Last_Hope.LoadContent` via `GumBootstrap.Initialize` and updated/drawn every frame in `Last_Hope` (`GumService.Default.Update` / `Draw`) after `GameManager.Draw`.

---

## 1. Where menus are wired

1. **`GameState`** (`LastHope/GameState.cs`) — add a new enum value if you need a new top-level screen.
2. **`GameManager.Update` / `Draw`** (`LastHope/Engine/GameManager.cs`) — `switch` on `_state` calls `Menu.Update…` / `Menu.Draw…`.
3. **`Menu`** (`LastHope/UI/Menu.cs`) — holds one instance per menu type; add `UpdateX` / `DrawX` methods and a private field for new screens.
4. **Leaving Gum-backed states** — `GameManager.Update` calls `Menu.ReleaseMainMenuGum()` when leaving `MainMenu`, and `Menu.ReleasePausedMenuGum()` when leaving `Paused`. If you add another Gum-only state, add a similar release when transitioning away.

---

## 2. Title hub: add a main-menu row (Gum)

**File:** `LastHope/UI/Menus/MainMenuScreen.cs`

1. Add a string to the **`Entries`** array (order = top to bottom).
2. Extend **`ApplySelection(int index)`** with a new `case` for the new index (same order as `Entries`).
3. Set **`_state`** to the right `GameState`, or call **`Game.Exit()`**, etc.

**Important:** `ApplySelection` calls **`ReleaseGumUi()`** first so Gum controls are removed as soon as you leave the hub. Keep that pattern for any new exit path.

**Buttons** are **`MenuAnimatedButton`** instances: pass **`Game.GraphicsDevice`**, **`buttonWidth`**, and **`buttonHeight`** so the label/chrome match the row size (see `MenuAnimatedButton.cs`).

**Navigation:** Tab / Up / Down / W / S are configured in `GumBootstrap.cs` (`FrameworkElement.TabKeyCombos`, etc.).

---

## 3. Pause menu: add or change a button (Gum)

**File:** `LastHope/UI/Menus/PausedMenu.cs`

1. In **`EnsurePauseGum`**, after the `Panel` is created, add another **`MenuAnimatedButton`** (same `btnW` / `btnH` pattern as existing rows).
2. Set **`Text`**, **`X` / `Y`**, **`Width` / `Height`**, and **`Click`** (usually call **`ReleaseGumUi()`** then change **`_state`**).
3. Adjust **`stackH`**, **`startY`**, and focus (**`IsFocused`**) if you add/remove rows.

**Leaving pause to gameplay:** `Escape` already calls **`ReleaseGumUi()`** then sets **`GameState.Running`**.

**Settings from pause:** the **Settings** button sets **`gm.StateAfterClosingSettings = GameState.Paused`** before switching to **`SettingsMenu`**, so **Esc / Q** in settings returns to the paused game. The title hub sets **`StateAfterClosingSettings`** to **`MainMenu`** when opening settings from there (`GameManager.StateAfterClosingSettings`).

---

## 4. Custom Gum button chrome (`MenuAnimatedButton`)

**File:** `LastHope/UI/GumForms/MenuAnimatedButton.cs`

- Subclasses **`Gum.Forms.Controls.Button`** and configures **`ButtonVisual`** + nine-slice **animation chains** (MonoGame Gum tutorial style).
- **Do not** set `TextRuntime.CustomFontFile` to loose `Font.fnt` unless you also provide a **Content pipeline** asset Gum can load (e.g. `.xnb`). The game’s **`Font.fnt`** is intended for **`BmFont`** / **`GameManager.DrawUiString`** on SpriteBatch menus.

To reuse the same button look elsewhere, instantiate **`MenuAnimatedButton`** like `MainMenuScreen` / `PausedMenu` do.

---

## 5. New full-screen menu (SpriteBatch + `MenuBase`)

Use **`SettingsMenu`** or **`ItemsIndexMenu`** as templates.

### 5.1 Inherit `MenuBase`

You get:

- **`Game`**, **`gm`**, **`_state`**, **`Pixel`**, **`MenuUiScale`**, **`DrawHubMenuBackdrop`**, **`DrawHubMenuLeftRail`**, **`DrawPanelOutline`**, **`DrawUiString` / `MeasureUiString`** via `gm` / `MenuUiFont` patterns used across menus.

### 5.2 Match the “settings box” look

Same pattern as **`SettingsMenu.Draw`** / **`ItemsIndexMenu.Draw`**:

1. `spriteBatch.Begin(… PointClamp)`  
2. **`DrawHubMenuBackdrop`**, **`DrawHubMenuLeftRail`**  
3. `spriteBatch.End()` then begin again for the panel pass (or keep one batch if you prefer — match an existing menu for consistency).  
4. Full-screen dim: `Pixel` rectangle `(0,0,vp.Width,vp.Height)` with alpha **72**.  
5. Panel fill: **`new Color(18, 24, 38, 200)`** on the centered **`Rectangle`**.  
6. Border: **`DrawPanelOutline(spriteBatch, panel, new Color(140, 185, 255, 110))`**.  
7. Title + body: prefer **`gm.DrawUiString`** / **`gm.MeasureUiString`** so **bitmap font** works when `GameManager.FontBitmap` is set.

### 5.3 Wire the new screen

1. Add **`GameState.YourMenu`**.  
2. **`GameManager`**: `case` in **`Update`** and **`Draw`**.  
3. **`Menu.cs`**: field + **`UpdateYourMenu`** / **`DrawYourMenu`**.  
4. Navigate **to** it from another menu by setting **`_state = GameState.YourMenu`** (menus use **`MenuBase`**’s **`_state`** accessor to the game manager).

---

## 6. Text and fonts

- **SpriteBatch menus:** use **`GameManager.DrawUiString`** / **`MeasureUiString`** with `MenuUiFont` (and optional bitmap path) so behavior matches settings / items index.  
- **Gum:** default **V3** button text (SpriteFont). Scaling text heavily can look blurry; keep defaults unless you add a proper pipelined font for Gum.

---

## 7. Build / Git hygiene

- Prefer normal **`dotnet build`** / **`dotnet run --project LastHope/Last_Hope.csproj`** without **`-o`** into random folders under `LastHope/`.  
- Custom output dirs like **`LastHope/_align_check/`** are **ignored** in `.gitignore`; they should **never** be committed.

---

## 8. Reference links

- Gum UI overview: [Implementing UI with Gum](https://docs.monogame.net/articles/tutorials/building_2d_games/20_implementing_ui_with_gum/index.html)  
- Custom controls: [Customizing Gum UI](https://docs.monogame.net/articles/tutorials/building_2d_games/21_customizing_gum_ui/index.html)  

---

## Quick checklist (new `GameState` screen)

- [ ] `GameState` enum  
- [ ] New menu class inheriting **`MenuBase`** (`Update` + `Draw`)  
- [ ] `Menu.cs`: instance + public update/draw wrappers  
- [ ] `GameManager.Update` / `Draw`: switch cases  
- [ ] Entry point: set **`_state`** from another menu  
- [ ] If Gum-only overlay: **`Release…Gum`** when leaving that state  
- [ ] No **`dotnet -o LastHope/_scratch`** folders in Git  
