[Documentation index](../README.md)

# Templates

Your fish is a clone of one of the game's own creatures with your artwork
grafted on. The `Template` you choose decides body physics, how it behaves on
the line, whether it attacks, and every stat you do not override.

All figures below were measured from the game at runtime.

## Peaceful templates

| Template | Size (X, Y, Z) | Health | Endangered | Worth |
| --- | --- | --- | --- | --- |
| Superdwarf Fish | 0.04, 0.01, 0.01 | 120 | yes | 1700 |
| Shrimp | 0.10, 0.01, 0.07 | 12 | no | 5 |
| Goby | 0.15, 0.07, 0.06 | 40 | no | 12 |
| Clownfish | 0.22, 0.07, 0.11 | 140 | no | 46 |
| Lobster | 0.22, 0.37, 0.08 | 85 | no | 9 |
| Goldfish | 0.23, 0.05, 0.11 | 50 | no | 24 |
| BrownCrab | 0.23, 0.14, 0.05 | 15 | no | 3 |
| RockCrab | 0.23, 0.14, 0.05 | 70 | no | 7 |
| Angelfish | 0.31, 0.05, 0.20 | 160 | no | 62 |
| Bluegill | 0.39, 0.08, 0.22 | 150 | no | 62 |
| Needlefish | 0.44, 0.05, 0.04 | 130 | yes | 60 |
| Yellow Boxfish | 0.45, 0.19, 0.18 | 160 | no | 53 |
| Voxel Fish | 0.71, 0.20, 0.37 | 380 | yes | 340 |
| Triggerfish | 0.73, 0.23, 0.35 | 100 | no | 18 |
| Salmon | 0.76, 0.12, 0.20 | 75 | no | 14 |
| Mackerel | 0.76, 0.10, 0.23 | 40 | no | 6 |
| Cod | 0.77, 0.20, 0.31 | 50 | no | 10 |
| Flying Fish | 0.80, 0.46, 0.37 | 250 | no | 320 |
| Bowlfish | 0.82, 0.87, 0.82 | 160 | yes | 150 |
| Perch | 0.84, 0.15, 0.41 | 75 | no | 18 |
| Catfish | 0.88, 0.27, 0.35 | 200 | no | 46 |
| Bass | 0.92, 0.17, 0.41 | 350 | no | 250 |
| Dripper | 1.16, 0.80, 0.72 | 350 | yes | 380 |
| Sengarat | 1.29, 0.22, 0.44 | 350 | no | 280 |
| Pike | 1.32, 0.50, 0.40 | 80 | no | 12 |
| Halibut | 1.43, 0.92, 0.11 | 500 | no | 290 |
| Oarfish | 1.60, 0.26, 0.35 | 1000 | yes | 1450 |
| Seahorse | 2.55, 0.57, 1.92 | 350 | yes | 100 |

## Hostile templates

These attack the player. Hostility comes from the template and **cannot be added
any other way**.

| Template | Size (X, Y, Z) | Health | Worth |
| --- | --- | --- | --- |
| Sea Urchin | 0.26, 0.24, 0.24 | 100 | 52 |
| Piranha | 0.60, 0.18, 0.31 | 5 | 4 |
| Red Snapper | 0.64, 0.14, 0.32 | 280 | 280 |
| Anglerfish | 0.74, 0.35, 0.44 | 650 | 1500 |
| Stonefish | 0.79, 0.41, 0.34 | 800 | 1500 |
| Blobfish | 0.98, 0.64, 0.31 | 1000 | 1360 |
| Eel | 0.99, 0.06, 0.10 | 250 | 280 |
| Gar | 1.44, 0.33, 0.27 | 40 | 5 |
| Tigerfish | 1.66, 0.33, 0.67 | 380 | 310 |
| Parrotfish | 1.69, 0.78, 1.00 | 550 | 350 |

`Hostile = false` neutralises an attacking template if you want the body plan
without the biting.

## Choosing one

**Match the body plan, then override the stats.** Template choice is about shape
and physics. Health, worth, endangered status and flopping are all settable, so
do not pick Blobfish just because you want 1000 health.

**Cod is a good default.** 0.77m, 50 health, worth 10, not endangered, a normal
fish silhouette and unremarkable stats.

**Piranha is the best hostile starting point.** A proper fish shape at 0.60m,
where Sea Urchin is a small spiky ball and most other hostiles are large.

**Watch the flat ones.** Needlefish is 44cm long but 5cm tall. Halibut is 1.43m
long and 11cm thick. A chunky model on a flat template will look out of place.

**Seahorse is an outlier** at 2.55m, far larger than anything else, so may perform wonky with your art.

## What you cannot use

Bosses and mini bosses are excluded for now, so Giant Piranha, Bowhead Whale, Blue
Shark, Goblin Shark, Tuna, The Old Pike and Mutated Bowhead Whale are not
available. Creatures flagged out of the journal, such as BingBong, are also
excluded.

The full list is logged at startup, so if a game update adds creatures they should
appear there without this page being updated.

## Next

* [FishDefinition reference](fish-definition.md)
* [Limits](limitations.md)
