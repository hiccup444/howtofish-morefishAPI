using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoreFishAPI
{
	/// <summary>BepInEx entry point. Modders should use <see cref="MoreFish"/> rather than this type.</summary>
	[BepInPlugin(Guid, Name, Version)]
	public class Plugin : BaseUnityPlugin
	{
		/// <summary>BepInEx plugin GUID, for use in BepInDependency.</summary>
		public const string Guid = "dazed.howtofish.morefishapi";
		/// <summary>Human readable plugin name.</summary>
		public const string Name = "MoreFishAPI";
		/// <summary>Plugin version.</summary>
		public const string Version = "1.0.0";

		/// <summary>Log source for this plugin.</summary>
		public static ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource(Name);

		/// <summary>Whether custom fish are listed in the journal.</summary>
		public static ConfigEntry<bool> AddToJournal;
		/// <summary>Scales how often custom fish are caught.</summary>
		public static ConfigEntry<float> RarityMultiplier;
		/// <summary>Creature used when a fish does not name its own template.</summary>
		public static ConfigEntry<string> TemplateFish;

		private Harmony _harmony;

		private void Awake()
		{
			AddToJournal = Config.Bind("General", "AddToJournal", true,
				"List custom fish in the journal alongside the game's own catches, so they count toward what you have seen. Turn this off to keep the journal purely vanilla.");
			RarityMultiplier = Config.Bind("General", "RarityMultiplier", 1f,
				"Scales how often custom fish are caught compared to what their author asked for. Below 1 makes them rarer, above 1 more common. 0 stops them appearing without unregistering them.");

			TemplateFish = Config.Bind("General", "TemplateFish", "Cod",
				"Which of the game's own fish a custom fish is built from when it does not name its own. This decides body shape, physics and how it behaves on the line. Every valid name is listed in the log at startup. Leave blank to let the mod pick one.");

			if (!Injector.Resolve())
			{
				Log.LogError("Aborting: the game's item lists do not look the way this mod expects.");
				enabled = false;
				return;
			}

			_harmony = new Harmony(Guid);
			Patches.ApplyAll(_harmony);

			Log.LogInfo($"{Name} {Version} loaded. Modders: call MoreFishAPI.MoreFish.Register to add a fish.");
		}

		private void OnDestroy()
		{
			_harmony?.UnpatchSelf();
		}

		private void Update()
		{
			Injector.TryBuild();

			if (Injector.AlreadyBuilt)
			{
				enabled = false;
			}
		}
	}

	internal static class Patches
	{
		internal static void ApplyAll(Harmony harmony)
		{
			MethodInfo getName = AccessTools.Method(typeof(Item), "GetName");
			if (getName != null)
			{
				harmony.Patch(getName, new HarmonyMethod(typeof(Patches), nameof(GetNamePrefix)));
			}
			else
			{
				Plugin.Log.LogWarning("Could not hook item naming, so custom fish will show the template fish's name.");
			}

			MethodInfo loadFromSave = AccessTools.Method(typeof(PlayerInventory), "LoadFromSave");
			if (loadFromSave != null)
			{
				harmony.Patch(loadFromSave, new HarmonyMethod(typeof(Patches), nameof(LoadFromSavePrefix)));
			}
			else
			{
				Plugin.Log.LogWarning("Could not hook inventory loading. A save holding a fish this game does not have may lose the rest of that inventory.");
			}

			MethodInfo setDrip = AccessTools.Method(typeof(Creature), "SetDrip");
			if (setDrip != null)
			{
				harmony.Patch(setDrip, new HarmonyMethod(typeof(Patches), nameof(SetDripPrefix)));
			}

			MethodInfo pick = AccessTools.Method(typeof(CreatureManager), "GetRandomItem");
			if (pick != null)
			{
				harmony.Patch(pick, new HarmonyMethod(typeof(Patches), nameof(GetRandomItemPrefix)));
			}
			else
			{
				Plugin.Log.LogWarning("Could not hook catch selection, so MoreFish.ForceNextCatch will not work.");
			}

			MethodInfo slotSkin = AccessTools.Method(typeof(InventorySlot), "ApplySkin");
			if (slotSkin != null)
			{
				harmony.Patch(slotSkin, null, new HarmonyMethod(typeof(Patches), nameof(InventorySlotSkinPostfix)));
			}

			MethodInfo journalSet = AccessTools.Method(typeof(JournalSlot), "SetCreature");
			if (journalSet != null)
			{
				harmony.Patch(journalSet, null, new HarmonyMethod(typeof(Patches), nameof(JournalSlotPostfix)));
			}

			Plugin.Log.LogInfo("Hooked item naming, inventory loading, the shiny roll, catch selection and item previews.");
		}

		private static void LoadFromSavePrefix(PlayerInventory __instance)
		{
			try
			{
				SavedPlayer saved = AccessTools.Field(typeof(PlayerInventory), "_toLoadFrom")?.GetValue(__instance) as SavedPlayer;
				if (saved == null)
				{
					return;
				}

				int removed = Strip(saved.InventoryItems);

				if (saved.HeldItem != null && saved.HeldItem.Exists && !Known(saved.HeldItem.ItemID))
				{
					saved.HeldItem.Exists = false;
					removed++;
				}

				if (removed > 0)
				{
					Plugin.Log.LogWarning($"This save holds {removed} item(s) from a fish mod that is not installed. They have been skipped so the rest of the inventory still loads.");
				}
			}
			catch (Exception e)
			{
				Plugin.Log.LogError("Could not check the saved inventory: " + e);
			}
		}

		private static int Strip(List<SavedItem> items)
		{
			if (items == null)
			{
				return 0;
			}

			int removed = 0;
			for (int i = items.Count - 1; i >= 0; i--)
			{
				if (items[i] != null && items[i].Exists && !Known(items[i].ItemID))
				{
					items.RemoveAt(i);
					removed++;
				}
			}

			return removed;
		}

		private static bool Known(byte id)
		{
			return GameInfo.GetSpawnable(id) != null;
		}

		private static readonly Dictionary<Renderer, Material> _painted = new Dictionary<Renderer, Material>();

		private static void PaintPreview(object slot, string rendererField, Item item)
		{
			Renderer renderer = AccessTools.Field(slot.GetType(), rendererField)?.GetValue(slot) as Renderer;
			if (renderer == null)
			{
				MeshFilter filter = AccessTools.Field(slot.GetType(), "_filter")?.GetValue(slot) as MeshFilter;
				renderer = filter != null ? filter.GetComponent<Renderer>() : null;
			}

			if (renderer == null)
			{
				return;
			}

			FishDefinition fish = Injector.FromItem(item);

			if (fish?.RuntimeMaterial == null)
			{
				_painted.Remove(renderer);
				return;
			}

			if (_painted.TryGetValue(renderer, out Material already) && already == fish.RuntimeMaterial)
			{
				return;
			}

			renderer.sharedMaterial = fish.RuntimeMaterial;
			_painted[renderer] = fish.RuntimeMaterial;

			if (_painted.Count > _pruneAt)
			{
				Prune();
			}
		}

		private static int _pruneAt = 64;

		private static void Prune()
		{
			List<Renderer> gone = new List<Renderer>();

			foreach (KeyValuePair<Renderer, Material> pair in _painted)
			{
				if (pair.Key == null)
				{
					gone.Add(pair.Key);
				}
			}

			foreach (Renderer dead in gone)
			{
				_painted.Remove(dead);
			}

			_pruneAt = Mathf.Max(64, _painted.Count * 2);
		}

		private static void InventorySlotSkinPostfix(InventorySlot __instance, Item item)
		{
			PaintPreview(__instance, "_renderer", item);
		}

		private static void JournalSlotPostfix(JournalSlot __instance, Creature creature)
		{
			PaintPreview(__instance, "_renderer", creature);
		}

		private static bool GetRandomItemPrefix(ref Fishable __result)
		{
			if (MoreFish.Forced == null)
			{
				return true;
			}

			Fishable sample = BaitTables.SampleFor(MoreFish.Forced);
			if (sample == null)
			{
				Plugin.Log.LogWarning($"'{MoreFish.Forced}' was forced but has no catch entry, so the game will pick normally.");
				MoreFish.Forced = null;
				return true;
			}

			Plugin.Log.LogInfo($"Forcing the catch to '{MoreFish.Forced}'.");
			MoreFish.Forced = null;
			__result = sample;
			return false;
		}

		private static bool SetDripPrefix(Creature __instance)
		{
			FishDefinition fish = Injector.FromItem(__instance);
			if (fish == null)
			{
				return true;
			}

			return fish.AllowDrip;
		}

		private static bool GetNamePrefix(Item __instance, ref string __result)
		{
			FishDefinition fish = Injector.FromItem(__instance);
			if (fish == null)
			{
				return true;
			}

			__result = fish.DisplayName;
			return false;
		}
	}
}
