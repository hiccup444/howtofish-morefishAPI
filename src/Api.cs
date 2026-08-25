using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MoreFishAPI
{
	/// <summary>
	/// Describes one custom fish. Pass it to <see cref="MoreFish.Register(FishDefinition)"/> from your plugin's Awake.
	/// Anything left unset is inherited from the creature named by <see cref="Template"/>.
	/// </summary>
	public class FishDefinition
	{
		/// <summary>Unique key, namespace it like you.yourmod.thing. The save file id is derived from this, so never change it once published.</summary>
		public string Id { get; set; }

		/// <summary>Name shown in game. Falls back to <see cref="Id"/>.</summary>
		public string DisplayName { get; set; }

		/// <summary>A bare mesh, scaled and oriented to fit the template. Ignored when <see cref="Prefab"/> is set.</summary>
		public Mesh Mesh { get; set; }

		/// <summary>Your authored visual, used exactly as made with no fitting. Preferred over <see cref="Mesh"/>. Put your material on its renderer, because <see cref="Material"/> and <see cref="Texture"/> are ignored on this path.</summary>
		public GameObject Prefab { get; set; }

		/// <summary>Which of the game's creatures to build on. Decides body shape, physics and whether the fish attacks. Every valid name is listed in the log at startup.</summary>
		public string Template { get; set; } = "";

		/// <summary>Base sell value. The game varies the actual price per catch around this.</summary>
		public int Worth { get; set; } = 10;

		/// <summary>Uniform scale applied to your mesh after any automatic fitting.</summary>
		/// <remarks>Leave at 1 and let <see cref="AutoFit"/> size the fish to its template. Use this to nudge the result.</remarks>
		public float MeshScale { get; set; } = 1f;

		/// <summary>How common this fish is relative to an average catch on the same bait. 1 is typical, 0.2 is about a fifth as likely.</summary>
		public float Rarity { get; set; } = 1f;

		/// <summary>Euler rotation in degrees applied to your mesh before it is baked.</summary>
		/// <remarks>The game expects a fish to lie along its X axis. If yours stands on its tail or swims sideways, correct it here rather than re-exporting.</remarks>
		public Vector3 MeshRotation { get; set; } = Vector3.zero;

		/// <summary>Scale and centre the mesh to match the template creature's bounds.</summary>
		/// <remarks>True keeps your fish a believable size next to the game's own. Set false when you have authored exact real world dimensions and want them respected.</remarks>
		public bool AutoFit { get; set; } = true;

		/// <summary>Allow the game's shiny variant, which swaps in a rainbow shader that suits most custom art poorly.</summary>
		public bool AllowDrip { get; set; }

		/// <summary>Texture applied to the fish, used when you have not supplied a full <see cref="Material"/>.</summary>
		/// <remarks>Wrapped in a copy of the template's material, so it inherits the game's shader and lighting.</remarks>
		public Texture Texture { get; set; }

		/// <summary>Complete material for the fish, overriding <see cref="Texture"/>.</summary>
		/// <remarks>Use a shader the game already ships. A material built against a shader not present in the game renders as the magenta fallback.</remarks>
		public Material Material { get; set; }

		/// <summary>Lower bound of the sale price range. Negative means unset.</summary>
		/// <remarks>Set together with <see cref="WorthMax"/>. The pair replaces <see cref="Worth"/> with their midpoint.</remarks>
		public int WorthMin { get; set; } = -1;

		/// <summary>Upper bound of the sale price range. Negative means unset.</summary>
		/// <remarks>Set together with <see cref="WorthMin"/>.</remarks>
		public int WorthMax { get; set; } = -1;

		/// <summary>Hit points. Leave at -1 to inherit the template's, which ranges from 5 to 1000 depending on the creature.</summary>
		public int Health { get; set; } = -1;

		/// <summary>The endangered tag and its score bonus. Leave null to inherit, and note several templates are flagged endangered.</summary>
		public bool? Endangered { get; set; }

		/// <summary>Setting this false neutralises an attacking template. Setting it true does nothing, because hostility cannot be added at runtime, so pick a hostile template instead.</summary>
		public bool? Hostile { get; set; }

		/// <summary>Keep the template's skin renderers so its skin presets still apply.</summary>
		/// <remarks>Normally false, so your artwork replaces the template's entirely. True is for a fish that deliberately reuses a vanilla skin set.</remarks>
		public bool KeepTemplateSkin { get; set; }

		/// <summary>Distance along the fish's local Z axis past which a hit counts as a headshot. Negative inherits the template's.</summary>
		/// <remarks>Only set this if you have measured your mesh. Too low makes every hit a headshot.</remarks>
		public float HeadPosition { get; set; } = -1f;

		/// <summary>Scale used for the inventory and journal icons. Negative derives one from the mesh bounds.</summary>
		public float PreviewScale { get; set; } = -1f;

		/// <summary>Shortest pause between flops once landed. Negative inherits the template's.</summary>
		public float FlopIntervalMin { get; set; } = -1f;

		/// <summary>Longest pause between flops once landed. Negative inherits the template's.</summary>
		public float FlopIntervalMax { get; set; } = -1f;

		/// <summary>Force behind each flop. Negative inherits the template's.</summary>
		public float FlopStrength { get; set; } = -1f;

		/// <summary>Downward force applied in water, deciding how fast the fish sinks. Negative inherits the template's.</summary>
		public float SinkForce { get; set; } = -1f;

		/// <summary>Hunger restored when eaten. Negative inherits the template's.</summary>
		public int FoodValue { get; set; } = -1;

		/// <summary>Health restored when eaten. Negative inherits the template's.</summary>
		public int HealValue { get; set; } = -1;

		/// <summary>Comma separated bait names, matched as substrings. Empty means every bait. Bait names are listed in the log at startup.</summary>
		public string BaitFilter { get; set; } = "";

		/// <summary>List this fish in the journal. A journal fish that cannot be caught makes the catch everything achievements unobtainable.</summary>
		public bool InJournal { get; set; } = true;

		/// <summary>Name of the plugin that registered this fish, filled in by the API.</summary>
		/// <remarks>Detected from the calling assembly and used in log messages.</remarks>
		public string SourcePlugin { get; internal set; }

		/// <summary>Item id the fish was given, filled in by the API once the fish are built.</summary>
		/// <remarks>Derived from a hash of <see cref="Id"/>, so it agrees across machines with the same fish mods. Zero until built.</remarks>
		public byte RuntimeItemId { get; internal set; }

		/// <summary>Whether the fish actually made it into the game. This, not the return of Register, is how you confirm success. Read it after <see cref="MoreFish.OnFishRebuilt"/>.</summary>
		public bool Live { get; internal set; }

		/// <summary>Material the fish ended up with, filled in by the API once the fish are built.</summary>
		/// <remarks>Also used to paint the inventory and journal previews.</remarks>
		public Material RuntimeMaterial { get; internal set; }
	}

	/// <summary>
	/// The entry point for adding custom fish. Register from your plugin's Awake; fish are built once at startup.
	/// </summary>
	public static class MoreFish
	{
		private static readonly Dictionary<string, FishDefinition> _byId =
			new Dictionary<string, FishDefinition>(StringComparer.OrdinalIgnoreCase);

		private static readonly Dictionary<string, AssetBundle> _bundles =
			new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);

		/// <summary>Fires once after custom fish have been built. Check <see cref="FishDefinition.Live"/> here to confirm yours made it in.</summary>
		public static event Action OnFishRebuilt;

		/// <summary>Always true. Lets a soft dependent confirm the API loaded without catching a type load failure.</summary>
		public static bool IsAvailable => true;

		/// <summary>How many fish have been registered by all mods.</summary>
		public static int RegisteredCount => _byId.Count;

		/// <summary>Every registered fish, in no particular order. Use <see cref="Ordered"/> when order matters.</summary>
		public static IReadOnlyCollection<FishDefinition> All => _byId.Values;

		/// <summary>Adds a custom fish to the game.</summary>
		/// <param name="fish">The fish to add. Must have an Id and either a Mesh or a Prefab.</param>
		/// <returns>True if the definition was accepted. False if it was invalid, a duplicate, or registered too late.</returns>
		/// <remarks>Call from your plugin's Awake. A true return only means the definition was valid, so check <see cref="FishDefinition.Live"/> after <see cref="OnFishRebuilt"/> to confirm the fish reached the game.</remarks>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool Register(FishDefinition fish)
		{
			if (fish == null || string.IsNullOrEmpty(fish.Id))
			{
				Plugin.Log.LogError("A fish was registered with no id, ignoring it.");
				return false;
			}

			if (fish.Mesh == null && fish.Prefab == null)
			{
				Plugin.Log.LogError($"Fish '{fish.Id}' was registered with neither a Mesh nor a Prefab, ignoring it.");
				return false;
			}

			if (fish.Prefab != null && fish.Prefab.GetComponentInChildren<MeshFilter>(true) == null)
			{
				Plugin.Log.LogError($"Fish '{fish.Id}': the prefab '{fish.Prefab.name}' has no MeshFilter anywhere in it, so there is nothing to show. Ignoring it.");
				return false;
			}

			if (_byId.ContainsKey(fish.Id))
			{
				Plugin.Log.LogWarning($"Fish '{fish.Id}' is already registered, ignoring the duplicate.");
				return false;
			}

			fish.SourcePlugin = Caller();

			if (Injector.AlreadyBuilt)
			{
				Plugin.Log.LogError($"Fish '{fish.Id}' from {fish.SourcePlugin} was registered too late. Custom fish are built once at startup, so register from your plugin's Awake. This fish will not appear.");
				return false;
			}

			if (string.IsNullOrEmpty(fish.DisplayName))
			{
				fish.DisplayName = fish.Id;
			}

			fish.Worth = Mathf.Max(0, fish.Worth);
			if (fish.WorthMin >= 0 && fish.WorthMax >= fish.WorthMin)
			{
				fish.Worth = Mathf.Max(1, (fish.WorthMin + fish.WorthMax) / 2);
			}
			fish.Rarity = Mathf.Max(0.0001f, fish.Rarity);
			fish.MeshScale = Mathf.Max(0.01f, fish.MeshScale);

			if (fish.InJournal && !fish.AllowDrip)
			{
				Plugin.Log.LogWarning($"Fish '{fish.Id}' is in the journal but can never roll shiny, so the game's catch every shiny check can never pass while it is installed. Set AllowDrip true, or InJournal false.");
			}

			_byId[fish.Id] = fish;
			Plugin.Log.LogInfo($"Registered fish '{fish.DisplayName}' as {fish.Id} from {fish.SourcePlugin}.");

			Injector.MarkDirty();
			return true;
		}

		/// <summary>Adds a custom fish from a mesh, for the simple case.</summary>
		/// <param name="id">Unique id, conventionally author.mod.fish. Never change it after release.</param>
		/// <param name="displayName">Name shown in game.</param>
		/// <param name="mesh">The fish mesh.</param>
		/// <param name="worth">Sale price.</param>
		/// <param name="rarity">Relative catch weight. Higher is more common.</param>
		/// <returns>True if the definition was accepted.</returns>
		public static bool Register(string id, string displayName, Mesh mesh, int worth, float rarity = 1f)
		{
			return Register(new FishDefinition
			{
				Id = id,
				DisplayName = displayName,
				Mesh = mesh,
				Worth = worth,
				Rarity = rarity
			});
		}

		/// <summary>Adds a custom fish by loading its mesh from an asset bundle.</summary>
		/// <param name="id">Unique id, conventionally author.mod.fish.</param>
		/// <param name="displayName">Name shown in game.</param>
		/// <param name="bundle">Bundle to load from, usually via <see cref="LoadBundle"/>.</param>
		/// <param name="meshName">Name of the mesh asset inside the bundle.</param>
		/// <param name="worth">Sale price.</param>
		/// <param name="rarity">Relative catch weight.</param>
		/// <returns>True if the definition was accepted.</returns>
		public static bool RegisterFromBundle(string id, string displayName, AssetBundle bundle, string meshName, int worth, float rarity = 1f)
		{
			if (bundle == null)
			{
				Plugin.Log.LogError($"Fish '{id}' was given a null asset bundle.");
				return false;
			}

			Mesh mesh = bundle.LoadAsset<Mesh>(meshName);
			if (mesh == null)
			{
				Plugin.Log.LogError($"Fish '{id}': the bundle has no mesh called '{meshName}'. It contains: {string.Join(", ", bundle.GetAllAssetNames())}");
				return false;
			}

			return Register(id, displayName, mesh, worth, rarity);
		}

		/// <summary>Loads an AssetBundle and caches it by path, so several fish from one bundle do not load it repeatedly.</summary>
		public static AssetBundle LoadBundle(string absolutePath)
		{
			if (string.IsNullOrEmpty(absolutePath))
			{
				return null;
			}

			if (_bundles.TryGetValue(absolutePath, out AssetBundle cached) && cached != null)
			{
				return cached;
			}

			try
			{
				if (!File.Exists(absolutePath))
				{
					Plugin.Log.LogError("No asset bundle at " + absolutePath);
					return null;
				}

				AssetBundle bundle = AssetBundle.LoadFromFile(absolutePath);
				if (bundle == null)
				{
					Plugin.Log.LogError($"Unity refused to load the asset bundle at {absolutePath}. This usually means the bundle is already loaded, that it was built for a different platform, or that it was built with a Unity version the game cannot read.");
					return null;
				}

				_bundles[absolutePath] = bundle;
				return bundle;
			}
			catch (Exception e)
			{
				Plugin.Log.LogError("Failed to load asset bundle: " + e);
				return null;
			}
		}

		internal static string Forced;

		/// <summary>Makes the next thing landed on any rod be this fish, once. For testing, so you do not have to fish for hours. Host only.</summary>
		public static bool ForceNextCatch(string id)
		{
			FishDefinition fish = Get(id);
			if (fish == null)
			{
				Plugin.Log.LogWarning($"Cannot force '{id}', no fish is registered under that id.");
				return false;
			}

			if (!fish.Live)
			{
				Plugin.Log.LogWarning($"Cannot force '{id}', it is registered but was not added to the game.");
				return false;
			}

			Forced = fish.Id;
			Plugin.Log.LogInfo($"The next thing caught on any rod will be '{fish.DisplayName}'.");
			return true;
		}

		/// <summary>Cancels a pending <see cref="ForceNextCatch"/>.</summary>
		public static void ClearForcedCatch()
		{
			if (Forced != null)
			{
				Plugin.Log.LogInfo("Forced catch cleared.");
				Forced = null;
			}
		}

		/// <summary>Whether a <see cref="ForceNextCatch"/> is still pending.</summary>
		public static bool IsForcing => Forced != null;

		/// <summary>Whether a fish with this id has been registered.</summary>
		/// <param name="id">The fish id to look for.</param>
		/// <returns>True if registered. This says nothing about whether it reached the game, for which see <see cref="FishDefinition.Live"/>.</returns>
		public static bool IsRegistered(string id)
		{
			return !string.IsNullOrEmpty(id) && _byId.ContainsKey(id);
		}

		/// <summary>Looks up a registered fish.</summary>
		/// <param name="id">The fish id to look for.</param>
		/// <returns>The definition, or null if no fish with that id was registered.</returns>
		public static FishDefinition Get(string id)
		{
			return !string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out FishDefinition fish) ? fish : null;
		}

		/// <summary>All registered fish sorted by id.</summary>
		/// <returns>A new list, safe to modify. The order is stable across machines, which is what the id allocation relies on.</returns>
		public static List<FishDefinition> Ordered()
		{
			List<FishDefinition> ordered = new List<FishDefinition>(_byId.Values);
			ordered.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
			return ordered;
		}

		internal static void RaiseRebuilt()
		{
			Delegate[] listeners = OnFishRebuilt?.GetInvocationList();
			if (listeners == null)
			{
				return;
			}

			foreach (Delegate listener in listeners)
			{
				try
				{
					((Action)listener)();
				}
				catch (Exception e)
				{
					Plugin.Log.LogError($"A listener of OnFishRebuilt threw, the others still ran: {e}");
				}
			}
		}

		private static string Caller()
		{
			try
			{
				System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
				for (int i = 1; i < trace.FrameCount; i++)
				{
					Type type = trace.GetFrame(i).GetMethod()?.DeclaringType;
					if (type != null && type.Assembly != typeof(MoreFish).Assembly)
					{
						return type.Assembly.GetName().Name;
					}
				}
			}
			catch (Exception)
			{
			}

			return "unknown";
		}
	}
}
