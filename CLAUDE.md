# NoRegRats

Unity 6 project (editor 6000.6.5f1), C#. A 2D side-scrolling metroidvania ("No regRats") for the
Game Development Methodologies course. Team: Tigran Tokatyan, Srbuhi Mashinyan, Robert Sargsyan,
Samvel Sardaryan.

## The game

- Hand-drawn hero (the team artist's drawings) in an "on-paper" style; enemies are rats in street
  clothes, led by the Rat King. The level runs from a city street through a subway station down
  into the sewers.
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
street → drop into the subway station → drop into the sewers → Rat King fight → take the Heart →
escape → You Won. Losing all 3 lives shows You Lost.

Done: hero art and animation, movement (run, jump, crouch, push), bare-handed punching in combos,
health, 3 lives and respawn at 4 checkpoints, three rat types with telegraphed attack patterns,
the Rat King (patterns for him are next), the Heart item, the level, the street and
subway art, HUD (hearts, goal, boss bar, speech bubbles), the full-screen lives count, the drawn title / pause /
You Won / You Lost screens, changeable keys (Controls on the title and in the pause screen), dust
and camera-shake effects, sound effects and music (free packs, see `Assets/Audio/CREDITS.md`).

Still open (ask Robert who is taking what before starting one):

| Task | What it needs | How it plugs in |
|---|---|---|
| Brass knuckles | Item art; then code: picking them up raises punch damage | Not started. `PlayerPunch`'s `damage` is the value to raise |
| Pause button drawing | A PNG of the in-game pause button, saved over `Assets/Art/UI/Menus/PauseButton.png` | The current one is a stand-in drawn to match the menu buttons |
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

The current sounds and music are in `Assets/Audio/SFX/` and `Assets/Audio/Music/` and already
assigned in Level1 and MovementTest. To swap or add a sound:

1. Put the file in `Assets/Audio/SFX/` or `Assets/Audio/Music/` (WAV or OGG), and add its source
   to `Assets/Audio/CREDITS.md`. Only use free sounds that need no credit (CC0 or similar).
2. Open the scene, select `Audio/Sound Effects`, and in the `Sfx` component drag the file into
   the Clips list of the matching sound. Several clips for one sound = a random one plays each
   time. Adjust Volume per sound. New sounds go at the end of the `Sfx.Sound` list.
3. Select `Audio/Music` and set Level Track, Boss Track and Menu Track.
4. Do this in both scenes that should have sound (Level1, MovementTest).

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

### Stand-in art (for the artist)

Pieces made in code from the artist's own drawings because nothing was drawn for them yet. Each
can be replaced by saving a drawing over the file with the same name (same proportions, transparent
background):

| File | What it is | Made from |
|---|---|---|
| `Assets/Art/UI/Menus/PauseButton.png` | In-game pause button (top right) | Drawn to match the menu buttons |
| `Assets/Art/UI/Menus/Title/Controls.png` | CONTROLS button on the title | CONTINUE's box; letters from START and CONTINUE (the L is CONTINUE's E without two arms) |
| `Assets/Art/UI/Menus/Controls/Reset.png`, `Return.png` | RESET / RETURN buttons on the controls window | START's box; letters from START and CONTINUE |
| `Assets/Art/UI/Menus/Controls/Key.png` | The box drawn round each key name | START's box, emptied |
| `Assets/Art/UI/Menus/Controls/Arrow.png`, `Run`, `Jump`, `Crouch`, `Pause`, `Punch` | Words and arrow of the controls window | Cut from `Controls.png` (the "PuNch" line was built from its letters) |
| `Assets/Art/UI/Menus/Lives/One.png`, `Two`, `Three`, `Times`, `Strike` | Digits, "×" and the red strike-out on the lives screen (white, tinted in Unity) | Strokes from `Controls.png`: the l of Ctrl, the C of Ctrl mirrored, the / of A/D, the arrow's shaft |
| `Assets/Art/UI/Menus/Lives/Hero.png` | The hero on the lives screen | A copy of `Hero_Idle.png` |
| `Assets/Art/Level/Checkpoint.png`, `CheckpointTick.png` | Checkpoint sign (post and board) and the tick that appears on it | The l of Ctrl as the post, START's box (taller) as the board, two slashes as the tick |
| `Assets/Art/UI/Alphabet/A.png` … `Z.png`, `0.png` … `9.png`, `Slash.png` | The hand-drawn capitals used for key names (key boxes and the start wall); white, tinted in Unity; capitals 200 px tall | Cut from the menu lettering (START, CONTINUE, RESTART, QUIT, EXIT, You Won, You Lost the Game, the stats labels): A C D E G H I L M N O Q R S T U W X Y. Built from those strokes: B (P with a second bowl), F (E), J (U), K (L's stem and half the X), P (R), V (top of the Y), Z and the digits (the "=" bars, a W stroke, C and O pieces). The slash is from `Controls.png` |
| `Assets/Art/Level/StartWall/StartWall.png` | The brick wall at the start of the street (5 x 4.6 units, 160 px per unit) | Drawn in the street props' style: thin wobbly ink lines, flat muted fills, darker edge strips, a few hatch marks, a concrete cap like the power box's |
| `Assets/Art/Level/StartWall/Run.png`, `Jump`, `Crouch`, `Punch`, `Pause`, `Arrow` | The painted words and arrow on the start wall | White copies of `UI/Menus/Controls/` |

Also still wanted: a hand-drawn font for the longer texts (stats and goal text use Unity's default
font), a full hand-drawn alphabet and digits 0–9 to replace the stand-in one, and, for the enemies, wind-up or lunge poses for Rat1 and Rat3 and a
"gun raised high" pose for Rat2 (their attacks currently reuse the existing frames plus squash and
a tilt). The Checkpoint sound is the Heart's chime for now.

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
  (e.g. `InputSystem.actions.FindAction("Player/Jump")`). Player map: Move (A / D, left stick),
  Jump (Space / gamepad South), Attack (K / left mouse / gamepad West), Crouch (S / gamepad East),
  Interact, Previous, Next, Look. Sprint still exists in the asset but nothing reads it (the hero
  has one move speed). UI map: the template's actions plus Pause (Esc / gamepad Start); it stays
  on while menus switch the Player map off. Each control has exactly one keyboard key, and those
  keys are the player's to change (`KeyBindings`, see "UI"); the asset holds the defaults. Don't
  add a second keyboard binding to Move left / right, Jump, Attack, Crouch or Pause:
  `KeyBindings` only knows about the first.

## Layout

| Path | Contents |
|---|---|
| `Assets/Scenes/Level1.unity` | The prototype level (first in Build Settings): street → subway station → shaft → sewers → boss arena. See "Level1" below |
| `Assets/Scenes/MovementTest.unity` | Sandbox: ground, platforms, pushable crates, a tall block, a low ceiling, a crawl tunnel, one test rat of each type ("Test Rat", "Test Red-cap", "Test Jacket"). Second in Build Settings |
| `Assets/Prefabs/Level/` | Level pieces: `Checkpoint` (the checkpoint sign; see Level1), `Block` (dark ink rectangle, scale it), `Crate` (pushable), `TrainCar`, `StoneBridge`, `BrickLedge`, `ACUnit`, `Pipe`, `ScaffoldBar` (art with colliders on the solid part), `Sign_Sewers`, `Sign_Subway` (no collider), `Gate` (tiled bars + `SlidingGate`) |
| `Assets/Prefabs/Level/Street/` | The team's street art as prefabs: `Sidewalk` (slab; swap its sprite between Ground1–4), `Road`, `SidewalkChunk_Small` / `_Pipe` / `_Double` / `_Wide` (floating platforms), `FireHydrant`, `PowerBox` (solid props), `Dumpster` (pushable). Pivots sit on the walking line or base, so place them by where you stand |
| `Assets/Prefabs/Level/Subway/` | The team's subway ("Level 2") art as prefabs: `Platform` (the tiled platform floor, no collider), `PlatformTile_Single` / `_Double` / `_Triple` (floating platforms), `FireExtinguisher`, `WetFloorSign`, `RecycleBins`, `PhoneBooth` (solid props). Pivots on the walking line or base, as for the street |
| `Assets/Prefabs/Player.prefab` | The player |
| `Assets/Prefabs/Combat/` | `PaperBall` (the old shot, unused since the hero punches), `Trash` (the Rat King's) |
| `Assets/Prefabs/Enemies/` | `Rat1` (the chain rat, the regular rat: 3 HP, bite 1, speed 1.8), `Rat3` (the jacket machete rat: 4 HP, bite 2, speed 1.3), `Rat_Gunner` (the gun rat, Rat2: 3 HP, bite 2, bullet 2, shoots), `RatKing` (8 HP, bite 2, trash 1) |
| `Assets/Prefabs/Items/` | `Heart` (dropped by the Rat King) |
| `Assets/Scripts/Player/` | `PlayerMovement`, `HeroAnimator`, `PlayerHealth`, `PlayerPunch`, `HeroEffects` |
| `Assets/Scripts/Combat/` | `Health`, `DamageFlash`, `Projectile`, `ContactDamage` |
| `Assets/Scripts/Enemies/` | `RatEnemy`, `RatFighter` (a rat's attacks), `RatAnimator`, `RatShooter`, `EnemyBullet`, `RatKing`, `TrashProjectile` |
| `Assets/Scripts/Items/` | `HeartPickup` |
| `Assets/Scripts/Level/` | `GameSession` (lives, what survives a death, the last checkpoint, the win or loss, run time), `Checkpoint`, `ParallaxBackground`, `BossArena`, `SlidingGate`, `LevelExit`, `GoalTrigger`, `SpeechTrigger` |
| `Assets/Scripts/UI/` | `ScreenFader` (fade out, reload the level, fade in), `BossHealthBar`, `HeartsDisplay`, `LivesScreen` (the full-screen lives count), `DrawnAlphabet` and `DrawnText` (the hand-drawn capitals), `GoalText`, `SpeechBubble`, `TitleMenu`, `PauseMenu`, `WinScreen`, `LoseScreen`, `MenuButtonFeedback`, `GamePause` (freeze behind menus), `KeyBindings` (the player's keys: change, save, reset), `KeyButton` (one key box on the controls window), `ControlsSheet` (the controls window: prompt, Reset) |
| `Assets/Art/` | Drawn art. `Hero/`: `Hero_Idle.png`, `Hero_Run_1..8.png`, `Hero_Punch_1.png` (guard) and `Hero_Punch_2.png` (arm out), and `Hero_Hurt.png` (originals at full size; imported capped at 512 px). `Rats/Rat1/`, `Rats/Rat2/` and `Rats/Rat3/` (jacket machete rat, `Rat3_Anim_1..5`, `Rat3_Standing`, `Rat3_Hit`): the team artist's chain rat (`Rat1_Anim_1..8`, `Rat1_Standing`, `Rat1_Hit`) and gun rat (`Rat2_Standing`, `Rat2_Shoot_1..4`, `Rat2_Hit`, `Bullet`); both drawn facing left, full-size originals (imported capped at 512 px; Rat1 is 1.8 units tall on its canvas and Rat2 1.65, feet on the origin). `Rats/`: `Rat_Plain.png`, `Rat_RedCap.png`, `Rat_Jacket.png`, `RatKing.png`, cut out of the paper-prototype sketch photos (placeholders; replace with same-named PNGs). `Items/Heart.png` (cut out). `Placeholder/`: `PaperBall.png`, `Trash.png`, `Bars.png` (tiles vertically, 1 x 4 units), `Puff.png` (scribble puff, 1 unit). `Level/`: level pieces cut out of the sketch photos (the train car is one drawn end mirrored; the two signs were redrawn because their light grey can't be separated from the grid), and `Checkpoint.png` / `CheckpointTick.png` (stand-ins, see "Stand-in art"; imported at full size, not capped at 512). `Street/`: the team artist's street pieces (Ground1–4 sidewalk slabs, Street road strip, Tile1/1_2/2/3 floating sidewalk chunks, FireHydrant, GarbageBin, PowerBox), sized from their mockup. `Background/GraphPaper.png`: generated, tiles at 1 cell = 1 unit. `Background/Resources/`: `City1_*.png` / `City2_*.png`, the team's two skylines split into `Far`, `Mid` and `Near` layers with transparent backgrounds (replace with same-named PNGs). `Subway/`: the team artist's subway pieces (Ground platform floor, Tile1–3 floating tiles, FireExtinguisher, WetFloorSign, RecycleBin, PhoneBooth), sized from their mockup. `UI/`: `HeartFull.png` / `HeartEmpty.png` (made from the Heart cut-out), `SpeechBubble.png`; `UI/Menus/`: the team's menu drawings, cut from their mockups: `Title/` (Title, Start, Continue, Exit, Character, and `Controls.png`, made from CONTINUE's box and letters cut from START and CONTINUE, the L being CONTINUE's E without two arms), `Window.png` (the frame shared by pause / won / lost / controls), `Restart.png`, `Quit.png`, `Controls.png` (the artist's controls sheet; its last line, "SPACE → Punch", is built from letters cut from the other four; no longer shown whole, its words were cut into `Controls/`), `Lives/` (the lives screen: `Hero.png`, a copy of `Hero_Idle` at UI size; `One`, `Two`, `Three`, `Times`, `Strike` built from the controls sheet's strokes: the "l" of Ctrl, the C of Ctrl mirrored, the "/" of A/D and the arrow's shaft; white so Unity can tint them; replace with same-named drawings), `Controls/` (the controls window's pieces: `Arrow`, `Run`, `Jump`, `Crouch`, `Punch`, `Pause` cut from `Controls.png`; `Key.png`, the START box emptied, stretched round each key name (its frame is the sprite border); `Reset.png` and `Return.png` in the START box, from START's and CONTINUE's letters), `PauseButton.png` (stand-in for the in-game pause button), `Paused/Continue.png`, `YouWon/` (YouWon, Stats labels, PlayAgain, Quit), `YouLost/YouLost.png`. `Fonts/` (not there yet): put the hand-drawn font here; see "UI" |
| `Assets/Scripts/Objects/` | `PushableBox` |
| `Assets/Scripts/Effects/` | `PuffSpawner` (pooled dust / paper-scrap puffs), `Sfx` (sound effects), `Music` (menu, level and boss tracks) |
| `Assets/Scripts/Camera/` | `CameraFollow2D` (on the Main Camera) |
| `Assets/Sprites/Placeholder/` | Generated stand-in art (crate, white square used for platforms) |
| `Assets/Physics/` | `NoFriction.physicsMaterial2D` (on the player so it never sticks to walls) |
| `Assets/Audio/` | `SFX/` (sound effects), `Music/` (menu, level and boss loops), `CREDITS.md` (where each file came from) |
| `Assets/Editor/` | Editor-only scripts. `ArtImportSettings` (first-import sprite settings for `Assets/Art/`), `HeroSetup` (Tools > NoRegRats > Set Up Hero Sprite: the hero frames' pivots and the HeroAnimator's idle, run, punch and hurt frames), `CombatSetup` (Tools > NoRegRats > Set Up Combat: sprite sizes, PaperBall and rat prefabs if missing (stats in its `RatTypes` table), the Player's combat components, the test rats), `MovementTestSetup` (Tools > NoRegRats > Set Up Push And Crouch Test), `Level1Builder` (Tools > NoRegRats > Build Level1: creates missing level-piece prefabs and builds Level1 only if it doesn't exist; never overwrites it), `BossArenaSetup` (Tools > NoRegRats > Set Up Boss Arena: boss prefabs if missing, and the fight added to Level1's arena if not there yet), `HudSetup` (Tools > NoRegRats > Set Up HUD: Level1's UI, EventSystem, goal and joke-sign triggers if not there yet), `PolishSetup` (Tools > NoRegRats > Set Up Effects And Audio: HeroEffects on the Player, "Effects" and "Audio" objects in both scenes if missing), `StreetDressing` (Tools > NoRegRats > Dress Level1 Street: street prefabs if missing, and Level1's first section dressed with them if not already), `PunchSetup` (Tools > NoRegRats > Set Up Punching: PlayerPunch on the Player instead of the old shooter, the Rat King's stop distance), `MenuSetup` (Tools > NoRegRats > Set Up Menus: the drawn title / pause / won / lost screens, the title's Controls button and Controls screen, the key boxes in the controls windows, the full-screen lives count, the hearts made 1.6x bigger (removing the old "Lives: 3" label), the HUD pause button (and removes the old corner controls card), replacing the old plain menus if the drawn ones aren't there and the old fixed picture of the controls with key boxes), `SubwaySetup` (Tools > NoRegRats > Build Subway Station: subway prefabs if missing, and the station added between the street and the sewers if not there yet, moving everything after it right by 56), `EnemyAttackSetup` (Tools > NoRegRats > Set Up Enemy Attacks: RatFighter on Rat1 and Rat3 with their frames and attack settings if it isn't there, and their standing frames), `CheckpointSetup` (Tools > NoRegRats > Set Up Checkpoints: the Checkpoint prefab if missing, the four checkpoints under Level1's `Level/Checkpoints` if not there yet, the Checkpoint sound), `AlphabetSetup` (makes `Assets/Art/UI/Alphabet/Alphabet.asset` from the glyph pictures; used by the next two), `StartWallSetup` (Tools > NoRegRats > Build Start Wall: the brick wall with the painted controls at the start of the street if it isn't there, removes the old corner controls card, gives the menus' key boxes the drawn capitals, moves the first checkpoint sign past the hero's start) |
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
  the standing frame with a breathing squash, and one run frame in the air. A punch plays the drawn
  punch as in the artist's animation (guard 0.05 s, arm out 0.1 s, guard 0.05 s; each drawing
  shows for at least one frame even if the game hitches), and a hit shows the drawn hurt pose for
  `hurtTime` (0.35 s) and while dead. After a combo (`CatchBreath`), standing still shows the
  guard drawing, a little lowered and panting, until the hero can punch again. The hurt pose wins over a punch, and both go over running,
  jumping and standing. Animations that aren't drawn yet are faked with squash and stretch (stretch rising, squash falling, snap-and-spring on
  take-off and landing, a squashed figure while crouching, a forward lean while pushing). It faces
  left by mirroring `Visual`'s scale. Sprites must have their pivot at the feet; run
  Tools > NoRegRats > Set Up Hero Sprite after replacing the hero frames.
- Sprite scale: art in `Assets/Art/` imports at 870 pixels per unit, the hero's drawing scale
  (1570 px tall = 1.8 units). Cut-outs from photos are smaller, so CombatSetup sizes them by
  height instead (rat 1.15 units, paper ball 0.35).
- `PlayerPunch`: Attack (K by default) punches bare-handed every 0.3 s, in combos of 3
  (`comboLength`): the third punch knocks what it hits 1.8x further (`finisherKnockback`) with a
  bigger shake and puff, then the hero can't punch for 0.7 s (`recoveryTime`), so mashing can't
  stun-lock rats (their stun is 0.25 s) and they get to bite back. A gap of more than 0.6 s
  (`comboGap`) between punches starts the count again, so spaced punches never tire the hero.
  Misses count. Each punch: everything with a `Health` in a
  box `reach` (0.9) deep in front of the hero's collider, at fist height (low enough for rats),
  takes `damage` (1). Every punch calls `HeroAnimator.Punch` (the drawn punch). A miss puffs at
  the drawn fist (`drawnFist`) and plays Punch; a hit puffs where the fist meets what it hit and
  plays Hit with a tiny camera shake. The hit box reaches past the drawn fist so the Rat King can
  be hit from where he stops. It uses an
  overlap check, so rats, which pass through the hero, are hit too. Brass knuckles (later) are
  meant to raise `damage`.
- `PlayerHealth` reacts to the hero's `Health` (3 hearts, 1 s invulnerability): knockback through
  `PlayerMovement.Knockback` (input ignored meanwhile), and on death the controls stop and
  `GameSession.RecordDeath()` takes one of the run's 3 lives. With lives left,
  `ScreenFader.ReloadScene` fades out, reloads the level and fades in, and the lives screen shows
  the count going down. Reloading is the respawn: the hero starts at the last checkpoint reached
  (see "Checkpoints" under Level1) with full hearts, and every rat and the Rat King come back at
  full HP. With none left, `GameSession.Lose()` raises
  `GameLost` and the You Lost screen shows. Only `GameSession` survives a reload (lives, death
  count, `HasHeart`, `Checkpoint`); with the Heart, `PlayerHealth` adds `heartBonus` to max hearts on start.
  `GameSession.StartNewRun()` clears it for a fresh run.
  `DamageFlash` tints the hero red and blinks it while invulnerable and after dying, so the last
  hit looks like any other: on death `PlayerMovement.InputLocked` only ignores the controls, and
  the hero still slows down and lands until the level fades out.

## Level1

- Built once by `Level1Builder`; from then on it's edited by hand in Unity (one person at a time).
  Objects are prefab instances grouped under `Level/Subway` (the street, despite its name),
  `Level/Subway Station`, `Level/Shaft`, `Level/Sewers`, `Level/Arena`, so pieces and whole
  sections can be moved, added or swapped in the editor.
- The first section (group `Level/Subway`) is dressed as a street by `StreetDressing`: sidewalk
  slabs over the floor `Block` (the road strip was removed from Level1; the `Road` prefab is still there), and the team's props as the platforms.
- The subway station (group `Level/Subway Station`, added by `SubwaySetup`) sits between the
  street and the sewers: the tiled platform floor over a rock `Floor`, a rock `Ceiling`, the
  team's subway props and floating tiles.
- Coordinates: street floor top y = 0, station floor top y = -8 (ceiling -0.5), sewer floor top
  y = -20. Route: start x 2 → small floating chunk (x 6) → power box (x 12, top 2.78, Plain rat
  on top) → hydrant → wide floating chunk (x 21, 2.9) → double chunk (x 27.5, 5.0), Red-cap below
  → crate puzzle (the big power box at x 40–43.8 is 4.2 high, above the 3.5 jump; push the
  dumpster (1.85 high) against it; the hydrant at x 31.3 stops it being pushed too far back) →
  hydrant, Plain rat → chunk with pipe (x 54, 3.0) → "SUBWAY" hole x 62–66 down into the station
  → fire extinguisher (x 70), wet-floor sign (72.5), Plain rat → recycling bins (x 82, top -6.5)
  → double tiles (x 86.5 at -5, x 91 at -3 with an extinguisher) → phone booth (x 96, top -5.35,
  jumped over), Red-cap → single tile (x 101, -4.5) → triple tile (x 106.5, -5) → Rat3 →
  wet-floor sign, bins → "SEWERS" shaft x 118–122 → Rat3 → Rat1 (`Rat Thug`, x 134) → crouch gap under pipes at
  x 140–146 (1.4 high) → AC-unit steps → Rat3 on a brick ledge → Gun rat (`Rat Gunner`, x 163) → arena x 168–190, y -20 to
  -10 → exit hole at the bottom of the arena's right wall (x 190–192, y -20 to -16) behind the
  exit grate, with the `LevelExit` trigger in it.
- Boss fight (`Level/Arena/Boss Arena`, `BossArena`): its trigger covers the arena floor past the
  entrance. On entry the `Entrance Gate` drops (`SlidingGate.Close`), the camera locks on
  `Camera Spot`, the `Rat King` starts and `HUD`'s `BossHealthBar` shows. Beating him drops the
  Heart; taking it (`HeartPickup.Collected`) opens the `Exit Grate` and unlocks the camera;
  walking out through `LevelExit` calls `GameSession.Win()` (raises `GameSession.GameWon`) and
  stops the controls. After a death with the Heart already taken, the King stays gone and the
  exit starts open.
- Start wall (`Level/Subway/Start Wall`, built by `StartWallSetup`): a brick wall from x -3.9 to
  1.1 (just right of the level's dark left edge, ending before the hero's start at x 2), its
  bottom behind the sidewalk (sorting -1; the sidewalk is 1). The controls are written on it in
  black (like the controls sheet's ink) on a world-space canvas (`Paint`, 100 canvas units to a world unit, sorting 0): per row,
  the player's keys in the hand-drawn capitals (`DrawnText`, right-aligned), the artist's arrow and
  action word; each row tilted a little as if painted by hand. `ControlsWall` repaints the keys
  when they change (Run shows both keys, "A / D"). It replaced the old controls card in the HUD's
  corner.
- Checkpoints (`Checkpoint`, under `Level/Checkpoints`): four signs, one at the start of each part:
  0 the street where the run starts (x 2.7, just past the hero's start), 1 the subway station just past where you land
  (x 66.5, y -8), 2 the bottom of the shaft into the sewers (x 121, y -20), 3 just before the arena
  gate (x 166, y -20). Walking past a sign reaches it (`GameSession.Checkpoint`; a tick pops onto
  the board with a puff and the Checkpoint sound); reaching one doesn't refill hearts. After a
  death the hero starts at the furthest one reached, just right of the sign (`respawnOffset`, so
  it isn't hidden behind them), with full hearts; the camera jumps there (`CameraFollow2D.SnapActive`); every enemy within `safeRadius`
  (8) of it is removed, so a respawn never lands next to one (only the boss checkpoint has any:
  the gun rat and the machete rat on the ledge); and `respawnGoal` is shown (sewers and boss:
  "Find the Rat King"; not once the Heart is taken). The sign prefab is scaled 1.4 (about 2.2
  units, board above the hero's head) and drawn in front of backgrounds, behind props and
  characters (sorting order 1, tick 2). A new run (Start, Restart, Play Again) goes
  back to checkpoint 0. Signs further along can be added by placing the prefab with the next
  `index`.
- The hero can jump 3.5 up and about 4.5 across; keep new gaps and steps within that.
- Camera (`CameraFollow2D`): looks ahead in the facing direction and down while falling fast,
  stays inside `bounds` (Level1: x -6..192, y -24..14), and `LockTo(center)` / `Unlock()` hold it
  on a spot (for the arena).
- Background: one tiled `GraphPaper` sprite with `ParallaxBackground` (drifts at 0.6 of the
  camera's movement, tint by depth: blue-grey subway, green-grey sewers). It must stay big
  enough to cover the view everywhere (110 x 44 units now; `SubwaySetup` moved it right by 11.2
  when the level got 56 longer, which keeps both ends covered). Above ground, `ParallaxBackground`
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
- `RatEnemy` patrols at `walkSpeed`, turning at walls, ledges and `patrolRange`. If the wall or
  ledge it last turned at stands between it and home (a rat knocked off its perch), where it
  stands becomes home, so it patrols there instead of flipping round every step; walled in on
  both sides, it stands still. Hit = knockback and a short stun; death = shrink-and-fade poof, then the object is deactivated. Rats ignore
  collisions with the hero (`Physics2D.IgnoreCollision`); `ContactDamage` bites by distance check.
  Its drawing waddles (bob and tilt) on the `Visual` child, which is mirrored to face the
  walking direction. Rat types are the same setup with different Health / ContactDamage / speed
  / attacks. While a `RatFighter` fights, it drives the walking and facing (`Steer`) and squashes
  the drawing (`Pose`).
- `RatFighter` (on Rat1 and Rat3, added by `EnemyAttackSetup`) gives a rat real attacks. When the
  hero is within `noticeRange` and `noticeHeight` (feet to feet) it stops patrolling, faces them,
  closes in (never off a ledge) and attacks, then rests a random `restTime`. Each attack is picked
  at random by weight from `moves`: Strike, DoubleStrike (a second strike after a short wind-up),
  LungeStrike (dashes `lungeSpeed` for `lungeTime` first, so standing just out of reach isn't
  safe) and Feint (winds up, holds, doesn't strike: baits a punch). Every attack starts with a
  wind-up (`windUpTime`): the rat steps through its wind-up drawings, crouches (`windUpSquash`)
  and flashes `warningTint`, so it can be read. The strike hurts the hero once if they are within
  `reach` of the rat's front and `strikeHeight` of its feet (`damage` hearts). A punch cancels the
  attack and gives it a shorter rest. `ContactDamage` still bites on touch as before.
  `CurrentMove` says which attack is running.
- `RatAnimator` shows a rat's drawings: `standingFrame` while walking (waddled by RatEnemy), the
  frame RatFighter asks for (`ShowAttackFrame`) during an attack, and `hitFrame` when struck.
  Without a standing frame it loops `frames` all the time, as the rats did at first.
- `Rat1` (was `Rat_Thug`; replaced the plain and red-cap rats everywhere; the chain is its necklace, so it fights by hand) is a `RatEnemy` (3 HP, bite 1, speed 1.8) with a `RatAnimator` and a `RatFighter`: a claw swipe, `reach` 0.75, 1 heart, wind-up 0.35 s (`Rat1_Anim_2..5`, the claw raised), strike `Rat1_Anim_6..7`; mixes Strike 3 / DoubleStrike 2 / LungeStrike 2; notices the hero within 3. It stands in `Rat1_Standing` while walking and shows `Rat1_Hit` when hit. The drawing faces left, so the sprite has `flipX` on (RatEnemy mirrors the `Visual` child for the walking direction).
- `Rat3` (the jacket rat; replaced `Rat_Jacket`, whose prefab is now unused, as did `Rat_Plain`/`Rat_RedCap`) is a `RatEnemy` (4 HP, bite 2, speed 1.3: the old jacket rat's stats) with a `RatAnimator` and a `RatFighter`: an overhead machete slash with twice the swipe's reach (`reach` 1.6), 2 hearts, wind-up 0.45 s (`Rat3_Anim_1..3`, raising the machete), strike `Rat3_Anim_4..5`; mixes Strike 3 / DoubleStrike 2 / LungeStrike 2 / Feint 1.5; notices the hero within 4. Stands in `Rat3_Standing`. Art in `Rats/Rat3/`, faces left like Rat1.
- `Rat_Gunner` (Rat2) is a `RatShooter` (3 HP, bite 2, bullet 2 hearts): it faces the hero, and when the hero is within `sightRange` (11), within `sightHeight` (2.5) of its height and not behind a wall, raises the gun for `aimTime` (0.5 s, the warning) and fires `EnemyBullet`s (second shoot frame, `Sfx.Gunshot`), recoils, then waits `fireInterval` (1.8 s) give or take `intervalJitter` (30%). Each attack is picked by weight: a single low shot (`singleWeight` 3; the bullet flies 1 unit up: jump it), a burst of `burstCount` (3) shots `burstGap` (0.16 s) apart (`burstWeight` 2), or a high shot (`highWeight` 2): the drawing tilts up `highShotTilt` (14°) during the aim as the warning and the bullet flies `highShotLift` (0.5) higher, over a crouching hero (whose top is 1.2 up) and into a standing one: crouch under it. While the hero is within `backOffRange` (2.5) it backs away at `backOffSpeed` before it shoots again (never off a ledge or into a wall; cornered, it shoots anyway). A punch cancels the shot and staggers it. The bullet hurts only the hero, passes through rats and the King, and vanishes on walls or after its range. Tune everything in the Inspector.
- `RatKing` idles until `StartFight()`, then walks toward the hero, stopping `stopDistance` (1.7)
  away, just short of his bite, so the hero has to step in to punch him. Every `throwInterval` (2 s) he
  stops, squashes for `windUpTime` (the telegraph) and throws a `TrashProjectile` on an arc that
  lands on the hero (higher arcs for far throws). Trash flies through platforms, only hurts the
  hero (1 heart), and vanishes just below the hero's height if it misses. Bite through
  `ContactDamage` (2). On death: shrink-and-fade, the Heart drops, `Defeated` is raised, and any
  trash still in the air puffs away (`TrashProjectile.PuffAll`), so nothing can hurt the hero
  during the escape.
  The sketch faces left, so `artFacesRight` is off.
- `HeartPickup` (trigger): `PlayerHealth.GainHeart()` (+1 max heart, refilled),
  `GameSession.HasHeart = true`, raises the static `Collected` event.

## UI

- Level1's `HUD` canvas (screen-space overlay, scales from 1920 x 1080; built by `HudSetup`):
  `Hearts` (top left, `HeartsDisplay`: one icon per max heart, "+1 heart!" when the max rises;
  the whole group is scaled 1.6x by `MenuSetup`, and it's the only thing in that corner),
  `Goal` (top centre, `GoalText`), `Boss Bar` (bottom centre, `BossHealthBar`), `Speech`
  (`SpeechBubble`: one line at a time above a world object, follows it on screen), and the
  `Title Menu`, `Pause Menu`, `Win Screen`, `Lose Screen` panels (components on the HUD root),
  `Lives Screen` (`LivesScreen`, see below) and `Pause Button` (top-right, opens the pause menu
  during play). The controls are shown on the brick wall at the start of the street (see "Start
  wall" under Level1), not on the HUD. An `EventSystem`
  with `InputSystemUIInputModule` (project-wide UI actions) drives the buttons. The HUD has no
  `GraphicRaycaster` in the scene, so `TitleMenu` adds one at runtime; without it the mouse can't
  reach any menu. Test menus with real clicks, not by calling `onClick` from code.
- Goal steps (no goal text at the start): "Find the Rat King" (`Sewers Goal` trigger) →
  "Defeat the Rat King" → "Take the Heart" → "Escape through the grate!" (set by `BossArena`).
- Lines: the start's `Joke Sign Line` (`SpeechTrigger` on the SUBWAY sign) and the Rat King's
  fight-start and defeat lines (`BossArena`). All are editable text fields in the Inspector.
- The menus are the team's drawings (built by `MenuSetup`), placed as in the artist's mockups.
  Each button has `MenuButtonFeedback`: it grows and tilts while selected, squashes while held,
  plays Click, and hovering selects it, so mouse and keyboard / gamepad share one highlight.
  Navigation is up / down and skips buttons that can't be used.
- Title (`TitleMenu`): Start (a fresh run), Continue, Controls, Exit (closes the game). Shown on
  launch, after Quit on the pause screen (over the frozen run; Continue goes back into it), and
  after Quit on You Won / You Lost (the level is reloaded fresh behind it). Continue is greyed out
  when there's no run to go back to. Not shown after deaths or restarts. Controls opens the
  `Controls Screen` (the controls window and Return) over the title; Return or the pause key
  closes it.
- Changing keys (`KeyBindings`, `KeyButton`, `ControlsSheet`): the controls window (title's
  Controls screen and beside the pause menu) shows one drawn key box per control: Run left, Run
  right, Jump, Crouch, Punch, Pause (A, D, Space, S, K, Esc by default). Clicking a box (or
  Enter / Space on it) shows "?" and "Press a key"; the next keyboard key becomes that control's.
  Esc cancels (for the Pause key, which may be set to Esc, a click cancels instead). A key another
  control already has swaps the two. Reset puts the defaults back. Keys are saved in PlayerPrefs
  (`NoRegRats.KeyBindings`, binding overrides on the project-wide actions) and loaded before the
  first scene; mouse and gamepad bindings never change. Menus ignore a key for 0.25 s after it
  was picked, so choosing Esc or Space doesn't also close a menu or press a button. Key names come
  from the Input System and are shortened ("L Shift", "Esc", "Up"); each box widens to fit. They
  are written in the hand-drawn capitals (`DrawnText` with the `DrawnAlphabet` at
  `Assets/Art/UI/Alphabet/Alphabet.asset`: A–Z, 0–9 and "/", made from the artist's menu lettering,
  see "Stand-in art"); a name with a character the alphabet lacks (";", "?") shows in the plain
  font instead (`DrawnText`'s fallback Text).
- The HUD's pause button lights up on hover but is never selected (`MenuButtonFeedback`'s
  `selectOnHover` off, navigation None), so Space can't press it; `PlayerPunch` ignores a mouse
  click that lands on a HUD button, and leaving the pause menu clears the selection.
- Lives (`LivesScreen`, `Lives Screen`): the lives count is no longer in the corner; a full
  screen (the title's grey, the hero, "×" and the number) shows when a run starts (Start,
  Restart, Play Again, Start over a run) and after each death, with the game frozen. After a
  death the old number shows, a red stroke crosses it out and the new one pops in; the last life
  is a red 1 that shakes. It goes after 2 s, or on any key or click once the number is up. Not
  shown over the title, on Continue, or when the last life is lost (You Lost instead). It knows a
  death from a restart by comparing `GameSession.Lives` with the count it last showed. The
  digits, "×" and the strike are built from strokes on the artist's controls sheet (no drawn
  digits exist yet), as white ink tinted in Unity; the hero is `Hero_Idle`.
- Pause (`PauseMenu`, the pause key or the HUD button): Continue, Restart, Quit (to the title),
  with the controls window (keys can be changed there) beside it. The music holds while paused
  and carries on from the same spot on Continue (`Music.SetPaused`). You Won (`WinScreen`): time, deaths and hearts left written next to
  the drawn labels, Play Again, Quit (to the title). You Lost (`LoseScreen`, after the last life):
  Restart, Quit (to the title).
- `GamePause.Freeze()` / `Unfreeze()`: time scale 0/1 and the Player action map off/on, behind
  every menu. Restart and Play Again call `GameSession.StartNewRun()` then
  `ScreenFader.ReloadScene()`.
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
- `Sfx.Play(Sfx.Sound.X)` (scene object "Audio/Sound Effects"): Jump, Land, Step, Punch (a punch
  that hits nothing), Hit (a punch that lands), RatPoof, Hurt, KingThrow, KingDefeated, Heart,
  Gate, Click (menu buttons), Gunshot (for the shooting rat, `RatShooter`), Push (the scrape every 0.45 s while a box or bin is pushed, from `PushableBox`), Checkpoint (a checkpoint reached; for now the Heart's chime). Rat attacks use Punch for their swing. Sounds are saved by number in the scenes, so new ones go at the end
  of the list (Punch was Shoot). Each sound has
  a clip list (a random one plays, slightly random pitch) and a volume in the Inspector; empty
  lists are silent. `Music` ("Audio/Music", one AudioSource, so only one track plays at a time)
  plays the level track and switches to the boss track when the fight starts (`Music.PlayBoss`,
  short fade). The final punch on the Rat King cuts the music (`Music.Stop`, from `BossArena` on
  the King's `Health.Died`, with his explosion), and the escape is silent until the level
  reloads. While the title screen is up it plays the menu track (`Music.PlayMenu`, called by
  `TitleMenu`; `Music.ResumeGame` goes back to the level or boss track, or stays silent after
  the King). You Lost also cuts the music (`Music.Stop` from `LoseScreen`); Restart reloads the
  level with its music, Quit plays the title's. `Music.SetPaused` (the pause menu) pauses all audio through `AudioListener.pause`;
  the Sfx source ignores that, so menu clicks still sound. Level1's boss track is `Music/Boss_Paranoid.ogg` (Black Sabbath's "Paranoid",
  instrumental, streamed); it is copyrighted and only cleared for the course, see
  `Assets/Audio/CREDITS.md`. The free `Music/Boss.ogg` is the drop-in replacement for anything
  published. The clips are in `Assets/Audio/SFX/` and `Assets/Audio/Music/` (credits in `Assets/Audio/CREDITS.md`) and are assigned in Level1 and MovementTest. Click plays one of two clips, King defeated is two explosions joined into one file.
