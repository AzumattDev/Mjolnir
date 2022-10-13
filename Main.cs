using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ItemManager;
using LocalizationManager;
using ServerSync;
using UnityEngine;

namespace Mjolnir
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public partial class Mjolnir : BaseUnityPlugin
    {
        public const string ModVersion = "1.3.0";
        public const string ModGUID = "Azumatt.Mjolnir";
        public const string ModAuthor = "Azumatt";
        public const string ModName = "Mjolnir";
        private static GameObject mjolnir;
        public static readonly ManualLogSource MJOLLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        internal static string ConnectionError = "";

        internal readonly ConfigSync configSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        private readonly Dictionary<string, ConfigEntry<string>> m_localizedStrings = new();

        private Harmony _harmony;
        private bool flight;

        private ConfigFile localizationFile;

        public static Mjolnir Instance { get; private set; }
        
        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        private void Awake()
        {
            Instance = this;
            Localizer.Load();
            Util.Functions.ConfigGen();

            Item mjolnirItem = new("mjolnir", "Mjolnir", "EmbeddedAsset");
            mjolnirItem.Name.English("Mjölnir");
            mjolnirItem.Description.English("Whosoever holds this hammer, if he be worthy, shall possess the power of Thor.");
            mjolnirItem.Crafting.Add(CraftingTable.Forge, 4);
            mjolnirItem.MaximumRequiredStationLevel = 4;
            mjolnirItem.RequiredItems.Add("FineWood", 30);
            mjolnirItem.RequiredItems.Add("Stone", 30);
            mjolnirItem.RequiredItems.Add("SledgeIron", 1);
            mjolnirItem.RequiredItems.Add("DragonTear", 3);
            mjolnirItem.RequiredUpgradeItems.Add("FineWood", 10);
            mjolnirItem.RequiredUpgradeItems.Add("Stone", 10);
            mjolnirItem.RequiredUpgradeItems.Add("SledgeIron", 1);
            mjolnirItem.RequiredUpgradeItems.Add("DragonTear", 1);
            mjolnirItem.RecipeIsActive = NoCraft;
            mjolnirItem.GenerateWeaponConfigs = true;

            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            AnimationAwake();
        }

        private void Update()
        {
            if (!Player.m_localPlayer) return;
            if(!FlightHotKey.Value.IsDown()) return;
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
                if (NoFlight.Value == Toggle.On)
                {
                    if (Player.m_localPlayer.TakeInput())
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, NoFlightMessage.Value);
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

        private void OnDestroy()
        {
            Config.Save();
        }

        #region Configs

        public ConfigEntry<Toggle> ServerConfigLocked = null!;

        public ConfigEntry<Toggle> NoCraft = null!;
        public ConfigEntry<Toggle> NoFlight = null!;
        public ConfigEntry<string> NoFlightMessage = null!;
        internal ConfigEntry<KeyboardShortcut> FlightHotKey = null!;


        public bool UpdateRecipe;
        public static Recipe recipe;

        internal ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        internal ConfigEntry<T> config<T>(string group, string name, T value, string description,
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