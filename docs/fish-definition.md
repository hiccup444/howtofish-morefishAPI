[Documentation index](../README.md)

# FishDefinition reference

Every property you can set when registering a fish. Only `Id` and one of
`Prefab` or `Mesh` are required.

## Identity

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Id` | string | required | Unique key. Namespace it, `you.yourmod.thing`. Drives the item id, so it must stay stable across releases |
| `DisplayName` | string | falls back to `Id` | Shown in game |
| `Template` | string | config default | Which of the game's creatures to build on. See [Templates](templates.md) |

`Id` is not cosmetic. The item id written into save files is derived from it, so
renaming an id in a later version orphans every one already saved. Treat it as
permanent once published.

## Appearance

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Prefab` | GameObject | none | Your authored visual. Used exactly as made |
| `Mesh` | Mesh | none | A bare mesh, used when no `Prefab` is given |
| `Material` | Material | none | Replaces materials outright. **Mesh path only** |
| `Texture` | Texture | none | Copies the game's material and swaps the texture. **Mesh path only** |
| `PreviewScale` | float | inherit | Size of the inventory and journal preview |
| `MeshScale` | float | 1 | Multiplier |
| `MeshRotation` | Vector3 | zero | Extra rotation in degrees |
| `AutoFit` | bool | true | Scale and orient the mesh to match the template |
| `KeepTemplateSkin` | bool | false | Keep the template's skin preset |

**`Prefab` and `Mesh` behave differently.** With `Prefab`, your transform is used
as authored and `AutoFit` does nothing. With `Mesh`, the API scales and rotates
it to match the template, which is convenient but a guess. Prefer `Prefab`.

**`Material` and `Texture` are ignored when you supply a `Prefab`.** Put the
material on the prefab's own renderer instead, which is where it is read from.
Nothing warns you if you set them anyway.

## Value

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Worth` | int | 10 | Base sell value |
| `WorthMin` / `WorthMax` | int | unset | Convenience, averaged into `Worth` |
| `FoodValue` | int | inherit | Fullness restored when eaten |
| `HealValue` | int | inherit | Health restored when eaten |

The game already varies sale price per catch. `Creature` rolls a Gaussian
multiplier around 1, clamped to roughly 0.25x to 1.75x, seeded so every player
agrees on a given fish's value. You set the centre, not the spread.

`WorthMin` and `WorthMax` only average into `Worth`. They do not widen the
spread. If you want a specific value, just set `Worth`.

## Behaviour

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Health` | int | inherit | Hit points |
| `Endangered` | bool? | inherit | The endangered tag and its score bonus |
| `Hostile` | bool? | inherit | See below |
| `FlopIntervalMin` | float | inherit | Shortest gap between flops on land |
| `FlopIntervalMax` | float | inherit | Longest gap between flops |
| `FlopStrength` | float | inherit | How hard it throws itself about |
| `SinkForce` | float | inherit | How strongly it pulls toward water |
| `AllowDrip` | bool | false | Allow the game's shiny variant |
| `HeadPosition` | float | auto | Where a headshot registers, along the body |

Anything left unset **inherits from the template**, and templates vary wildly.
Piranha has 5 health, Blobfish has 1000. Needlefish is flagged endangered.
If you do not set `Health` you get whatever your template has.

`Hostile = false` works and neutralises an attacking template. **`Hostile = true`
does not work** and only logs a warning. Hostility is part of the game's
`AttackingFish` type and cannot be added at runtime, so a hostile fish means
choosing a hostile template. See [Limits](limitations.md).

`AllowDrip` defaults to false because the shiny variant swaps in a rainbow
shader that looks wrong on most custom art.

`HeadPosition` is derived from your mesh if you leave it alone. It drives both
the headshot damage bonus and the Headshot score bonus, so a fish much longer or
shorter than its template should be set by hand.

## Where it appears

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Rarity` | float | 1 | Relative to an average catch on that bait |
| `BaitFilter` | string | all baits | Comma separated bait names, substring matched |
| `InJournal` | bool | true | List it in the journal |

`Rarity` is relative, not absolute. `1` is roughly as common as a typical vanilla
catch on that bait, `0.2` about a fifth as likely. It is computed against each
bait's own average because the game sums all weights and rolls, so an absolute
number would mean something different on every bait.

Bait names are logged at startup so you do not have to guess them.

## Read back after registering

| Property | Type | Meaning |
| --- | --- | --- |
| `Live` | bool | Whether it actually made it into the game |
| `RuntimeItemId` | byte | The id it was assigned this session |
| `RuntimeMaterial` | Material | The material previews use |
| `SourcePlugin` | string | Which assembly registered it, detected automatically |

`Register` returns `false` if your arguments were invalid, or if you registered
too late for the fish to ever be built. It returns `true` before anything is
actually built, so **`Live` is still what tells you your fish is really in the
game.** Subscribe to `MoreFish.OnFishRebuilt` and check it.

These four have internal setters, so you can read them but not assign them.

## Static API

| Member | Purpose |
| --- | --- |
| `Register(FishDefinition)` | Register a fish |
| `Register(id, name, mesh, worth, rarity)` | Short form for a bare mesh |
| `RegisterFromBundle(id, name, bundle, meshName, worth, rarity)` | Load a mesh from a bundle and register |
| `LoadBundle(path)` | Load and cache an AssetBundle |
| `Get(id)` / `IsRegistered(id)` / `All` / `RegisteredCount` | Query |
| `ForceNextCatch(id)` / `ClearForcedCatch()` / `IsForcing` | Testing, see [Testing](testing.md) |
| `Ordered()` | Registered fish, sorted the way ids are assigned |
| `OnFishRebuilt` | Fires once after fish are built |
| `IsAvailable` | For reflection users checking the type resolved |

## Next

* [Templates](templates.md) for what each template gives you
* [Testing your fish](testing.md)
