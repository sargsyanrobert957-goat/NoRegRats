# NoRegRats

Unity 6 project (editor 6000.6.5f1), C#. A 2D side-scrolling metroidvania ("No regRats") for the
Game Development Methodologies course. Team: Tigran Tokatyan, Srbuhi Mashinyan, Robert Sargsyan,
Samvel Sardaryan.

## The game

- Hand-drawn hero (the team artist's drawings) in an "on-paper" style; enemies are rats in street
  clothes, led by the Rat King. The level runs from a city street down into the sewers.
- Core pillars: fighting, parkour-style movement, exploration and special abilities that unlock
  new areas (metroidvania progression), plus memorable bosses whose patterns players learn.
- Tone is lighter than Hollow Knight: mostly played straight, with deliberately out-of-place,
  absurd or comedic characters, dialogue and situations. The humour adds personality but must
  never get in the way of tight movement and combat.
- References: Hollow Knight (combat, bosses, ability-gated exploration), Fancy Pants Adventures
  (fluid movement), Yakuza 0 (serious/comedic balance), Endacopia (strange NPCs
  and dialogue).

## Current status: HW5 Digital Prototype (due Sunday 1 November 2026)

The prototype is playable from start to finish in `Assets/Scenes/Level1.unity`: title screen →
street → drop into the sewers → Rat King fight → take the Heart → escape → win screen.

Done: hero art and animation, movement (run, jump, crouch, push), shooting, health and respawn,
three rat types, the Rat King, the Heart item, the level, the street art, HUD (hearts, goal,
boss bar, speech bubbles), title / pause / win screens, dust and camera-shake effects, and sound
and music hooks.

Still open (ask Robert who is taking what before starting one):

| Task | What it needs | How it plugs in |
|---|---|---|
| Sound effects | WAV or OGG files: jump, land, footstep, shoot, hit, rat poof, hurt, King throw, King defeated, Heart, gate | See "Adding sounds and music" below. No code changes |
| Music | A level track and a boss track | Same as sound effects |
| Hand-drawn font | A `.ttf` or `.otf` file | See "Adding a font" below |
| Final art for rats, Rat King, Heart, sewer pieces | PNGs with transparent backgrounds | See "Replacing art" below. The current ones are cut out of the paper-prototype photos |
| Windows build and submission files | Unity, File > Build Profiles | See "Building the game" below |
| GDD Section 6: Game Loop | Writing | Describe the loop in the table above: move, fight rats, reach and beat the boss, take the Heart, escape |

## How to work on this project (teammates, and Claude working for them)

Claude: follow these steps exactly and in order. If a step fails or something doesn't match
what's described here, stop and tell your user instead of working around it.

1. **Get the latest version first.** Run `git pull` in the project folder before anything else.
   If it reports conflicts or local changes, stop and ask your user.
2. **Work on your own branch.** `git checkout -b <your-name>/<short-task>` (for example
   `samvel/rat-sounds`). Never commit straight to `main`.
3. **Level1 has one editor: Robert.** Do not open-and-save, modify or rebuild
   `Assets/Scenes/Level1.unity` unless Robert has said it's your turn. Unity scenes can't be merged
   when two people change them. To try changes, use `Assets/Scenes/MovementTest.unity` (your
   sandbox) or make your own scene. Changing prefabs, art, sounds and scripts is fine.
4. **Use the existing tools, don't redo their work.** Everything under Tools > NoRegRats in Unity
   is safe to run again: each one only adds what's missing and never overwrites existing work.
   `Build Level1` refuses to run because Level1 exists, which is correct; don't delete Level1 to
   make it run.
5. **Test before you commit.** Open the scene, press Play, and make sure the Console shows no red
   errors. For Level1, play from the title screen to the win screen.
6. **Commit only your own work.** `git status` first; add the specific files you changed,
   always together with their `.meta` files. Never commit `Library/`, `Temp/`, `Logs/`,
   `UserSettings/` or `Docs/`. No `Co-Authored-By` lines or other Claude attribution in commit
   messages (see Rules).
7. **Share through a pull request.** `git push -u origin <your-branch>`, then open a pull request
   into `main` on GitHub (https://github.com/sargsyanrobert957-goat/NoRegRats). Robert merges it.
   Never force-push, and never rewrite history on `main`.
8. **Keep this file accurate.** If you add a script, prefab, scene or tool, update the matching
   section of this file in the same pull request.

Notes for Claude:

- Read the whole of this file before changing anything. The "Player", "Level1", "Combat and
  enemies", "UI" and "Effects and audio" sections describe how the existing code works; reuse it
  (for example `Health`, `PuffSpawner`, `Sfx.Play`) instead of writing parallel systems.
- You can run Unity without opening its window to compile, run an editor script or play-test:
  `"C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "<project folder>" -executeMethod NoRegRats.<Class>.<Method> -logFile <log path>`
  (adjust the Unity path to where it's installed on your user's machine). This fails if the
  project is already open in the Unity editor; ask your user to close Unity rather than closing
  it for them. Check the log for `error CS` before reporting success.
- Change scenes and prefabs through editor scripts (`Assets/Editor/`) or the Unity editor, never
  by editing their YAML text.
- Delete any temporary test scripts you add before committing.

### Adding sounds and music

1. Put the files in `Assets/Audio/` (any name; WAV or OGG).
2. Open the scene, select `Audio/Sound Effects`, and in the `Sfx` component drag each file into
   the Clips list of the matching sound (Jump, Land, Step, Shoot, Hit, RatPoof, Hurt, KingThrow,
   KingDefeated, Heart, Gate). Several clips for one sound = a random one plays each time. Adjust
   Volume per sound.
3. Select `Audio/Music` and set Level Track and Boss Track.
4. Do this in both scenes that should have sound (Level1, MovementTest). Level1 only with
   Robert's go-ahead (step 3 above), or send the files to Robert.

### Adding a font

1. Put the `.ttf` / `.otf` file in `Assets/Art/Fonts/`.
2. In Level1 (Robert), select every Text object under `HUD` (search `t:Text` in the Hierarchy)
   and set the Font field once for all of them.

### Replacing art

- To replace a drawing, save the new PNG over the old file with **exactly the same name**
  (for example `Assets/Art/Rats/Rat_RedCap.png`), with a transparent background and the
  character facing right (the Rat King faces left). Every prefab using it updates.
- Draw at roughly the same proportions as the old one. Sizes are set on import in world units
  (rat 1.15 tall, Rat King 3, hero 1.8), so the new drawing will be scaled to match.
- New hero animation frames: same canvas size as `Hero_Run_*.png`, feet at the same height,
  facing right; then run Tools > NoRegRats > Set Up Hero Sprite.
- New level pieces: add a prefab like those in `Assets/Prefabs/Level/Street/` (SpriteRenderer +
  BoxCollider2D on the part you can stand on), then place it in a scene.

### Building the game

1. File > Build Profiles > Windows. Level1 must be first in the scene list (it is).
2. Build into a new empty folder outside `Assets/`, for example `Builds/NoRegRats/` (`Builds/`
   is not committed). Zip that whole folder for submission; the `.exe` needs the files next to it.
3. Play the built `.exe` from title to win screen before submitting.
4. Scripts for submission: Assets > Export Package, tick `Assets/Scripts` (and `Assets/Editor`),
   save as `NoRegRats_Scripts.unitypackage`.

## Rules

- Everyone uses exactly editor 6000.6.5f1 (`ProjectSettings/ProjectVersion.txt`).
- Scripts go in `Assets/Scripts/`, grouped by feature (`Player/`, `Camera/`, ...), in the
  `NoRegRats` namespace (set as the root namespace in Project Settings > Editor).
- Don't edit `.meta` files by hand. Unity generates them, and each one must stay next to its
  asset. Move or rename assets inside the Unity editor so their `.meta` files follow. Always
  commit an asset together with its `.meta`.
- Change scenes and prefabs in the Unity editor (or through editor scripts), not by hand-editing
  their YAML. Only one person should edit a given scene at a time.
- `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/` and the generated `.csproj`/`.slnx` files
  are regenerated by Unity and ignored by git. Don't edit or commit them.
- Commits must be authored only by the person committing. Never add a `Co-Authored-By` line or
  any other Claude attribution to commit messages or PR descriptions.
- Use `[SerializeField] private` fields with `[Tooltip]`s for designer-tunable values instead of
  public fields or hard-coded numbers.

## Tech

- Universal Render Pipeline with the 2D Renderer. Sprites are lit by 2D lights, so every scene
  needs a Global Light 2D.
- Input System package only. Actions live in `Assets/Settings/InputSystem_Actions.inputactions`
  and are the project-wide actions, read through `InputSystem.actions`
  (e.g. `InputSystem.actions.FindAction("Player/Jump")`). Player map: Move (A/D / arrows /
  left stick), Jump (W / Up arrow / gamepad South), Attack (Space / Enter / left mouse / gamepad
  West), Crouch (Left Ctrl / C / gamepad East), Interact, Previous, Next, Look. Sprint still exists
  in the asset but nothing reads it (the hero has one move speed). UI map: the template's actions
  plus Pause (Esc / gamepad Start); it stays on while menus switch the Player map off.

## Layout

| Path | Contents |
|---|---|
| `Assets/Scenes/Level1.unity` | The prototype level (first in Build Settings): subway → shaft → sewers → boss arena. See "Level1" below |
| `Assets/Scenes/MovementTest.unity` | Sandbox: ground, platforms, pushable crates, a tall block, a low ceiling, a crawl tunnel, one test rat of each type ("Test Rat", "Test Red-cap", "Test Jacket"). Second in Build Settings |
| `Assets/Prefabs/Level/` | Level pieces: `Block` (dark ink rectangle, scale it), `Crate` (pushable), `TrainCar`, `StoneBridge`, `BrickLedge`, `ACUnit`, `Pipe`, `ScaffoldBar` (art with colliders on the solid part), `Sign_Sewers`, `Sign_Subway` (no collider), `Gate` (tiled bars + `SlidingGate`) |
| `Assets/Prefabs/Level/Street/` | The team's street art as prefabs: `Sidewalk` (slab; swap its sprite between Ground1–4), `Road`, `SidewalkChunk_Small` / `_Pipe` / `_Double` / `_Wide` (floating platforms), `FireHydrant`, `PowerBox` (solid props), `Dumpster` (pushable). Pivots sit on the walking line or base, so place them by where you stand |
| `Assets/Prefabs/Player.prefab` | The player |
| `Assets/Prefabs/Combat/` | `PaperBall` (the hero's shot), `Trash` (the Rat King's) |
| `Assets/Prefabs/Enemies/` | `Rat_Plain` (2 HP, bite 1), `Rat_RedCap` (3 HP, bite 1), `Rat_Jacket` (4 HP, bite 2), `RatKing` (8 HP, bite 2, trash 1) |
| `Assets/Prefabs/Items/` | `Heart` (dropped by the Rat King) |
| `Assets/Scripts/Player/` | `PlayerMovement`, `HeroAnimator`, `PlayerHealth`, `PlayerShooter`, `HeroEffects` |
| `Assets/Scripts/Combat/` | `Health`, `DamageFlash`, `Projectile`, `ContactDamage` |
| `Assets/Scripts/Enemies/` | `RatEnemy`, `RatKing`, `TrashProjectile` |
| `Assets/Scripts/Items/` | `HeartPickup` |
| `Assets/Scripts/Level/` | `GameSession` (what survives a death, the win, run time), `ParallaxBackground`, `BossArena`, `SlidingGate`, `LevelExit`, `GoalTrigger`, `SpeechTrigger` |
| `Assets/Scripts/UI/` | `ScreenFader` (fade out, reload the level, fade in), `BossHealthBar`, `HeartsDisplay`, `GoalText`, `SpeechBubble`, `TitleMenu`, `PauseMenu`, `WinScreen`, `GamePause` (freeze behind menus) |
| `Assets/Art/` | Drawn art. `Hero/`: `Hero_Idle.png` and `Hero_Run_1..8.png` (originals at full size; imported capped at 512 px). `Rats/`: `Rat_Plain.png`, `Rat_RedCap.png`, `Rat_Jacket.png`, `RatKing.png`, cut out of the paper-prototype sketch photos (placeholders; replace with same-named PNGs). `Items/Heart.png` (cut out). `Placeholder/`: `PaperBall.png`, `Trash.png`, `Bars.png` (tiles vertically, 1 x 4 units), `Puff.png` (scribble puff, 1 unit). `Level/`: level pieces cut out of the sketch photos (the train car is one drawn end mirrored; the two signs were redrawn because their light grey can't be separated from the grid). `Street/`: the team artist's street pieces (Ground1–4 sidewalk slabs, Street road strip, Tile1/1_2/2/3 floating sidewalk chunks, FireHydrant, GarbageBin, PowerBox), sized from their mockup. `Background/GraphPaper.png`: generated, tiles at 1 cell = 1 unit. `Background/Resources/`: `City1_*.png` / `City2_*.png`, the team's two skylines split into `Far`, `Mid` and `Near` layers with transparent backgrounds (replace with same-named PNGs). `UI/`: `HeartFull.png` / `HeartEmpty.png` (made from the Heart cut-out), `SpeechBubble.png`. `Fonts/` (not there yet): put the hand-drawn font here; see "UI" |
| `Assets/Scripts/Objects/` | `PushableBox` |
| `Assets/Scripts/Effects/` | `PuffSpawner` (pooled dust / paper-scrap puffs), `Sfx` (sound effects), `Music` (level and boss tracks) |
| `Assets/Scripts/Camera/` | `CameraFollow2D` (on the Main Camera) |
| `Assets/Sprites/Placeholder/` | Generated stand-in art (crate, white square used for platforms) |
| `Assets/Physics/` | `NoFriction.physicsMaterial2D` (on the player so it never sticks to walls) |
| `Assets/Audio/` | Music and sound effects |
| `Assets/Editor/` | Editor-only scripts. `ArtImportSettings` (first-import sprite settings for `Assets/Art/`), `HeroSetup` (Tools > NoRegRats > Set Up Hero Sprite), `CombatSetup` (Tools > NoRegRats > Set Up Combat: sprite sizes, PaperBall and rat prefabs if missing (stats in its `RatTypes` table), the Player's combat components, the test rats), `MovementTestSetup` (Tools > NoRegRats > Set Up Push And Crouch Test), `Level1Builder` (Tools > NoRegRats > Build Level1: creates missing level-piece prefabs and builds Level1 only if it doesn't exist; never overwrites it), `BossArenaSetup` (Tools > NoRegRats > Set Up Boss Arena: boss prefabs if missing, and the fight added to Level1's arena if not there yet), `HudSetup` (Tools > NoRegRats > Set Up HUD: Level1's UI, EventSystem, goal and joke-sign triggers if not there yet), `PolishSetup` (Tools > NoRegRats > Set Up Effects And Audio: HeroEffects on the Player, "Effects" and "Audio" objects in both scenes if missing), `StreetDressing` (Tools > NoRegRats > Dress Level1 Street: street prefabs if missing, and Level1's first section dressed with them if not already) |
| `Assets/Settings/` | URP / 2D renderer assets, input actions (from the template) |

## Player

- `PlayerMovement` (on the Player root, with Rigidbody2D + CapsuleCollider2D 0.55 x 1.8):
  input is read in `Update`, physics applied in `FixedUpdate` by setting `linearVelocity`.
  Runs at one speed (`moveSpeed`, 6), on the ground and in the air. Jump height
  is set in world units; falling uses extra gravity, releasing Jump early cuts the jump short,
  and coyote time plus a jump buffer make jumps forgiving. Ground detection is
  `Rigidbody2D.IsTouching` with a normal-angle `ContactFilter2D`, so no ground layer is needed.
  Crouch (ground only) shrinks the capsule from the top to `crouchHeight` and slows the player;
  it stays crouched under a low ceiling until there is headroom (an upward `Collider2D.Cast`).
  Walking on the ground into a `PushableBox` (found with a short `Rigidbody2D.Cast` ahead) pushes
  it: the player is capped at the box's `PushSpeed` and the box is given the player's velocity.
  `IsGrounded`, `IsCrouching`, `IsPushing`, `FacingDirection`, `Velocity` and the speeds are public.
- `PushableBox` (on a crate, needs a dynamic Rigidbody2D with frozen rotation): only exposes
  `PushSpeed` and `Push`. Ground friction stops it when let go; boxes on top ride along.
- `HeroAnimator` (on the `Visual` child, a SpriteRenderer placed at the bottom of the collider)
  shows the drawn frames: the 8-frame run cycle advanced by distance travelled (`runCycleDistance`),
  the standing frame with a breathing squash, and one run frame in the air. Animations that aren't
  drawn yet are faked with squash and stretch (stretch rising, squash falling, snap-and-spring on
  take-off and landing, a squashed figure while crouching, a forward lean while pushing). It faces
  left by mirroring `Visual`'s scale. Sprites must have their pivot at the feet; run
  Tools > NoRegRats > Set Up Hero Sprite after replacing the hero frames.
- Sprite scale: art in `Assets/Art/` imports at 870 pixels per unit, the hero's drawing scale
  (1570 px tall = 1.8 units). Cut-outs from photos are smaller, so CombatSetup sizes them by
  height instead (rat 1.15 units, paper ball 0.35).
- `PlayerShooter`: Attack (Space) fires a pooled `Projectile` (PaperBall: 14 u/s, 6 units range,
  1 damage) every 0.35 s from `muzzleOffset`, low enough to hit rats, and calls `HeroAnimator.Recoil`.
- `PlayerHealth` reacts to the hero's `Health` (3 hearts, 1 s invulnerability): knockback through
  `PlayerMovement.Knockback` (input ignored meanwhile), and on death the controls stop, the death
  is counted in `GameSession` and `ScreenFader.ReloadScene` fades out, reloads the level and fades
  in. Reloading is the respawn: every rat (and later the Rat King) comes back at full HP. Only
  `GameSession` survives (death count, `HasHeart`); with the Heart, `PlayerHealth` adds
  `heartBonus` to max hearts on start. `GameSession.StartNewRun()` clears it for a fresh run.
  `DamageFlash` tints the hero red and blinks it while invulnerable and after dying, so the last
  hit looks like any other: on death `PlayerMovement.InputLocked` only ignores the controls, and
  the hero still slows down and lands until the level fades out.

## Level1

- Built once by `Level1Builder`; from then on it's edited by hand in Unity (one person at a time).
  Objects are prefab instances grouped under `Level/Subway`, `Level/Shaft`, `Level/Sewers`,
  `Level/Arena`, so pieces and whole sections can be moved, added or swapped in the editor.
- The first section (group `Level/Subway`) is dressed as a street by `StreetDressing`: sidewalk
  slabs and the road strip over the floor `Block`, and the team's props as the platforms.
- Coordinates: street (subway group) floor top y = 0, sewer floor top y = -20. Route: start x 2
  → small floating chunk (x 6) → power box (x 12, top 2.78, Plain rat on top) → hydrant → wide
  floating chunk (x 21, 2.9) → double chunk (x 27.5, 5.0), Red-cap below → crate puzzle (the big
  power box at x 40–43.8 is 4.2 high, above the 3.5 jump; push the dumpster (1.85 high) against
  it; the hydrant at x 31.3 stops it being pushed too far back) → hydrant, Plain rat → chunk with
  pipe (x 54, 3.0) → "SEWERS" shaft x 62–66 → Jacket
  rat → crouch gap under pipes at x 84–90 (1.4 high) → AC-unit steps → Jacket rat on a brick ledge
  → arena x 112–134, y -20 to -10 → exit hole at the bottom of the arena's right wall (x 134–136,
  y -20 to -16) behind the exit grate, with the `LevelExit` trigger in it.
- Boss fight (`Level/Arena/Boss Arena`, `BossArena`): its trigger covers the arena floor past the
  entrance. On entry the `Entrance Gate` drops (`SlidingGate.Close`), the camera locks on
  `Camera Spot`, the `Rat King` starts and `HUD`'s `BossHealthBar` shows. Beating him drops the
  Heart; taking it (`HeartPickup.Collected`) opens the `Exit Grate` and unlocks the camera;
  walking out through `LevelExit` calls `GameSession.Win()` (raises `GameSession.GameWon`) and
  stops the controls. After a death with the Heart already taken, the King stays gone and the
  exit starts open.
- The hero can jump 3.5 up and about 4.5 across; keep new gaps and steps within that.
- Camera (`CameraFollow2D`): looks ahead in the facing direction and down while falling fast,
  stays inside `bounds` (Level1: x -6..136, y -24..14), and `LockTo(center)` / `Unlock()` hold it
  on a spot (for the arena).
- Background: one tiled `GraphPaper` sprite with `ParallaxBackground` (drifts at 0.6 of the
  camera's movement, tint by depth: blue-grey subway, green-grey sewers). It must stay big
  enough to cover the view everywhere (110 x 44 units now). Above ground, `ParallaxBackground`
  covers it at runtime with a white sky and the city's `Far`, `Mid` and `Near` layers (the two
  pictures side by side, loaded from `Resources`, so none of it is in the scene). Each layer has
  its own `parallax` (0.9, 0.75, 0.55), so the nearest slides fastest. The rock blocks hide the
  city from underground, where the paper still shows.

## Combat and enemies

- `Health` is shared by everything that can be hurt: `TakeDamage(amount, sourcePosition)`,
  `Heal`, `SetMax`, `Restore`, and `Damaged` / `Died` / `Changed` events that other components
  react to. Enemies use 0 invulnerability.
- `Projectile` needs a trigger collider and a Dynamic Rigidbody2D with gravity 0. It ignores its
  owner and other triggers, damages the first `Health` it touches and despawns on any solid hit.
- `RatEnemy` patrols at `walkSpeed`, turning at walls, ledges and `patrolRange`; hit = knockback
  and a short stun; death = shrink-and-fade poof, then the object is deactivated. Rats ignore
  collisions with the hero (`Physics2D.IgnoreCollision`); `ContactDamage` bites by distance check.
  Its single frame waddles (bob and tilt) on the `Visual` child, which is mirrored to face the
  walking direction. Rat types are the same setup with different Health / ContactDamage / speed.
- `RatKing` idles until `StartFight()`, then walks toward the hero and every `throwInterval` (2 s)
  stops, squashes for `windUpTime` (the telegraph) and throws a `TrashProjectile` on an arc that
  lands on the hero (higher arcs for far throws). Trash flies through platforms, only hurts the
  hero (1 heart), and vanishes just below the hero's height if it misses. Bite through
  `ContactDamage` (2). On death: shrink-and-fade, the Heart drops, `Defeated` is raised.
  The sketch faces left, so `artFacesRight` is off.
- `HeartPickup` (trigger): `PlayerHealth.GainHeart()` (+1 max heart, refilled),
  `GameSession.HasHeart = true`, raises the static `Collected` event.

## UI

- Level1's `HUD` canvas (screen-space overlay, scales from 1920 x 1080; built by `HudSetup`):
  `Hearts` (top left, `HeartsDisplay`: one icon per max heart, "+1 heart!" when the max rises),
  `Goal` (top centre, `GoalText`), `Boss Bar` (bottom centre, `BossHealthBar`), `Speech`
  (`SpeechBubble`: one line at a time above a world object, follows it on screen), and the
  `Title Menu`, `Pause Menu`, `Win Screen` panels (components on the HUD root). An `EventSystem`
  with `InputSystemUIInputModule` (project-wide UI actions) drives the buttons. The HUD has no
  `GraphicRaycaster` in the scene, so `TitleMenu` adds one at runtime; without it the mouse can't
  reach any menu. Test menus with real clicks, not by calling `onClick` from code.
- Goal steps: "Get to the sewers" (start) → "Find the Rat King" (`Sewers Goal` trigger) →
  "Defeat the Rat King" → "Take the Heart" → "Escape through the grate!" (set by `BossArena`).
- Lines: the start's `Joke Sign Line` (`SpeechTrigger` on the SUBWAY sign) and the Rat King's
  fight-start and defeat lines (`BossArena`). All are editable text fields in the Inspector.
- `GamePause.Freeze()` / `Unfreeze()`: time scale 0/1 and the Player action map off/on. Used by
  the title (shown once per launch, not after deaths or restarts), pause (Esc) and win screens.
  Restart and Play Again call `GameSession.StartNewRun()` then `ScreenFader.ReloadScene()`.
- Text uses Unity's built-in font until a font is put in `Assets/Art/Fonts/`;
  `HudSetup` picks the first one there for UI it creates. Existing Text components keep their
  font, so after adding one, set it on the HUD's Text components (or select them all and change
  it at once).

## Effects and audio

- `PuffSpawner` (scene object "Effects"): `PuffSpawner.Spawn(position, size, velocity, color)`
  and `Burst(position, count, size, speed, color)` from anywhere; a fixed pool reuses the oldest
  puff, so nothing is allocated during play. Used for the hero's dust (`HeroEffects`: take-off,
  landing, turning, every stride), shot impacts, rat and Rat King poofs, trash hits, the Heart.
- `CameraFollow2D.ShakeActive(strength, duration)`: hard landings (falling 15+ u/s), the hero
  getting hurt, the Rat King's defeat. A weaker shake never cuts a stronger one short.
- `Sfx.Play(Sfx.Sound.X)` (scene object "Audio/Sound Effects"): Jump, Land, Step, Shoot, Hit
  (a shot that does damage), RatPoof, Hurt, KingThrow, KingDefeated, Heart, Gate. Each sound has
  a clip list (a random one plays, slightly random pitch) and a volume in the Inspector; empty
  lists are silent. `Music` ("Audio/Music") plays the level track and switches to the boss
  track during the fight (`Music.PlayBoss` / `PlayLevel`, short fade). No clips are assigned yet.
