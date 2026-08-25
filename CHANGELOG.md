# Changelog

## 1.0.0

First release.

* Register custom fish from any mod with `MoreFish.Register`. Supply a prefab or a
  bare mesh, and the API builds a working networked fish from one of the game's own
  creatures.
* 38 of the game's creatures can be used as a template, including 10 that attack.
  The template decides body shape, physics and hostility, and is logged at startup
  with real measurements so authors do not have to guess.
* Custom fish appear in the journal and are tracked like vanilla catches.
* Health, worth, endangered status, flopping, food and healing values, rarity and
  which baits a fish appears on are all settable, and anything left alone is
  inherited from the template.
* `MoreFish.ForceNextCatch` makes the next catch a chosen fish, so authors do not
  have to fish for hours to test.
* Item ids are derived from the fish's id string, so the same fish gets the same id
  on every machine with the same fish mods installed. The network prefab list is
  seeded by id rather than by load order, so a player missing a mod sees nothing
  rather than the wrong fish. A checksum is logged for comparing between players.
* Loading a save that contains a fish from a mod you no longer have skips just that
  item. The game's own loader would otherwise drop the rest of the inventory and
  every purchased bait.
