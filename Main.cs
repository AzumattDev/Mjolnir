using BepInEx;
using HarmonyLib;
using System;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Collections;
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
        public const string version = "1.0.0";
        public const string PluginId = "azumatt.Mjolnir";
        public const string Author = "Azumatt";
        public const string PluginName = "Mjolnir";
        private static Mjolnir context;
        ConfigSync configSync = new ConfigSync(PluginId) { DisplayName = PluginName, CurrentVersion = version, MinimumRequiredVersion = version };
        public static Mjolnir Instance { get; private set; }
        private Harmony _harmony;
        private static GameObject mjolnir;
        private ConfigFile localizationFile;
        private Dictionary<string, ConfigEntry<string>> m_localizedStrings = new Dictionary<string, ConfigEntry<string>>();

        #region Configs
        public static ConfigEntry<bool> serverConfigLocked;
        public static ConfigEntry<int> nexusID;

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

        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        #endregion

        private void Awake()
        {
            serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            configSync.AddLockingConfigEntry(serverConfigLocked);
            nexusID = config("General", "NexusID", 1357, "Nexus mod ID for updates");
            /* Item 1 */
            req1Prefab = config("Recipe Item 1", "Prefab", "FineWood", "Item you want required to craft", true);
            req1Amount = config("Recipe Item 1", "Amount Required", 30, "Amount needed of this item for crafting", true);
            req1APL = config("Recipe Item 1", "Amount Per Level", 10, "Amount to increase crafting cost by for each level of the item", true);

            /* Item 2 */
            req2Prefab = config("Recipe Item 2", "Prefab", "Stone", "Item you want required to craft", true);
            req2Amount = config("Recipe Item 2", "Amount Required", 30, "Amount needed of this item for crafting", true);
            req2APL = config("Recipe Item 2", "Amount Per Level", 10, "Amount to increase crafting cost by for each level of the item", true);

            /* Item 3 */
            req3Prefab = config("Recipe Item 3", "Prefab", "SledgeIron", "Item you want required to craft", true);
            req3Amount = config("Recipe Item 3", "Amount Required", 1, "Amount needed of this item for crafting", true);
            req3APL = config("Recipe Item 3", "Amount Per Level", 1, "Amount to increase crafting cost by for each level of the item", true);

            /* Item 4 */
            req4Prefab = config("Recipe Item 4", "Prefab", "DragonTear", "Item you want required to craft", true);
            req4Amount = config("Recipe Item 4", "Amount Required", 3, "Amount needed of this item for crafting", true);
            req4APL = config("Recipe Item 4", "Amount Per Level", 1, "Amount to increase crafting cost by for each level of the item", true);

            localizationFile = new ConfigFile(Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath), PluginId + ".Localization.cfg"), false);

            LoadAssets();

            if (ConfigSync.ProcessingServerUpdate)
            {
                try
                {
                    LoadAllRecipeData();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed update for ConfigSync.ProcessingServerUpdate {ex}");
                }
            }

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
            Localize();
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
            if (ObjectDB.instance.m_recipes.Count() == 0)
            {
                //Mjolnir.LogInfo("Recipe database not ready for stuff, skipping initialization.");
                return;
            }

            try
            {
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
            GameObject thing1 = ObjectDB.instance.GetItemPrefab(req1Prefab.Value);
            GameObject thing2 = ObjectDB.instance.GetItemPrefab(req2Prefab.Value);
            GameObject thing3 = ObjectDB.instance.GetItemPrefab(req3Prefab.Value);
            GameObject thing4 = ObjectDB.instance.GetItemPrefab(req4Prefab.Value);
            Recipe newRecipe = ScriptableObject.CreateInstance<Recipe>();
            newRecipe.name = "RecipeMjolnir";
            newRecipe.m_craftingStation = ZNetScene.instance.GetPrefab("forge").GetComponent<CraftingStation>();
            newRecipe.m_repairStation = ZNetScene.instance.GetPrefab("forge").GetComponent<CraftingStation>();
            newRecipe.m_amount = 1;
            newRecipe.m_minStationLevel = 4;
            newRecipe.m_item = mjolnir.GetComponent<ItemDrop>();
            newRecipe.m_enabled = true;
            newRecipe.m_resources = new Piece.Requirement[]
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
            if (!ObjectDB.instance.m_recipes.Contains(newRecipe))
                ObjectDB.instance.m_recipes.Add(newRecipe);
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

        //[HarmonyPatch(typeof(ZNetScene), "Awake")]
        //[HarmonyPriority(Priority.Last)]
        //static class MJOLZNetScene_Awake_PostPatch
        //{
        //    static void Postfix()
        //    {
        //        try
        //        {
        //            context.StartCoroutine(DelayedLoadRecipes());
        //            LoadAllRecipeData();
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.LogError($"{ex}");
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        //[HarmonyPriority(Priority.Last)]
        //static class MJOLZNet_OnNewConnection_PostPatch
        //{
        //    static void Postfix()
        //    {
        //        try
        //        {
        //            context.StartCoroutine(DelayedLoadRecipes());
        //            LoadAllRecipeData();
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.LogError($"{ex}");
        //        }
        //    }
        //}

        public static IEnumerator DelayedLoadRecipes()
        {
            yield return null;
            LoadAllRecipeData();
            yield break;
        }

        private static void LoadAllRecipeData()
        {
            if (mjolnir.GetComponent<ItemDrop>() == null)
            {
                Debug.LogError($"Item data for {mjolnir.name} not found!");
                return;
            }
            //for (int i = ObjectDB.instance.m_recipes.Count - 1; i > 0; i--)
            for (int i = 0; i < ObjectDB.instance.m_recipes.Count; i++)
            {
                if (ObjectDB.instance.m_recipes[i].m_item?.m_itemData.m_shared.m_name == mjolnir.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                {
                    if (!mjolnir.gameObject.activeSelf)
                    {
                        Debug.LogError($"Removing recipe for {mjolnir.name} from the game");
                        ObjectDB.instance.m_recipes.RemoveAt(i);
                        return;
                    }

                    ObjectDB.instance.m_recipes[i].m_amount = 1;
                    ObjectDB.instance.m_recipes[i].m_minStationLevel = 4;
                    ObjectDB.instance.m_recipes[i].m_craftingStation = ZNetScene.instance.GetPrefab("forge").GetComponent<CraftingStation>();
                    List<Piece.Requirement> reqs = new List<Piece.Requirement>();

                    reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(req1Prefab.Value).GetComponent<ItemDrop>(), m_amount = req1Amount.Value, m_amountPerLevel = req1APL.Value, m_recover = true });
                    reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(req2Prefab.Value).GetComponent<ItemDrop>(), m_amount = req2Amount.Value, m_amountPerLevel = req2APL.Value, m_recover = true });
                    reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(req3Prefab.Value).GetComponent<ItemDrop>(), m_amount = req3Amount.Value, m_amountPerLevel = req3APL.Value, m_recover = true });
                    reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(req4Prefab.Value).GetComponent<ItemDrop>(), m_amount = req4Amount.Value, m_amountPerLevel = req4APL.Value, m_recover = true });

                    ObjectDB.instance.m_recipes[i].m_resources = reqs.ToArray();
                    return;
                }
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
            LocalizeWord("item_mjolnir_description", "The powerful hammer of the Thunder God Thor");

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