[Documentation index](../README.md)

# Making assets

Getting a fish out of Blender, into Unity, and into an AssetBundle the game can
load. This is the workflow that was used to build and verify the reference fish.

## Blender

Model the fish **longest on X, nose pointing +X**.

Blender is Z up and Unity is Y up, and the FBX exporter converts between them.
Verified on a real export:

```
Blender  X 0.5002   Y 0.09    Z 0.19
Unity    X 0.500    Y 0.190   Z 0.090
```

Blender Z became Unity Y, Blender Y became Unity Z, and X stayed put. So model
with X as length and Z as height, and it arrives correct.

Before exporting:

* Apply all transforms. A prefab carrying a leftover scale of 100 is the second
  most common sizing mistake after modelling in the wrong units
* Set the origin to the centre of the body, since that is the point the game
  applies physics to. An off centre origin makes the fish spin oddly on the line
* Unwrap UVs. A mesh with no UVs samples one texel and renders as a flat colour
* Keep it low poly. The reference fish is 61 vertices and looks at home

Export FBX with **Y up, -Z forward**, selected objects only, mesh only.

## Unity

Import the FBX and check the mesh bounds match what you expect before going
further.

Build a plain hierarchy:

```
YourFish              (empty root at origin, scale 1)
  Visual              (MeshFilter + MeshRenderer, scale 1)
```

Do not add colliders, scripts, lights or particle systems. MoreFishAPI strips
them, but a prefab that does not need stripping is easier to reason about.

## Materials and the shader problem

**Do not ship a custom shader in your bundle.** A shader compiled against a
different Unity version than the game routinely fails to load and renders
magenta. The game is on Unity 6000.4, and the reference bundle was built in
6000.3.17f1 without trouble for meshes and textures, but shaders are the part
that breaks.

Two safe options:

1. Use a stock **URP Lit** material. This is what the reference fish uses and it
   renders correctly in game
2. Supply `Texture` on your `FishDefinition` instead of a material. The API
   copies the game's own material and swaps the texture in, so you inherit
   whatever shader the game is using

## Inventory and journal previews

Worth knowing, because it caused a confusing bug during development.

The world model and the small previews are rendered by different code. The
previews reuse a shared UI renderer and swap only the mesh, so a custom fish used
to appear correct in the world but striped and rainbow in the hotbar and journal.
That was the mesh's UVs sampling the game's own texture atlas.

MoreFishAPI now applies your fish's material to the preview renderers, so you do
not need to do anything. But if you ever see a fish that looks right in the world
and wrong in the inventory, that is the mechanism.

## Building the bundle

Mark the prefab as part of an AssetBundle, then build for
**StandaloneWindows64**. Ship the bundle file next to your DLL in
`BepInEx/plugins/`.

Load it with `MoreFish.LoadBundle(path)`, which caches by path so several fish
from one bundle do not load it repeatedly.

## Templates to start from

A Unity package of starter template prefabs is available, sized to the game's own
creatures, including two hostile ones. Each is an empty root with a `Visual`
child at the right dimensions. Drop your mesh in, reset the scale, and you have a
correctly proportioned fish.

See [Templates](templates.md) for the measurements if you would rather build from
scratch.

## Next

* [Getting started](getting-started.md)
* [Testing your fish](testing.md)
