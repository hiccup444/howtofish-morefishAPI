using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FishNet;
using FishNet.Managing.Object;
using FishNet.Object;
using HarmonyLib;
using UnityEngine;

namespace MoreFishAPI
{
	internal static class Injector
	{
		private const ushort CollectionId = 41;

		private static FieldInfo _allItems;
		private static FieldInfo _idToSpawnable;
		private static FieldInfo _nameToSpawnable;
		private static FieldInfo _allCreatures;
		private static FieldInfo _allBaits;

		private static FieldInfo _itemId;
		private static FieldInfo _itemWorth;
		private static FieldInfo _itemMesh;
		private static FieldInfo _dripMesh;
		private static FieldInfo _excludeFromJournal;
		private static FieldInfo _baitWeights;
		private static FieldInfo _weightFishable;
		private static FieldInfo _weightValue;
		private static FieldInfo _fishableItem;

		private static readonly Dictionary<byte, FishDefinition> _byRuntimeId = new Dictionary<byte, FishDefinition>();

		private static GameObject _nursery;
		private static readonly List<GameObject> _placeholders = new List<GameObject>();
		private static bool _resolved;
		private static bool _dirty;
		private static bool _attempted;
		private static bool _succeeded;
		private static bool _warnedTemplate;
		private static bool _listedCandidates;
		private static float _firstTry;

		public static bool Succeeded => _succeeded;

		public static bool AlreadyBuilt => _attempted;

		public static FishDefinition FromItem(Item item)
		{
			return item != null && _byRuntimeId.TryGetValue(item.ID, out FishDefinition fish) ? fish : null;
		}

		public static bool KnowsId(byte id)
		{
			return _byRuntimeId.ContainsKey(id);
		}

		public static bool Resolve()
		{
			if (_resolved)
			{
				return _allItems != null;
			}

			_resolved = true;

			_allItems = AccessTools.Field(typeof(GameInfo), "_allItems");
			_idToSpawnable = AccessTools.Field(typeof(GameInfo), "_idToSpawnable");
			_nameToSpawnable = AccessTools.Field(typeof(GameInfo), "_nameToSpawnable");
			_allCreatures = AccessTools.Field(typeof(GameInfo), "_allCreatures");
			_allBaits = AccessTools.Field(typeof(GameInfo), "_allBaits");

			_itemId = AccessTools.Field(typeof(Item), "_id");
			_itemWorth = AccessTools.Field(typeof(Item), "_worth");
			_itemMesh = AccessTools.Field(typeof(Item), "_mesh");
			_dripMesh = AccessTools.Field(typeof(Creature), "_dripMesh");
			_excludeFromJournal = AccessTools.Field(typeof(Creature), "_excludeFromJournal");
			_baitWeights = AccessTools.Field(typeof(BaitInfo), "_itemWeights");
			_weightFishable = AccessTools.Field(typeof(ItemInfoWeight), "fishable");
			_weightValue = AccessTools.Field(typeof(ItemInfoWeight), "_weight");
			_fishableItem = AccessTools.Field(typeof(Fishable), "_itemToSpawn");

			Dictionary<string, FieldInfo> required = new Dictionary<string, FieldInfo>
			{
				{ "GameInfo._allItems", _allItems },
				{ "GameInfo._allCreatures", _allCreatures },
				{ "GameInfo._allBaits", _allBaits },
				{ "Item._id", _itemId },
				{ "BaitInfo._itemWeights", _baitWeights },
				{ "ItemInfoWeight.fishable", _weightFishable },
				{ "ItemInfoWeight._weight", _weightValue },
				{ "Fishable._itemToSpawn", _fishableItem }
			};

			foreach (KeyValuePair<string, FieldInfo> pair in required)
			{
				if (pair.Value == null)
				{
					Plugin.Log.LogError($"Could not find {pair.Key}. Custom fish cannot be added to this version of the game.");
					_allItems = null;
					return false;
				}
			}

			if (_allItems.FieldType.IsGenericType)
			{
				Type key = _allItems.FieldType.GetGenericArguments()[0];
				if (key != typeof(byte))
				{
					Plugin.Log.LogError($"The game now keys its item list by {key.Name} rather than byte. Custom fish would be added in a way the game cannot see, so none will be added.");
					_allItems = null;
					return false;
				}
			}

			return true;
		}

		public static void MarkDirty()
		{
			if (_attempted)
			{
				Plugin.Log.LogWarning("Backstop: a fish reached MarkDirty after the custom fish were built. MoreFish.Register already refuses late registration, so reaching here means something called this directly.");
				return;
			}

			_dirty = true;
		}

		public static void TryBuild()
		{
			if (!_dirty || _attempted || !Resolve())
			{
				return;
			}

			IDictionary items = _allItems.GetValue(null) as IDictionary;
			if (items == null || items.Count == 0)
			{
				return;
			}

			if (InstanceFinder.NetworkManager == null)
			{
				return;
			}

			Creature template = FindTemplate(items);
			if (template == null)
			{
				if (_firstTry <= 0f)
				{
					_firstTry = Time.realtimeSinceStartup;
				}

				if (!_warnedTemplate && Time.realtimeSinceStartup - _firstTry > 10f)
				{
					_warnedTemplate = true;
					_attempted = true;
					Plugin.Log.LogError("No ordinary fish could be found to use as a template, so no custom fish were added.");
				}

				return;
			}

			_dirty = false;
			_attempted = true;

			try
			{
				_succeeded = Build(items, template);
			}
			catch (Exception e)
			{
				Plugin.Log.LogError("Failed to add custom fish: " + e);
			}
			finally
			{
				MoreFish.RaiseRebuilt();
			}
		}

		private static readonly Dictionary<string, Creature> _candidates =
			new Dictionary<string, Creature>(StringComparer.OrdinalIgnoreCase);

		public static Creature TemplateFor(FishDefinition fish, Creature fallback)
		{
			string wanted = !string.IsNullOrEmpty(fish.Template) ? fish.Template : Plugin.TemplateFish.Value;

			if (!string.IsNullOrEmpty(wanted) && _candidates.TryGetValue(wanted, out Creature match))
			{
				return match;
			}

			if (!string.IsNullOrEmpty(fish.Template))
			{
				Plugin.Log.LogWarning($"Fish '{fish.Id}' asked for template '{fish.Template}', which this game does not have. Using '{fallback.name}' instead.");
			}

			return fallback;
		}

		private static Creature FindTemplate(IDictionary items)
		{
			List<Creature> candidates = new List<Creature>();

			foreach (DictionaryEntry entry in items)
			{
				Item item = entry.Value as Item;
				if (item == null)
				{
					continue;
				}

				Creature creature = item.GetComponent<Creature>();
				if (creature == null || creature.BossType != BossType.None || creature.ExcludeFromJournal)
				{
					continue;
				}

				bool hasVisual = item.GetComponentsInChildren<MeshFilter>(true).Length > 0
					|| item.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0;

				if (item.GetComponent<NetworkObject>() == null || !hasVisual)
				{
					continue;
				}

				candidates.Add(creature);
			}

			if (candidates.Count == 0)
			{
				return null;
			}

			_candidates.Clear();
			foreach (Creature candidate in candidates)
			{
				_candidates[candidate.name] = candidate;
			}

			if (!_listedCandidates)
			{
				_listedCandidates = true;
				List<string> names = new List<string>();
				foreach (Creature candidate in candidates)
				{
					names.Add(candidate.name);
				}

				names.Sort(string.CompareOrdinal);
				Plugin.Log.LogInfo("Fish available as a template: " + string.Join(", ", names));

				foreach (Creature candidate in candidates)
				{
					Bounds combined = CombinedBounds(candidate.gameObject);
					Component[] parts = candidate.GetComponentsInChildren<MeshFilter>(true).Length > 0
						? (Component[])candidate.GetComponentsInChildren<MeshFilter>(true)
						: candidate.GetComponentsInChildren<SkinnedMeshRenderer>(true);
					string first = parts.Length > 0 ? "n/a" : "none";

					string kind = candidate is AttackingFish ? "hostile" : candidate is Fish ? "fish" : "creature";
					int hp = (int)(AccessTools.Field(typeof(Creature), "_maxHp")?.GetValue(candidate) ?? 0);
					bool rare = (bool)(AccessTools.Field(typeof(Creature), "_isEndangered")?.GetValue(candidate) ?? false);

					Plugin.Log.LogInfo($"  {candidate.name}: {kind}, {parts.Length} part(s), combined {combined.size}, hp {hp}, endangered {rare}, worth {candidate.DefaultWorth}");
				}
			}

			string wanted = Plugin.TemplateFish.Value;
			if (!string.IsNullOrEmpty(wanted))
			{
				foreach (Creature candidate in candidates)
				{
					if (string.Equals(candidate.name, wanted, StringComparison.OrdinalIgnoreCase))
					{
						return candidate;
					}
				}

				Plugin.Log.LogWarning($"No fish called '{wanted}' exists, so one will be chosen automatically. See the list above for valid names.");
			}

			Creature best = null;
			int bestSegments = int.MaxValue;

			foreach (Creature candidate in candidates)
			{
				int segments = candidate.GetComponentsInChildren<MeshFilter>(true).Length;
				if (segments == 0)
				{
					continue;
				}

				if (segments < bestSegments)
				{
					best = candidate;
					bestSegments = segments;
				}
			}

			return best ?? candidates[0];
		}

		public static Bounds CombinedBounds(GameObject root)
		{
			bool started = false;
			Bounds total = new Bounds(Vector3.zero, Vector3.zero);

			foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
			{
				if (filter.sharedMesh == null)
				{
					continue;
				}

				Bounds local = filter.sharedMesh.bounds;
				Bounds shifted = new Bounds(local.center + filter.transform.localPosition,
					Vector3.Scale(local.size, filter.transform.localScale));

				if (!started) { started = true; total = shifted; }
				else { total.Encapsulate(shifted); }
			}

			foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				if (skinned.sharedMesh == null)
				{
					continue;
				}

				Bounds local = skinned.sharedMesh.bounds;
				Bounds shifted = new Bounds(local.center + skinned.transform.localPosition,
					Vector3.Scale(local.size, skinned.transform.localScale));

				if (!started) { started = true; total = shifted; }
				else { total.Encapsulate(shifted); }
			}

			return total;
		}

		private static GameObject Nursery()
		{
			if (_nursery == null)
			{
				_nursery = new GameObject("MoreFishAPI_Templates");
				UnityEngine.Object.DontDestroyOnLoad(_nursery);
				_nursery.SetActive(false);
			}

			return _nursery;
		}

		private static bool Build(IDictionary items, Creature template)
		{
			List<FishDefinition> ordered = MoreFish.Ordered();
			if (ordered.Count == 0)
			{
				return false;
			}

			Dictionary<string, byte> assigned = IdMap.Assign(ordered, items);
			if (assigned.Count == 0)
			{
				Plugin.Log.LogError("No item ids could be assigned, so no custom fish were added.");
				return false;
			}

			PrefabObjects collection = InstanceFinder.NetworkManager.GetPrefabObjects<SinglePrefabObjects>(CollectionId, true);
			if (collection == null)
			{
				Plugin.Log.LogError("FishNet has no prefab collection available, so custom fish cannot be made spawnable.");
				return false;
			}

			Dictionary<byte, GameObject> built = new Dictionary<byte, GameObject>();
			List<string> report = new List<string>();

			foreach (FishDefinition fish in ordered)
			{
				if (!assigned.TryGetValue(fish.Id, out byte id))
				{
					continue;
				}

				Creature basis = TemplateFor(fish, template);
				GameObject clone = UnityEngine.Object.Instantiate(basis.gameObject, Nursery().transform);
				clone.name = "MoreFish_" + fish.Id;

				Item item = clone.GetComponent<Item>();
				Creature creature = clone.GetComponent<Creature>();

				_itemId.SetValue(item, id);
				_itemWorth?.SetValue(item, fish.Worth);
				_itemMesh?.SetValue(item, fish.Mesh);
				_dripMesh?.SetValue(creature, fish.Mesh);

				bool journal = fish.InJournal && Plugin.AddToJournal.Value;
				_excludeFromJournal?.SetValue(creature, !journal);

				ApplyLook(clone, fish, basis);
				ApplyBehaviour(clone, fish, creature, basis);

				if (clone.GetComponent<NetworkObject>() == null)
				{
					Plugin.Log.LogError($"Fish '{fish.Id}' has no NetworkObject and cannot be spawned. Skipping it.");
					UnityEngine.Object.Destroy(clone);
					continue;
				}

				built[id] = clone;
				fish.RuntimeItemId = id;
				report.Add($"{fish.Id}={id} on {basis.name}");
			}

			if (built.Count == 0)
			{
				Plugin.Log.LogError("No custom fish could be prepared, so nothing was added to the game.");
				return false;
			}

			if (!SeedCollection(collection, built))
			{
				collection.Clear();

				foreach (GameObject orphan in built.Values)
				{
					UnityEngine.Object.Destroy(orphan);
				}

				foreach (GameObject filler in _placeholders)
				{
					UnityEngine.Object.Destroy(filler);
				}

				_placeholders.Clear();

				Plugin.Log.LogError("Custom fish were not added, because they could not be registered safely for multiplayer.");
				return false;
			}

			Publish(items, built, ordered, assigned);

			string sum = IdMap.Checksum(assigned);
			Plugin.Log.LogInfo($"Added {built.Count} custom fish. Ids: {string.Join(", ", report)}");
			Plugin.Log.LogInfo($"Fish set checksum {sum}. Everyone in a lobby must show the same checksum, or fish will not match.");
			return true;
		}

		private static bool SeedCollection(PrefabObjects collection, Dictionary<byte, GameObject> built)
		{
			if (collection.GetObjectCount() > 0)
			{
				Plugin.Log.LogWarning($"FishNet collection {CollectionId} already holds {collection.GetObjectCount()} prefab(s) that this mod did not add. Replacing them, which may break whatever put them there.");
			}

			collection.Clear();

			List<NetworkObject> slots = new List<NetworkObject>(IdMap.Slots);

			for (int i = 0; i < IdMap.Slots; i++)
			{
				byte id = (byte)(IdMap.Lowest + i);

				if (built.TryGetValue(id, out GameObject real))
				{
					slots.Add(real.GetComponent<NetworkObject>());
					continue;
				}

				slots.Add(Placeholder(id).GetComponent<NetworkObject>());
			}

			collection.AddObjects(slots, false, true);

			for (int i = 0; i < slots.Count; i++)
			{
				if (slots[i] == null || slots[i].SpawnableCollectionId != CollectionId || slots[i].PrefabId != i)
				{
					Plugin.Log.LogError($"FishNet did not take ownership of slot {i}, so custom fish are not safe to spawn.");
					return false;
				}
			}

			return true;
		}

		private static GameObject Placeholder(byte id)
		{
			GameObject filler = new GameObject("MoreFish_Unused_" + id);
			filler.transform.SetParent(Nursery().transform, false);
			filler.AddComponent<NetworkObject>();

			_placeholders.Add(filler);
			return filler;
		}

		private static void Publish(IDictionary items, Dictionary<byte, GameObject> built, List<FishDefinition> ordered, Dictionary<string, byte> assigned)
		{
			IDictionary idToSpawnable = _idToSpawnable?.GetValue(null) as IDictionary;
			IDictionary nameToSpawnable = _nameToSpawnable?.GetValue(null) as IDictionary;
			IList creatures = _allCreatures.GetValue(null) as IList;

			foreach (FishDefinition fish in ordered)
			{
				if (!assigned.TryGetValue(fish.Id, out byte id) || !built.TryGetValue(id, out GameObject clone))
				{
					continue;
				}

				Item item = clone.GetComponent<Item>();
				Creature creature = clone.GetComponent<Creature>();

				items[id] = item;

				if (idToSpawnable != null && !idToSpawnable.Contains(id))
				{
					idToSpawnable[id] = item;
				}

				if (nameToSpawnable != null)
				{
					string key = "morefish_" + fish.Id.Replace(" ", "").ToLowerInvariant();
					if (!nameToSpawnable.Contains(key))
					{
						nameToSpawnable[key] = item;
					}
				}

				bool journal = fish.InJournal && Plugin.AddToJournal.Value;
				if (journal && creatures != null && !creatures.Contains(creature))
				{
					creatures.Add(creature);
				}

				fish.Live = true;
				_byRuntimeId[id] = fish;
			}

			BaitTables.Inject(ordered, _allBaits, _baitWeights, _weightFishable, _weightValue, _fishableItem);
		}

		private static int LongestAxis(Vector3 size)
		{
			if (size.x >= size.y && size.x >= size.z)
			{
				return 0;
			}

			return size.y >= size.z ? 1 : 2;
		}

		private static Vector3 AlignAxes(Bounds source, Bounds target)
		{
			int from = LongestAxis(source.size);
			int to = LongestAxis(target.size);

			if (from == to)
			{
				return Vector3.zero;
			}

			if (from == 1 && to == 0) { return new Vector3(0f, 0f, -90f); }
			if (from == 0 && to == 1) { return new Vector3(0f, 0f, 90f); }
			if (from == 1 && to == 2) { return new Vector3(90f, 0f, 0f); }
			if (from == 2 && to == 1) { return new Vector3(-90f, 0f, 0f); }
			if (from == 2 && to == 0) { return new Vector3(0f, 90f, 0f); }
			if (from == 0 && to == 2) { return new Vector3(0f, -90f, 0f); }

			return Vector3.zero;
		}

		private static Mesh Bake(Mesh source, float scale, Quaternion turn)
		{
			if (!source.isReadable)
			{
				Plugin.Log.LogWarning($"Mesh '{source.name}' is not marked Read/Write in its asset bundle, so its size and orientation cannot be corrected. Tick Read/Write when building the bundle.");
				return source;
			}

			Mesh baked = new Mesh { name = source.name + "_fitted" };

			Vector3[] vertices = source.vertices;
			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i] = turn * vertices[i] * scale;
			}
			baked.vertices = vertices;

			Vector3[] normals = source.normals;
			if (normals != null && normals.Length == vertices.Length)
			{
				for (int i = 0; i < normals.Length; i++)
				{
					normals[i] = turn * normals[i];
				}
				baked.normals = normals;
			}

			baked.uv = source.uv;

			if (source.uv2 != null && source.uv2.Length == vertices.Length)
			{
				baked.uv2 = source.uv2;
			}

			if (source.colors32 != null && source.colors32.Length == vertices.Length)
			{
				baked.colors32 = source.colors32;
			}

			baked.subMeshCount = source.subMeshCount;
			for (int i = 0; i < source.subMeshCount; i++)
			{
				baked.SetTriangles(source.GetTriangles(i), i);
			}

			baked.RecalculateBounds();
			if (normals == null || normals.Length != vertices.Length)
			{
				baked.RecalculateNormals();
			}

			baked.RecalculateTangents();

			UnityEngine.Object.DontDestroyOnLoad(baked);
			return baked;
		}

		private static void Quiet(GameObject clone)
		{
			FieldInfo shinyField = AccessTools.Field(typeof(Creature), "_enableOnShiny");
			GameObject shiny = shinyField?.GetValue(clone.GetComponent<Creature>()) as GameObject;
			if (shiny != null)
			{
				shiny.SetActive(false);
			}

			foreach (Light light in clone.GetComponentsInChildren<Light>(true))
			{
				light.enabled = false;
			}

			foreach (ParticleSystem particles in clone.GetComponentsInChildren<ParticleSystem>(true))
			{
				ParticleSystem.EmissionModule emission = particles.emission;
				emission.enabled = false;
			}
		}

		private static void SetFloat(object target, string field, float value)
		{
			if (value < 0f || target == null)
			{
				return;
			}

			FieldInfo info = AccessTools.Field(target.GetType(), field);
			info?.SetValue(target, value);
		}

		private static void SetInt(object target, string field, int value)
		{
			if (value < 0 || target == null)
			{
				return;
			}

			FieldInfo info = AccessTools.Field(target.GetType(), field);
			info?.SetValue(target, value);
		}

		private static void ApplyBehaviour(GameObject clone, FishDefinition fish, Creature creature, Creature basis)
		{
			SetFloat(creature, "_headPos", fish.HeadPosition);
			SetFloat(clone.GetComponent<Item>(), "_inventoryMeshScale", fish.PreviewScale);

			if (!fish.KeepTemplateSkin)
			{
				AccessTools.Field(typeof(Item), "_skinPreset")?.SetValue(clone.GetComponent<Item>(), null);
			}

			SetInt(creature, "_maxHp", fish.Health);
			SetInt(creature, "_fullnessToRestore", fish.FoodValue);
			SetInt(creature, "_hpToRestore", fish.HealValue);

			if (fish.Endangered.HasValue)
			{
				AccessTools.Field(typeof(Creature), "_isEndangered")?.SetValue(creature, fish.Endangered.Value);
			}

			Fish swimmer = clone.GetComponent<Fish>();
			if (swimmer != null)
			{
				SetFloat(swimmer, "_minTime", fish.FlopIntervalMin);
				SetFloat(swimmer, "_maxTime", fish.FlopIntervalMax);
				SetFloat(swimmer, "_rotForce", fish.FlopStrength);
				SetFloat(swimmer, "_towardsWaterForce", fish.SinkForce);
			}

			AttackingFish hunter = clone.GetComponent<AttackingFish>();

			if (fish.Hostile.HasValue && fish.Hostile.Value && hunter == null)
			{
				Plugin.Log.LogWarning($"Fish '{fish.Id}' asked to be hostile, but '{basis.name}' is not an attacking creature. Hostility is built into the game's own creature and cannot be added, so pick a template that already attacks.");
			}

			if (fish.Hostile.HasValue && !fish.Hostile.Value && hunter != null)
			{
				SetFloat(hunter, "_towardsPlayerForce", 0f);
				SetFloat(hunter, "_airTowardsPlayerForce", 0f);
				SetFloat(hunter, "_underwaterAttackForceMulti", 0f);
				AccessTools.Field(typeof(AttackingFish), "_timeBetweenDamage")?.SetValue(hunter, 999999f);
				Plugin.Log.LogInfo($"Fish '{fish.Id}' built on the attacking creature '{basis.name}' but was made harmless.");
			}
		}

		private static void AdoptRenderers(GameObject clone, GameObject visual, bool keepSkin)
		{
			List<Renderer> own = new List<Renderer>(visual.GetComponentsInChildren<Renderer>(true));
			if (own.Count == 0)
			{
				return;
			}

			Item item = clone.GetComponent<Item>();

			if (AccessTools.Field(typeof(Item), "_renderers")?.GetValue(item) is List<Renderer> renderers)
			{
				renderers.Clear();
				renderers.AddRange(own);
			}

			if (!keepSkin)
			{
				return;
			}

			if (AccessTools.Field(typeof(Item), "_skinRenderers")?.GetValue(item) is List<Renderer> skinned)
			{
				skinned.Clear();
				skinned.AddRange(own);
			}
		}

		private static void GraftPrefab(GameObject clone, FishDefinition fish, Creature basis)
		{
			MeshFilter[] existing = clone.GetComponentsInChildren<MeshFilter>(true);
			SkinnedMeshRenderer[] skinned = clone.GetComponentsInChildren<SkinnedMeshRenderer>(true);

			Transform anchor = existing.Length > 0
				? existing[0].transform
				: skinned.Length > 0 ? skinned[0].transform : clone.transform;

			foreach (MeshFilter filter in existing)
			{
				Renderer renderer = filter.GetComponent<Renderer>();
				if (renderer != null)
				{
					renderer.enabled = false;
				}
			}

			foreach (SkinnedMeshRenderer body in skinned)
			{
				body.enabled = false;
			}

			GameObject visual = UnityEngine.Object.Instantiate(fish.Prefab, anchor);
			visual.name = "MoreFish_Visual";
			visual.transform.localPosition = Vector3.zero;
			visual.transform.localRotation = Quaternion.Euler(fish.MeshRotation);
			visual.transform.localScale = Vector3.one * Mathf.Max(0.0001f, fish.MeshScale);

			List<string> silenced = new List<string>();

			foreach (Behaviour behaviour in visual.GetComponentsInChildren<Behaviour>(true))
			{
				if (behaviour == null || behaviour is Animator || behaviour is Animation)
				{
					continue;
				}

				behaviour.enabled = false;
				silenced.Add(behaviour.GetType().Name);
			}

			if (silenced.Count > 0)
			{
				Plugin.Log.LogInfo($"'{fish.Id}': disabled {silenced.Count} component(s) on the supplied prefab: {string.Join(", ", silenced)}. Animators are left running.");
			}

			foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
			{
				UnityEngine.Object.Destroy(collider);
			}

			AdoptRenderers(clone, visual, fish.KeepTemplateSkin);

			MeshFilter primary = visual.GetComponentInChildren<MeshFilter>(true);
			if (primary != null && primary.sharedMesh != null)
			{
				_itemMesh?.SetValue(clone.GetComponent<Item>(), primary.sharedMesh);
				_dripMesh?.SetValue(clone.GetComponent<Creature>(), primary.sharedMesh);

				Renderer own = primary.GetComponent<Renderer>();
				if (own != null && own.sharedMaterial != null)
				{
					fish.RuntimeMaterial = own.sharedMaterial;
				}
			}

			Plugin.Log.LogInfo($"'{fish.Id}': grafted prefab '{fish.Prefab.name}' onto template '{basis.name}' exactly as authored, no auto fitting.");
		}

		private static void ApplyLook(GameObject clone, FishDefinition fish, Creature basis)
		{
			if (fish.Prefab != null)
			{
				GraftPrefab(clone, fish, basis);
				if (!fish.AllowDrip)
				{
					Quiet(clone);
				}
				return;
			}

			MeshFilter[] filters = clone.GetComponentsInChildren<MeshFilter>(true);
			if (filters.Length == 0)
			{
				Plugin.Log.LogError($"Fish '{fish.Id}' supplied a Mesh, but template '{basis.name}' is built from a skinned mesh and cannot take one. Supply a Prefab instead, or pick a different Template.");
				return;
			}

			Mesh original = filters[0].sharedMesh;
			Bounds whole = CombinedBounds(clone);
			float fit = 1f;
			Vector3 autoTurn = Vector3.zero;

			if (fish.AutoFit && whole.size.magnitude > 0.0001f)
			{
				float source = fish.Mesh.bounds.size.magnitude;
				if (source > 0.0001f)
				{
					fit = whole.size.magnitude / source;
				}

				autoTurn = AlignAxes(fish.Mesh.bounds, whole);
			}

			Quaternion spin = Quaternion.Euler(autoTurn) * Quaternion.Euler(fish.MeshRotation);
			Mesh shaped = Bake(fish.Mesh, fit * fish.MeshScale, spin);

			for (int i = 0; i < filters.Length; i++)
			{
				if (i == 0)
				{
					filters[i].sharedMesh = shaped;
					continue;
				}

				Renderer extra = filters[i].GetComponent<Renderer>();
				if (extra != null)
				{
					extra.enabled = false;
				}
			}

			_itemMesh?.SetValue(clone.GetComponent<Item>(), shaped);
			_dripMesh?.SetValue(clone.GetComponent<Creature>(), shaped);

			if (!fish.AllowDrip)
			{
				Quiet(clone);
			}

			Plugin.Log.LogInfo($"'{fish.Id}': mesh {fish.Mesh.bounds.size} vs whole template {whole.size}, fitted by {fit:F3}, turned by {autoTurn}, final size {shaped.bounds.size}.");

			if (fish.Material != null)
			{
				fish.RuntimeMaterial = fish.Material;
			}

			if (fish.Material == null && fish.Texture == null)
			{
				return;
			}

			foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true))
			{
				if (renderer == null || renderer.sharedMaterials.Length == 0)
				{
					continue;
				}

				Material[] replacements = new Material[renderer.sharedMaterials.Length];

				for (int i = 0; i < replacements.Length; i++)
				{
					if (fish.Material != null)
					{
						replacements[i] = fish.Material;
						continue;
					}

					Material source = renderer.sharedMaterials[i];
					if (source == null)
					{
						continue;
					}

					Material copy = new Material(source);
					foreach (string slot in new[] { "_BaseMap", "_MainTex", "_BaseColorMap" })
					{
						if (copy.HasProperty(slot))
						{
							copy.SetTexture(slot, fish.Texture);
						}
					}

					if (copy.HasProperty("_Use_Skin"))
					{
						copy.SetInt("_Use_Skin", 0);
					}

					if (copy.HasProperty("_Rainbow_Skin"))
					{
						copy.SetInt("_Rainbow_Skin", 0);
					}

					UnityEngine.Object.DontDestroyOnLoad(copy);
					replacements[i] = copy;
				}

				renderer.sharedMaterials = replacements;

				if (fish.RuntimeMaterial == null && replacements.Length > 0)
				{
					fish.RuntimeMaterial = replacements[0];
				}
			}
		}
	}
}
