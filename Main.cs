using BepInEx;
using HarmonyLib;
using System;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using ServerSync;

namespace Mjolnir
{

    [BepInPlugin(PluginId, "Mjolnir", version)]
    public class Mjolnir : BaseUnityPlugin
    {
        public const string version = "1.0.1";
        public const string PluginId = "azumatt.Mjolnir";
        public const string Author = "Azumatt";
        public const string PluginName = "Mjolnir";
        ConfigSync configSync = new ConfigSync(PluginId) { DisplayName = PluginName, CurrentVersion = version, MinimumRequiredVersion = version };
        public static Mjolnir Instance { get; private set; }
        private Harmony _harmony;
        private static GameObject mjolnir;
        private ConfigFile localizationFile;
        private Dictionary<string, ConfigEntry<string>> m_localizedStrings = new Dictionary<string, ConfigEntry<string>>();

        #region Configs
        public static ConfigEntry<bool> serverConfigLocked;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> noCraft;

        public static ConfigEntry<int> reqm_minStationLevel;

        public static ConfigEntry<string> req1Prefab;
        public static ConfigEntry<string> req2Prefab;
        public static ConfigEntry<string> req3Prefab;
        public static ConfigEntry<string> req4Prefab;

        public static ConfigEntry<int> req1Amount;
        public static ConfigEntry<int> req2Amount;
        public static ConfigEntry<int> req3Amount;
        public static ConfigEntry<int> req4Amount;

        public static ConfigEntry<int> req1APL;
        public static ConfigEntry<int> req2APL;
        public static ConfigEntry<int> req3APL;
        public static ConfigEntry<int> req4APL;

        public bool UpdateRecipe = false;
        public static Recipe recipe;

        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }
        #endregion

        private void Awake()
        {
            serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            configSync.AddLockingConfigEntry(serverConfigLocked);
            nexusID = config("General", "NexusID", 1357, new ConfigDescription("Nexus mod ID for updates", null, new ConfigurationManagerAttributes()), false);
            ConfigEntry<T> itemConfig<T>(string item, string name, T value, string description)
            {
                ConfigEntry<T> configEntry = config("Recipe " + item, name, value, description, true);
                configEntry.SettingChanged += (s, e) => UpdateRecipe = true;
                return configEntry;
            }

            /* No-Craft */
            noCraft = config<bool>("General", "Not Craftable", false, "Makes the Mjolnir non-craftable, ignoring all settings below", true);
            /* Item 1 */
            req1Prefab = itemConfig("Item 1", "Required Prefab", "FineWood", "Required item for crafting");
            req1Amount = itemConfig("Item 1", "Amount Required", 30, "Amount needed of this item for crafting");
            req1APL = itemConfig("Item 1", "Amount Per Level", 10, "Amount to increase crafting cost by for each level of the item");

            /* Item 2 */
            req2Prefab = itemConfig("Item 2", "Required Prefab", "Stone", "Required item for crafting");
            req2Amount = itemConfig("Item 2", "Amount Required", 30, "Amount needed of this item for crafting");
            req2APL = itemConfig("Item 2", "Amount Per Level", 10, "Amount to increase crafting cost by for each level of the item");

            /* Item 3 */
            req3Prefab = itemConfig("Item 3", "Required Prefab", "SledgeIron", "Required item for crafting");
            req3Amount = itemConfig("Item 3", "Amount Required", 1, "Amount needed of this item for crafting");
            req3APL = itemConfig("Item 3", "Amount Per Level", 1, "Amount to increase crafting cost by for each level of the item");

            /* Item 4 */
            req4Prefab = itemConfig("Item 4", "Required Prefab", "DragonTear", "Required item for crafting");
            req4Amount = itemConfig("Item 4", "Amount Required", 3, "Amount needed of this item for crafting");
            req4APL = itemConfig("Item 4", "Amount Per Level", 1, "Amount to increase crafting cost by for each level of the item");

            localizationFile = new ConfigFile(Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath), PluginId + ".Localization.cfg"), false);

            LoadAssets();

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
            Localize();
        }

        private void Update()
        {
            if (UpdateRecipe && !noCraft.Value)
            {
                Recipe();
            }
            if (ObjectDB.instance.m_recipes.Contains(recipe) && noCraft.Value)
            {
                ObjectDB.instance.m_recipes.Remove(recipe);
            }
            else if (!ObjectDB.instance.m_recipes.Contains(recipe) && !noCraft.Value)
            {
                ObjectDB.instance.m_recipes.Add(recipe);
            }
        }
        public static void TryRegisterFabs(ZNetScene zNetScene)
        {
            if (zNetScene == null || zNetScene.m_prefabs == null || zNetScene.m_prefabs.Count <= 0)
            {
                return;
            }
            zNetScene.m_prefabs.Add(mjolnir);

        }
        private static AssetBundle GetAssetBundleFromResources(string filename)
        {
            var execAssembly = Assembly.GetExecutingAssembly();
            var resourceName = execAssembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(filename));

            using (var stream = execAssembly.GetManifestResourceStream(resourceName))
            {
                return AssetBundle.LoadFromStream(stream);
            }
        }
        public static void LoadAssets()
        {
            AssetBundle assetBundle = GetAssetBundleFromResources("mjolnir");
            mjolnir = assetBundle.LoadAsset<GameObject>("Mjolnir");
            assetBundle?.Unload(false);

        }
        public static void RegisterItems()
        {
            if (ObjectDB.instance.m_items.Count == 0 || ObjectDB.instance.GetItemPrefab("Amber") == null)
            {
                return;
            }
            var itemDrop = mjolnir.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                if (ObjectDB.instance.GetItemPrefab(mjolnir.name.GetStableHashCode()) == null)
                {
                    ObjectDB.instance.m_items.Add(mjolnir);
                }
            }

        }

        public static void AddSomeRecipes()
        {
            try
            {
                if (ObjectDB.instance.m_recipes.Count() == 0)
                {
                    //Mjolnir.LogInfo("Recipe database not ready for stuff, skipping initialization.");
                    return;
                }
                Recipe();

                ObjectDB.instance.UpdateItemHashes();
            }
            catch (Exception exc)
            {
                Debug.Log(exc);
            }
        }
        public static void Recipe()
        {
            var db = ObjectDB.instance.m_items;
            try
            {
                db.Remove(mjolnir);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error removing Mjolnir from ODB  :{ex}");
            }
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<Recipe>();
            }
            if (!ObjectDB.instance.m_recipes.Contains(recipe))
            {
                ObjectDB.instance.m_recipes.Add(recipe);
            }
            GameObject thing1 = ObjectDB.instance.GetItemPrefab(req1Prefab.Value);
            GameObject thing2 = ObjectDB.instance.GetItemPrefab(req2Prefab.Value);
            GameObject thing3 = ObjectDB.instance.GetItemPrefab(req3Prefab.Value);
            GameObject thing4 = ObjectDB.instance.GetItemPrefab(req4Prefab.Value);
            recipe.name = "RecipeMjolnir";
            recipe.m_craftingStation = ZNetScene.instance.GetPrefab("forge").GetComponent<CraftingStation>();
            recipe.m_repairStation = ZNetScene.instance.GetPrefab("forge").GetComponent<CraftingStation>();
            recipe.m_amount = 1;
            recipe.m_minStationLevel = 4;
            recipe.m_item = mjolnir.GetComponent<ItemDrop>();
            recipe.m_enabled = true;
            recipe.m_resources = new Piece.Requirement[]
            {
                new Piece.Requirement(){m_resItem = thing1.GetComponent<ItemDrop>(), m_amount = req1Amount.Value, m_amountPerLevel = req1APL.Value, m_recover = true},
                new Piece.Requirement(){m_resItem = thing2.GetComponent<ItemDrop>(), m_amount = req2Amount.Value, m_amountPerLevel = req2APL.Value, m_recover = true},
                new Piece.Requirement(){m_resItem = thing3.GetComponent<ItemDrop>(), m_amount = req3Amount.Value, m_amountPerLevel = req3APL.Value, m_recover = true},
                new Piece.Requirement(){m_resItem = thing4.GetComponent<ItemDrop>(), m_amount = req4Amount.Value, m_amountPerLevel = req4APL.Value, m_recover = true}
            };
            try
            {
                db.Add(mjolnir);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding Mjolnir to ODB  :{ex}");
            }
        }


        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class MJOLZNetScene_Awake_Patch
        {
            public static bool Prefix(ZNetScene __instance)
            {
                TryRegisterFabs(__instance);
                return true;
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class MJOLObjectDB_Awake_Patch
        {
            public static void Postfix()
            {
                RegisterItems();
                AddSomeRecipes();
            }
        }
        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class MJOLObjectDB_CopyOtherDB_Patch
        {
            public static void Postfix()
            {
                RegisterItems();
                AddSomeRecipes();
            }
        }


        private void OnDestroy()
        {
            localizationFile.Save();
            _harmony?.UnpatchSelf();
        }

        private void Localize()
        {
            LocalizeWord("item_mjolnir", "Mjölnir");
            LocalizeWord("item_mjolnir_description", "Whosoever holds this hammer, if he be worthy, shall possess the power of Thor.");

        }

        public string LocalizeWord(string key, string val)
        {
            if (!m_localizedStrings.ContainsKey(key))
            {
                var loc = Localization.instance;
                var langSection = loc.GetSelectedLanguage();
                var configEntry = localizationFile.Bind(langSection, key, val);
                Localization.instance.AddWord(key, configEntry.Value);
                m_localizedStrings.Add(key, configEntry);
            }

            return $"${key}";
        }
    }
}