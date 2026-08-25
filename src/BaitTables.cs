using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MoreFishAPI
{
	internal static class BaitTables
	{
		private static readonly List<object> _added = new List<object>();

		private static readonly Dictionary<string, Fishable> _samples =
			new Dictionary<string, Fishable>(StringComparer.OrdinalIgnoreCase);

		public static Fishable SampleFor(string id)
		{
			return !string.IsNullOrEmpty(id) && _samples.TryGetValue(id, out Fishable found) ? found : null;
		}

		public static void Inject(
			List<FishDefinition> fish,
			FieldInfo allBaitsField,
			FieldInfo weightsField,
			FieldInfo weightFishableField,
			FieldInfo weightValueField,
			FieldInfo fishableItemField)
		{
			IList baits = allBaitsField.GetValue(null) as IList;
			if (baits == null || baits.Count == 0)
			{
				Plugin.Log.LogWarning("No baits are loaded yet, so custom fish were not added to any catch table.");
				return;
			}

			int additions = 0;
			List<string> names = new List<string>();
			List<string> skipped = new List<string>();

			foreach (object baitObject in baits)
			{
				BaitInfo bait = baitObject as BaitInfo;
				if (bait == null)
				{
					continue;
				}

				names.Add(bait.name);

				IList weights = weightsField.GetValue(bait) as IList;
				if (weights == null)
				{
					continue;
				}

				if (weights.Count == 0)
				{
					skipped.Add(bait.name);
					continue;
				}

				float baseline = Baseline(weights, weightValueField);

				foreach (FishDefinition entry in fish)
				{
					if (entry.RuntimeItemId == 0 || !Matches(entry, bait))
					{
						continue;
					}

					Item item = GameInfo.IDToItem(entry.RuntimeItemId);
					if (item == null)
					{
						continue;
					}

					Fishable fishable = ScriptableObject.CreateInstance<Fishable>();
					fishable.name = "MoreFish_" + entry.Id;
					fishableItemField.SetValue(fishable, item);
					UnityEngine.Object.DontDestroyOnLoad(fishable);

					object weight = Activator.CreateInstance(typeof(ItemInfoWeight), true);
					weightFishableField.SetValue(weight, fishable);
					weightValueField.SetValue(weight, baseline * entry.Rarity * Mathf.Max(0f, Plugin.RarityMultiplier.Value));

					weights.Add(weight);
					_added.Add(weight);
					_samples[entry.Id] = fishable;
					additions++;
				}
			}

			Plugin.Log.LogInfo($"Custom fish added to catch tables {additions} time(s). Bait names for BaitFilter: {string.Join(", ", names)}");

			if (skipped.Count > 0)
			{
				Plugin.Log.LogInfo($"Skipped {skipped.Count} bait(s) that catch nothing in the base game, so custom fish cannot be caught with no bait equipped: {string.Join(", ", skipped)}");
			}
		}

		private static float Baseline(IList weights, FieldInfo weightValueField)
		{
			float total = 0f;
			int counted = 0;

			foreach (object weight in weights)
			{
				if (_added.Contains(weight))
				{
					continue;
				}

				object value = weightValueField.GetValue(weight);
				if (value is float f && f > 0f)
				{
					total += f;
					counted++;
				}
			}

			return counted > 0 ? total / counted : 1f;
		}

		private static bool Matches(FishDefinition fish, BaitInfo bait)
		{
			if (string.IsNullOrEmpty(fish.BaitFilter))
			{
				return true;
			}

			string baitName = bait.name.Replace(" ", "").ToLowerInvariant();

			foreach (string raw in fish.BaitFilter.Split(','))
			{
				string wanted = raw.Trim().Replace(" ", "").ToLowerInvariant();
				if (wanted.Length > 0 && baitName.Contains(wanted))
				{
					return true;
				}
			}

			return false;
		}
	}
}
