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

namespace Mjolnir
{

    [BepInPlugin(PluginId, "Mjolnir", version)]
    public class Mjolnir : BaseUnityPlugin
    {
        public const string PluginId = "azumatt.Mjolnir";
        public const string Author = "Azumatt";
        public const string PluginName = "Mjolnir";
        public const string Version = "0.1.2";
        private Harmony _harmony;
        private static GameObject mjolnir;
        public static ManualLogSource logSource;
        private ConfigFile m_localizationFile;
        private Dictionary<string, ConfigEntry<string>> m_localizedStrings = new Dictionary<string, ConfigEntry<string>>();
        public const string version = "1.0.0";

        private void Awake()
        {
            m_localizationFile = new ConfigFile(Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath), PluginId + ".Localization.cfg"), false);
            LoadAssets();

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
            try
            {
                if (ObjectDB.instance.m_recipes.Count() == 0)
                {
                    //logSource.LogInfo("Recipe database not ready for stuff, skipping initialization.");
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
            GameObject thing1 = ObjectDB.instance.GetItemPrefab("FineWood");
            GameObject thing2 = ObjectDB.instance.GetItemPrefab("Stone");
            GameObject thing3 = ObjectDB.instance.GetItemPrefab("SledgeIron");
            GameObject thing4 = ObjectDB.instance.GetItemPrefab("DragonTear");
            Recipe newRecipe = ScriptableObject.CreateInstance<Recipe>();
            newRecipe.name = "RecipeMjolnir";
            newRecipe.m_craftingStation = ObjectDB.instance.GetItemPrefab("forge").GetComponentInChildren<CraftingStation>(true);
            newRecipe.m_repairStation = ObjectDB.instance.GetItemPrefab("forge").GetComponentInChildren<CraftingStation>(true);
            newRecipe.m_amount = 1;
            newRecipe.m_minStationLevel = 1;
            newRecipe.m_item = mjolnir.GetComponent<ItemDrop>();
            newRecipe.m_enabled = true;
            newRecipe.m_resources = new Piece.Requirement[]{
                new Piece.Requirement(){m_resItem = thing1.GetComponent<ItemDrop>(), m_amount = 30, m_amountPerLevel =10, m_recover = true},
                new Piece.Requirement(){m_resItem = thing2.GetComponent<ItemDrop>(), m_amount =30, m_amountPerLevel = 10, m_recover = true},
                new Piece.Requirement(){m_resItem = thing3.GetComponent<ItemDrop>(), m_amount = 1, m_amountPerLevel = 1, m_recover = true},
                new Piece.Requirement(){m_resItem = thing4.GetComponent<ItemDrop>(), m_amount = 3, m_amountPerLevel = 1, m_recover = true}
            };
            //logSource.LogInfo("Loaded mjolnir Recipe");
            ObjectDB.instance.m_recipes.Add(newRecipe);
        }


        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetScene_Awake_Patch
        {
            public static bool Prefix(ZNetScene __instance)
            {
                TryRegisterFabs(__instance);
                //logSource.LogInfo("Loading the prefabs");
                return true;
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class ObjectDB_Awake_Patch
        {
            public static void Postfix()
            {
                //logSource.LogInfo("Trying to register Items");
                RegisterItems();
                AddSomeRecipes();
            }
        }
        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class ObjectDB_CopyOtherDB_Patch
        {
            public static void Postfix()
            {
                RegisterItems();
                AddSomeRecipes();
            }
        }
        private void OnDestroy()
        {
            m_localizationFile.Save();
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
                var configEntry = m_localizationFile.Bind(langSection, key, val);
                Localization.instance.AddWord(key, configEntry.Value);
                m_localizedStrings.Add(key, configEntry);
            }

            return $"${key}";
        }
    }
}