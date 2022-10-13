using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;

namespace Mjolnir
{
    [BepInPlugin(PluginId, PluginName, version)]
    public partial class Mjolnir : BaseUnityPlugin
    {
        public const string version = "1.3.0";
        public const string PluginId = "Azumatt.Mjolnir";
        public const string Author = "Azumatt";
        public const string PluginName = "Mjolnir";
        private static GameObject mjolnir;
        public static readonly ManualLogSource MJOLLogger = BepInEx.Logging.Logger.CreateLogSource(PluginName);

        private readonly ConfigSync configSync = new ConfigSync(PluginId)
            { DisplayName = PluginName, CurrentVersion = version, MinimumRequiredVersion = version };

        private readonly Dictionary<string, ConfigEntry<string>> m_localizedStrings =
            new Dictionary<string, ConfigEntry<string>>();

        private Harmony _harmony;
        private bool flight;

        private ConfigFile localizationFile;

        public static Mjolnir Instance { get; private set; }

        private void Awake()
        {
            serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            configSync.AddLockingConfigEntry(serverConfigLocked);

            ConfigEntry<T> itemConfig<T>(string item, string name, T value, string description)
            {
                ConfigEntry<T> configEntry = config("Recipe " + item, name, value, description);
                configEntry.SettingChanged += (s, e) => UpdateRecipe = true;
                return configEntry;
            }

            /* No-Craft */
            noCraft = config("General", "Not Craftable", true,
                "Makes the Mjolnir non-craftable");

            /* No-Flight */
            noFlight = config("General", "No Flight", false,
                "Makes the Mjolnir less...Mjolnir. Disable the flight (but why though? It's Mjolnir!)");
            noFlightMessage = config("General", "No Flight Message",
                "Your God-Like ability to fly is suppressed by Odin himself",
                "Message to show when flight is denied to the player. Can make blank to hide message.", false);
            _flightHotKey = config("General", "FlightHotKey", KeyCode.Z,
                "Personal hotkey to toggle a flight", false);

            /* Item 1 */
            req1Prefab = itemConfig("Item 1", "Required Prefab", "FineWood", "Required item for crafting");
            req1Amount = itemConfig("Item 1", "Amount Required", 30, "Amount needed of this item for crafting");
            req1APL = itemConfig("Item 1", "Amount Per Level", 10,
                "Amount to increase crafting cost by for each level of the item");

            /* Item 2 */
            req2Prefab = itemConfig("Item 2", "Required Prefab", "Stone", "Required item for crafting");
            req2Amount = itemConfig("Item 2", "Amount Required", 30, "Amount needed of this item for crafting");
            req2APL = itemConfig("Item 2", "Amount Per Level", 10,
                "Amount to increase crafting cost by for each level of the item");

            /* Item 3 */
            req3Prefab = itemConfig("Item 3", "Required Prefab", "SledgeIron", "Required item for crafting");
            req3Amount = itemConfig("Item 3", "Amount Required", 1, "Amount needed of this item for crafting");
            req3APL = itemConfig("Item 3", "Amount Per Level", 1,
                "Amount to increase crafting cost by for each level of the item");

            /* Item 4 */
            req4Prefab = itemConfig("Item 4", "Required Prefab", "DragonTear", "Required item for crafting");
            req4Amount = itemConfig("Item 4", "Amount Required", 3, "Amount needed of this item for crafting");
            req4APL = itemConfig("Item 4", "Amount Per Level", 1,
                "Amount to increase crafting cost by for each level of the item");

            /* Damage */
            baseDamage = config("Damage", "Base Damage", 100, "");
            baseBlunt = config("Damage", "Base Blunt Damage", 200, "");
            baseSlash = config("Damage", "Base Slash Damage", 1, "");
            basePierce = config("Damage", "Base Pierce Damage", 1, "");
            baseChop = config("Damage", "Base Chop Damage", 0, "");
            basePickaxe = config("Damage", "Base Pickaxe Damage", 1, "");
            baseFire = config("Damage", "Base Fire Damage", 1, "");
            baseFrost = config("Damage", "Base Frost Damage", 1, "");
            baseLightning = config("Damage", "Base Lightning Damage", 500, "");
            basePoison = config("Damage", "Base Poison Damage", 1, "");
            baseSpirit = config("Damage", "Base Spirit Damage", 1, "");
            baseDamagePerPerLevel = config("Damage", "Base Damage Per Level", 100, "");
            baseBluntPerLevel = config("Damage", "Base Blunt Damage Per Level", 100, "");
            baseSlashPerLevel = config("Damage", "Base Slash Damage Per Level", 1, "");
            basePiercePerLevel = config("Damage", "Base Pierce Damage Per Level", 1, "");
            baseChopPerLevel = config("Damage", "Base Chop Damage Per Level", 1, "");
            basePickaxePerLevel = config("Damage", "Base Pickaxe Damage Per Level", 1, "");
            baseFirePerLevel = config("Damage", "Base Fire Damage Per Level", 1, "");
            baseFrostPerLevel = config("Damage", "Base Frost Damage Per Level", 1, "");
            baseLightningPerLevel = config("Damage", "Base Lightning Damage Per Level", 200, "");
            basePoisonPerLevel = config("Damage", "Base Poison Damage Per Level", 1, "");
            baseSpiritPerLevel = config("Damage", "Base Spirit Damage Per Level", 1, "");
            baseAttackForce = config("Damage", "Base Attack Force (a.k.a Knockback)", 200, "");
            baseBlockPower = config("Damage", "Base Block Power", 500, "");
            baseParryForce = config("Damage", "Base Parry Force", 20, "");
            baseBackstab = config("Damage", "Base Backstab Bonus", 3, "");


            localizationFile =
                new ConfigFile(
                    Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath) ?? throw new InvalidOperationException(),
                        PluginId + ".Localization.cfg"), false);

            LoadAssets();

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
            Localize();
            AnimationAwake();
        }

        private void Update()
        {
            if (!Player.m_localPlayer) return;
            if (UpdateRecipe && !noCraft.Value) Recipe();
            if (ObjectDB.instance.m_recipes.Contains(recipe) && noCraft.Value)
                ObjectDB.instance.m_recipes.Remove(recipe);
            else if (!ObjectDB.instance.m_recipes.Contains(recipe) && !noCraft.Value)
                ObjectDB.instance.m_recipes.Add(recipe);
            if (!Input.GetKeyDown(_flightHotKey.Value)) return;
            if (Player.m_localPlayer.GetCurrentWeapon()?.m_dropPrefab?.name != "Mjolnir") return;
            //Quaternion? rotation = Player.m_localPlayer.GetCurrentWeapon()?.m_dropPrefab.transform.rotation;
            if (flight)
            {
                /* Disable flight */
                flight = !flight;
                Player.m_localPlayer.m_body.useGravity = flight;
                if (!Player.m_localPlayer.IsDebugFlying())
                    Player.m_localPlayer.m_animator.runtimeAnimatorController = _origDebugFly;
                Player.m_localPlayer.m_zanim.SetTrigger("emote_stop");
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Mjolnir fly:" + flight);
            }
            else
            {
                /* Enable flight */
                if (noFlight.Value)
                {
                    if (Player.m_localPlayer.TakeInput())
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, noFlightMessage.Value);
                    return;
                }

                flight = !flight;
                Player.m_localPlayer.m_body.useGravity = flight;
                Player.m_localPlayer.m_animator.runtimeAnimatorController = _customDebugFly;
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Mjolnir fly:" + flight);
            }
        }

        public virtual void FixedUpdate()
        {
            try
            {
                if (!Player.m_localPlayer)
                    return;
                float fixedDeltaTime = Time.fixedDeltaTime;
                if (Player.m_localPlayer.GetCurrentWeapon()?.m_dropPrefab?.name != "Mjolnir")
                    /* Disable flight if the player isn't holding Mjolnir */
                    flight = false;


                UpdateMotion(fixedDeltaTime);
            }
            catch (Exception e)
            {
                MJOLLogger.LogError($"{e}");
            }
        }

        public void UpdateMotion(float dt)
        {
            Player p = Player.m_localPlayer;
            p.m_sliding = false;
            p.m_wallRunning = false;
            p.m_running = false;
            p.m_walking = false;
            if (p.IsDead())
                return;
            if (flight)
            {
                p.m_collider.material.staticFriction = 0.0f;
                p.m_collider.material.dynamicFriction = 0.0f;
                UpdateMjolnirFlight(dt);
            }
        }

        public void UpdateMjolnirFlight(float dt)
        {
            Player p = Player.m_localPlayer;
            p.UseStamina(dt);
            if (p.m_stamina == 0f)
            {
                Player.m_localPlayer.m_zanim.SetTrigger("emote_stop");
                flight = !flight;
                Player.m_localPlayer.m_body.useGravity = flight;
                Player.m_localPlayer.m_animator.runtimeAnimatorController = _origDebugFly;
                // Player.m_localPlayer.m_nview.GetZDO().Set("DebugFly", this.flight);
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Mjolnir fly:" + flight);
            }

            float num = p.m_run ? 50f : 20f;
            Vector3 b = p.m_moveDir * num;
            if (p.TakeInput())
            {
                if (ZInput.GetButton("Jump"))
                    b.y = num;
                else if (Input.GetKey(KeyCode.LeftControl))
                    b.y = -num;
            }

            p.m_currentVel = Vector3.Lerp(p.m_currentVel, b, 0.5f);
            p.m_body.velocity = p.m_currentVel;
            p.m_body.useGravity = false;
            p.m_lastGroundTouch = 0.0f;
            Transform transform1 = p.transform;
            p.m_maxAirAltitude = transform1.position.y;
            p.m_body.rotation = Quaternion.RotateTowards(transform1.rotation, p.m_lookYaw, p.m_turnSpeed * dt);
            p.m_body.angularVelocity = Vector3.zero;
            p.UpdateEyeRotation();
        }

        public static void TryRegisterFabs(ZNetScene zNetScene)
        {
            if (zNetScene == null || zNetScene.m_prefabs == null || zNetScene.m_prefabs.Count <= 0) return;
            zNetScene.m_prefabs.Add(mjolnir);
        }

        private static AssetBundle GetAssetBundleFromResources(string filename)
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = execAssembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(filename));

            using (Stream stream = execAssembly.GetManifestResourceStream(resourceName))
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
            if (ObjectDB.instance.m_items.Count == 0 || ObjectDB.instance.GetItemPrefab("Wood") == null) return;
            ItemDrop itemDrop = mjolnir.GetComponent<ItemDrop>();
            if (itemDrop != null)
                if (ObjectDB.instance.GetItemPrefab(mjolnir.name.GetStableHashCode()) == null)
                    ObjectDB.instance.m_items.Add(mjolnir);
        }

        public static void AddSomeRecipes()
        {
            try
            {
                if (!ObjectDB.instance.m_recipes.Any())
                    return;
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
            List<GameObject> db = ObjectDB.instance.m_items;
            try
            {
                db.Remove(mjolnir);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error removing Mjolnir from ODB  :{ex}");
            }

            if (recipe == null) recipe = ScriptableObject.CreateInstance<Recipe>();
            if (!ObjectDB.instance.m_recipes.Contains(recipe)) ObjectDB.instance.m_recipes.Add(recipe);
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
            recipe.m_resources = new[]
            {
                new Piece.Requirement
                {
                    m_resItem = thing1.GetComponent<ItemDrop>(), m_amount = req1Amount.Value,
                    m_amountPerLevel = req1APL.Value, m_recover = true
                },
                new Piece.Requirement
                {
                    m_resItem = thing2.GetComponent<ItemDrop>(), m_amount = req2Amount.Value,
                    m_amountPerLevel = req2APL.Value, m_recover = true
                },
                new Piece.Requirement
                {
                    m_resItem = thing3.GetComponent<ItemDrop>(), m_amount = req3Amount.Value,
                    m_amountPerLevel = req3APL.Value, m_recover = true
                },
                new Piece.Requirement
                {
                    m_resItem = thing4.GetComponent<ItemDrop>(), m_amount = req4Amount.Value,
                    m_amountPerLevel = req4APL.Value, m_recover = true
                }
            };
            InitDamageValues(recipe.m_item);
            try
            {
                db.Add(mjolnir);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding Mjolnir to ODB  :{ex}");
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
            LocalizeWord("item_mjolnir_description",
                "Whosoever holds this hammer, if he be worthy, shall possess the power of Thor.");
        }

        public string LocalizeWord(string key, string val)
        {
            if (m_localizedStrings.ContainsKey(key)) return $"${key}";
            Localization loc = Localization.instance;
            string langSection = loc.GetSelectedLanguage();
            ConfigEntry<string> configEntry = localizationFile.Bind(langSection, key, val);
            Localization.instance.AddWord(key, configEntry.Value);
            m_localizedStrings.Add(key, configEntry);

            return $"${key}";
        }

        private static void InitDamageValues(ItemDrop item)
        {
            ItemDrop itmdrop = item;
            itmdrop.m_itemData.m_shared.m_damages.m_damage = baseDamage.Value;
            itmdrop.m_itemData.m_shared.m_toolTier = baseBlunt.Value;
            itmdrop.m_itemData.m_shared.m_damages.m_blunt = baseBlunt.Value;
            itmdrop.m_itemData.m_shared.m_damages.m_slash = baseSlash.Value;
            itmdrop.m_itemData.m_shared.m_damages.m_pierce = basePierce.Value;
            itmdrop.m_itemData.m_shared.m_damages.m_chop = baseChop.Value;
            itmdrop.m_itemData.m_shared.m_damages.m_pickaxe = basePickaxe.Value;
            itmdrop.m_itemData.m_shared.m_damages.m_fire = baseFire.Value;
            itmdrop.m_itemData.m_shared.m_damages.m_frost = baseFrost.Value;
            itmdrop.m_itemData.m_shared.m_damages.m_lightning = baseLightning.Value;
            itmdrop.m_itemData.m_shared.m_damages.m_poison = basePoison.Value;
            itmdrop.m_itemData.m_shared.m_damages.m_spirit = baseSpirit.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_damage = baseDamagePerPerLevel.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_blunt = baseBluntPerLevel.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_slash = baseSlashPerLevel.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_pierce = basePiercePerLevel.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_chop = baseChopPerLevel.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_pickaxe = basePickaxePerLevel.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_fire = baseFirePerLevel.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_frost = baseFrostPerLevel.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_lightning = baseLightningPerLevel.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_poison = basePoisonPerLevel.Value;
            itmdrop.m_itemData.m_shared.m_damagesPerLevel.m_spirit = baseSpiritPerLevel.Value;
            itmdrop.m_itemData.m_shared.m_attackForce = baseAttackForce.Value;
            itmdrop.m_itemData.m_shared.m_blockPower = baseBlockPower.Value;
            itmdrop.m_itemData.m_shared.m_deflectionForce = baseParryForce.Value;
            itmdrop.m_itemData.m_shared.m_backstabBonus = baseBackstab.Value;
        }


        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        public static class MJOLZNetScene_Awake_Patch
        {
            public static bool Prefix(ZNetScene __instance)
            {
                TryRegisterFabs(__instance);
                return true;
            }
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class MJOLObjectDB_Awake_Patch
        {
            public static void Postfix()
            {
                RegisterItems();
                AddSomeRecipes();
            }
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        public static class MJOLObjectDB_CopyOtherDB_Patch
        {
            public static void Postfix()
            {
                RegisterItems();
                AddSomeRecipes();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AddKnownRecipe))]
        public static class MJOLRecipe_AddKnown_Patch
        {
            public static bool Prefix(Player __instance, Recipe recipe)
            {
                switch (recipe.m_item.m_itemData.m_shared.m_name)
                {
                    case "RecipeMjolnir" when noCraft.Value:
                        return false;
                    case "RecipeMjolnir" when !noCraft.Value:
                        return true;
                    default:
                        return true;
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.FixedUpdate))]
        public static class MJOLRecipe_Known_Patch
        {
            public static void Prefix(Player __instance)
            {
                if (__instance.m_knownRecipes.Contains("RecipeMjolnir") && !noCraft.Value)
                    __instance.m_knownRecipes.Remove("RecipeMjolnir");
                InitDamageValues(mjolnir.GetComponent<ItemDrop>());
                //AddSomeRecipes();
            }
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
        private static class MJOL_SyncConfigDamage
        {
            private static void Postfix()
            {
                InitDamageValues(mjolnir.GetComponent<ItemDrop>());
            }
        }

        #region Configs

        public static ConfigEntry<bool> serverConfigLocked;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> noCraft;
        public static ConfigEntry<bool> noFlight;
        public static ConfigEntry<string> noFlightMessage;
        private static ConfigEntry<KeyCode> _flightHotKey;

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

        /* Damage */
        public static ConfigEntry<int> baseDamage;
        public static ConfigEntry<int> baseBlunt;
        public static ConfigEntry<int> baseSlash;
        public static ConfigEntry<int> basePierce;
        public static ConfigEntry<int> baseChop;
        public static ConfigEntry<int> basePickaxe;
        public static ConfigEntry<int> baseFire;
        public static ConfigEntry<int> baseFrost;
        public static ConfigEntry<int> baseLightning;
        public static ConfigEntry<int> basePoison;
        public static ConfigEntry<int> baseSpirit;
        public static ConfigEntry<int> baseDamagePerPerLevel;
        public static ConfigEntry<int> baseBluntPerLevel;
        public static ConfigEntry<int> baseSlashPerLevel;
        public static ConfigEntry<int> basePiercePerLevel;
        public static ConfigEntry<int> baseChopPerLevel;
        public static ConfigEntry<int> basePickaxePerLevel;
        public static ConfigEntry<int> baseFirePerLevel;
        public static ConfigEntry<int> baseFrostPerLevel;
        public static ConfigEntry<int> baseLightningPerLevel;
        public static ConfigEntry<int> basePoisonPerLevel;
        public static ConfigEntry<int> baseSpiritPerLevel;
        public static ConfigEntry<int> baseAttackForce;
        public static ConfigEntry<int> baseBlockPower; // block power
        public static ConfigEntry<int> baseParryForce; // parry force
        public static ConfigEntry<int> baseKnockbackForce; // knockback force
        public static ConfigEntry<int> baseBackstab; // backstab


        public bool UpdateRecipe;
        public static Recipe recipe;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new ConfigDescription(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        #endregion
    }
}