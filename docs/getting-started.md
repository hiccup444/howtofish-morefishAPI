[Documentation index](../README.md)

# Getting started

Building your first custom fish, from an empty Blender scene to catching it in
game.

## What you need

* Blender, or any tool that exports FBX
* Unity, matching the game's version as closely as you can. The game is on
  **Unity 6000.4**. See [Making assets](making-assets.md) for why this matters
* A BepInEx plugin project, the same way you would build any other mod
* MoreFishAPI installed

## 1. Model the fish

Make it longest on **X**, nose pointing +X. If you get it wrong your fish stands on its tail.

Author at real size. One Unity unit is one metre, and the game's fish are small:
a Cod is 0.77m long, a Shrimp is 0.10m. Pick a size from
[Templates](templates.md) and match it.

Give the mesh UVs. A mesh without UVs renders as a flat single colour.

## 2. Build the prefab in Unity

Import the FBX, then build a plain GameObject:

```
YourFish              (empty root, at origin)
  Visual              (MeshFilter + MeshRenderer, scale 1,1,1)
```

No colliders, no scripts, no lights, no particles. MoreFishAPI strips those
anyway, but leaving them out keeps things predictable.

Put a simple URP Lit material on the renderer. Do not ship a custom shader, see
[Making assets](making-assets.md).

Mark the prefab as part of an AssetBundle and build the bundle for
**StandaloneWindows64**.

## 3. Ship the bundle with your mod

Put the bundle next to your DLL in `BepInEx/plugins/`.

## 4. Register it

```csharp
using MoreFishAPI;

[BepInPlugin("you.yourmod", "YourMod", "1.0.0")]
[BepInDependency("dazed.howtofish.morefishapi", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        string path = Path.Combine(Paths.PluginPath, "yourfish.bundle");
        AssetBundle bundle = MoreFish.LoadBundle(path);
        if (bundle == null)
        {
            return;
        }

        MoreFish.Register(new FishDefinition
        {
            Id          = "you.yourmod.anglerfish",
            DisplayName = "Anglerfish",
            Prefab      = bundle.LoadAsset<GameObject>("Anglerfish"),
            Template    = "Cod",
            Worth       = 400,
            Rarity      = 0.3f,
            Health      = 120,
            BaitFilter  = "Standard Lure, Professional Lure",
        });
    }
}
```

Register from `Awake`. Item ids and the network prefab list are assembled once at
startup, and registering after that returns `false` with an error naming your
mod. A `true` return still only means your arguments were valid, so check `Live`
after `OnFishRebuilt` to confirm the fish is really in the game.

The `BepInDependency` attribute is not strictly required, but BepInEx starts
plugins in id order and most ids sort before `dazed.howtofish.morefishapi`.
Adding it guarantees MoreFishAPI is up before you call it.

## 5. Test it

Do not fish for an hour hoping it appears:

```csharp
MoreFish.ForceNextCatch("you.yourmod.anglerfish");
```

The next thing landed on any rod is your fish. Bind it to a key while you
iterate. See [Testing your fish](testing.md) for reading the log and diagnosing
what went wrong.

## What to check on your first catch

| Symptom | Cause |
| --- | --- |
| Stands on its tail | Mesh is not longest on X |
| Enormous or microscopic | Not authored at real size |
| Wrong name | `DisplayName` not set |
| Glowing, rainbow | The game rolled a shiny. Set `AllowDrip = false` |
| Far too much health | Inherited from the template. Set `Health` |
| Says endangered | Inherited from the template. Set `Endangered = false` |

## Next

* [FishDefinition reference](fish-definition.md) for every property
* [Templates](templates.md) to pick the right creature to build on
* [Limits and gotchas](limitations.md) before you plan anything ambitious
