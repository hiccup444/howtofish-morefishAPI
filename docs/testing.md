[Documentation index](../README.md)

# Testing your fish

## Force the next catch

Waiting for a rare fish to appear naturally is a poor way to iterate. Force it:

```csharp
MoreFish.ForceNextCatch("you.yourmod.yourfish");
```

The next thing landed on any rod is your fish, once, then normal odds resume.

```csharp
MoreFish.ClearForcedCatch();
bool pending = MoreFish.IsForcing;
```

Bind it to a key while you work:

```csharp
private void Update()
{
    Keyboard keyboard = Keyboard.current;
    if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
    {
        MoreFish.ForceNextCatch("you.yourmod.yourfish");
    }
}
```

It refuses, with a warning naming the reason, if the id is not registered or if
the fish was registered but never made it into the game.

This works by intercepting the game's catch roll on the host. In multiplayer only
the host decides what is caught, so forcing from a client does nothing.

## Confirm your fish actually loaded

`Register` returning `true` only means your arguments were valid. It is called
before anything is built. What matters is `Live`:

```csharp
MoreFish.OnFishRebuilt += delegate
{
    FishDefinition fish = MoreFish.Get("you.yourmod.yourfish");
    Log.LogInfo($"Live={fish.Live} ItemId={fish.RuntimeItemId}");
};
```

`OnFishRebuilt` fires once, after fish are built. If `Live` is false, your fish
is registered but not in the game, and the log will say why.

## Reading the log

`BepInEx/LogOutput.log`. A healthy startup looks like this:

```
Registered fish 'Your Fish' as you.yourmod.yourfish from YourMod.
Fish available as a template: Angelfish, Anglerfish, Bass, ...
  Cod: fish, 2 part(s), combined (0.77, 0.20, 0.31), hp 50, endangered False, worth 10
'you.yourmod.yourfish': grafted prefab 'YourFish' onto template 'Cod' exactly as authored, no auto fitting.
Custom fish added to catch tables 1 time(s). Bait names for BaitFilter: Empty Bait, HotDog, Leech Bait, ...
Added 1 custom fish. Ids: you.yourmod.yourfish=230 on Cod
Fish set checksum B40F1DCF. Everyone in a lobby must show the same checksum, or fish will not match.
```

Three useful things fall out of that:

* The **template list** with real measurements, so you can pick without guessing
* The **bait names**, so you can write a `BaitFilter` that matches
* The **item id**, which should stay the same across restarts

## Common problems

| What you see | What it means |
| --- | --- |
| No mention of your fish at all | `Register` was never reached. Check your bundle path loaded |
| "registered after the custom fish had already been built" | You registered too late. Register from `Awake`. The fish is accepted but never built |
| "has no MeshFilter anywhere in it" | Your prefab has no renderable mesh |
| "asked for template 'X', which this game does not have" | Check spelling against the logged template list |
| "added to catch tables 0 time(s)" | Your `BaitFilter` matched no bait |
| Fish stands on its tail | Mesh is not longest on X |
| Striped or rainbow in the world | The game rolled a shiny. Set `AllowDrip = false` |
| Far too much health | Inherited from the template. Set `Health` |
| "can never roll shiny" warning | `InJournal` with `AllowDrip = false`. See [Limits](limitations.md) |

## Testing in multiplayer

**Every player needs the same fish mods installed.** A player missing your mod
will not see your fish at all. A player with a different set of fish mods may see
the wrong fish entirely.

If you can, test with a second machine before release.

## Next

* [Limits](limitations.md)
* [FishDefinition reference](fish-definition.md)
