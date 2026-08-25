using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace MoreFishAPI
{
	internal class IdMapFile
	{
		public int Version = 2;

		public Dictionary<string, byte> Assigned = new Dictionary<string, byte>();
	}

	internal static class IdMap
	{
		public const byte Lowest = 128;
		public const byte Highest = 250;
		public const int Slots = Highest - Lowest + 1;

		private static IdMapFile _file;
		private static bool _loaded;

		public static string Path => System.IO.Path.Combine(Application.persistentDataPath, "MoreFishAPI", "fishids.json");

		private static void Load()
		{
			if (_loaded)
			{
				return;
			}

			_loaded = true;

			try
			{
				if (File.Exists(Path))
				{
					IdMapFile read = JsonConvert.DeserializeObject<IdMapFile>(File.ReadAllText(Path));
					if (read?.Assigned != null)
					{
						_file = read;
						return;
					}
				}
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Could not read the fish id record, it will be rewritten: " + e.Message);
			}

			_file = new IdMapFile();
		}

		private static void Save()
		{
			try
			{
				string directory = System.IO.Path.GetDirectoryName(Path);
				Directory.CreateDirectory(directory);

				string temp = Path + ".writing";
				File.WriteAllText(temp, JsonConvert.SerializeObject(_file, Formatting.Indented));

				if (File.Exists(Path))
				{
					File.Replace(temp, Path, null);
				}
				else
				{
					File.Move(temp, Path);
				}
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Could not write the fish id record: " + e.Message);
			}
		}

		public static byte Preferred(string id)
		{
			unchecked
			{
				uint h = 2166136261u;
				foreach (char c in id.ToLowerInvariant())
				{
					h ^= c;
					h *= 16777619u;
				}

				return (byte)(Lowest + (int)(h % (uint)Slots));
			}
		}

		public static Dictionary<string, byte> Assign(List<FishDefinition> fish, IDictionary vanillaItems)
		{
			Load();

			Dictionary<string, byte> result = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
			HashSet<byte> taken = new HashSet<byte>();

			foreach (FishDefinition entry in fish)
			{
				byte start = Preferred(entry.Id);
				byte chosen = 0;

				for (int step = 0; step < Slots; step++)
				{
					byte candidate = (byte)(Lowest + (start - Lowest + step) % Slots);
					if (!taken.Contains(candidate) && !vanillaItems.Contains(candidate))
					{
						chosen = candidate;
						break;
					}
				}

				if (chosen == 0)
				{
					Plugin.Log.LogError($"No free item id remains for fish '{entry.Id}' from {entry.SourcePlugin}. Only {Slots} custom fish can exist across all installed mods.");
					continue;
				}

				result[entry.Id] = chosen;
				taken.Add(chosen);

				if (chosen != start)
				{
					Plugin.Log.LogWarning($"Fish '{entry.Id}' wanted id {start} but it was taken, so it got {chosen}. If other players have a different set of fish mods, this fish may not match on their machine.");
				}
			}

			ReportDrift(result);
			return result;
		}

		private static void ReportDrift(Dictionary<string, byte> assigned)
		{
			bool changed = false;

			foreach (KeyValuePair<string, byte> pair in assigned)
			{
				if (_file.Assigned.TryGetValue(pair.Key, out byte previous) && previous != pair.Value)
				{
					Plugin.Log.LogWarning($"Fish '{pair.Key}' had id {previous} last time and has id {pair.Value} now. Anything of this fish already in a save will not come back correctly.");
				}

				if (!_file.Assigned.ContainsKey(pair.Key) || _file.Assigned[pair.Key] != pair.Value)
				{
					_file.Assigned[pair.Key] = pair.Value;
					changed = true;
				}
			}

			if (changed)
			{
				Save();
			}
		}

		public static string Checksum(Dictionary<string, byte> assigned)
		{
			List<string> keys = new List<string>(assigned.Keys);
			keys.Sort(StringComparer.OrdinalIgnoreCase);

			unchecked
			{
				uint h = 2166136261u;
				foreach (string key in keys)
				{
					foreach (char c in key.ToLowerInvariant())
					{
						h ^= c;
						h *= 16777619u;
					}

					h ^= assigned[key];
					h *= 16777619u;
				}

				return h.ToString("X8");
			}
		}
	}
}
