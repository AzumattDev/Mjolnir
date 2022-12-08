using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;
using LocalizationManager;
using ItemManager;

namespace Mjolnir;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class MjolnirPlugin : BaseUnityPlugin
{
    public const string ModVersion = "1.4.3";
    public const string ModGUID = "Azumatt.Mjolnir";
    public const string ModAuthor = "Azumatt";
    public const string ModName = "Mjolnir";
    public static readonly ManualLogSource MJOLLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
    private static string ConfigFileName = ModGUID + ".cfg";
    private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    internal static string ConnectionError = "";
    public static MjolnirPlugin context;

    internal static readonly ConfigSync? configSync = new(ModName)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    private bool _flight;

    private readonly Harmony _harmony = new(ModGUID);

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    public void Awake()
    {
        context = this;
        Localizer.Load();
        Util.Functions.ConfigGen();

        Item mjolnirItem = new("mjolnir", "Mjolnir", "EmbeddedAsset");
        mjolnirItem.Name.English("Mjölnir");
        mjolnirItem.Description.English(
            "Whosoever holds this hammer, if he be worthy, shall possess the power of Thor.");
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
        //mjolnirItem.GenerateWeaponConfigs = true;
        FlightAnimations.AnimationAwake();
        _harmony.PatchAll();
        SetupWatchers();
    }

    public void Update()
    {
        if (!Player.m_localPlayer) return;
        if (!FlightHotKey.Value.IsDown()) return;
        if (Player.m_localPlayer.GetCurrentWeapon()?.m_dropPrefab?.name != "Mjolnir") return;
        //Quaternion? rotation = Player.m_localPlayer.GetCurrentWeapon()?.m_dropPrefab.transform.rotation;
        if (_flight)
        {
            /* Disable flight */
            _flight = !_flight;
            Player.m_localPlayer.m_body.useGravity = _flight;
            if (!Player.m_localPlayer.IsDebugFlying())
                Player.m_localPlayer.m_animator.runtimeAnimatorController = FlightAnimations.OrigDebugFly;
            Player.m_localPlayer.m_zanim.SetTrigger("emote_stop");
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Mjolnir fly:" + _flight);
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

            _flight = !_flight;
            Player.m_localPlayer.m_body.useGravity = _flight;
            Player.m_localPlayer.m_animator.runtimeAnimatorController = FlightAnimations.CustomDebugFly;
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Mjolnir fly:" + _flight);
        }
    }

    public void FixedUpdate()
    {
        try
        {
            if (!Player.m_localPlayer)
                return;
            float fixedDeltaTime = Time.fixedDeltaTime;
            if (Player.m_localPlayer.GetCurrentWeapon()?.m_dropPrefab?.name != "Mjolnir")
                /* Disable flight if the player isn't holding Mjolnir */
                _flight = false;


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
        if (_flight)
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
            _flight = !_flight;
            Player.m_localPlayer.m_body.useGravity = _flight;
            Player.m_localPlayer.m_animator.runtimeAnimatorController = FlightAnimations.OrigDebugFly;
            // Player.m_localPlayer.m_nview.GetZDO().Set("DebugFly", this.flight);
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Mjolnir fly:" + _flight);
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

    public void OnDestroy()
    {
        Config.Save();
    }

    private void SetupWatchers()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath)) return;
        try
        {
            MJOLLogger.LogDebug("ReadConfigValues called");
            Config.Reload();
        }
        catch
        {
            MJOLLogger.LogError($"There was an issue loading your {ConfigFileName}");
            MJOLLogger.LogError("Please check your config entries for spelling and format!");
        }
    }

    #region Configs

    public static ConfigEntry<Toggle> ServerConfigLocked = null!;

    public static ConfigEntry<Toggle> NoCraft = null!;
    public static ConfigEntry<Toggle> NoFlight = null!;
    public static ConfigEntry<string> NoFlightMessage = null!;
    internal static ConfigEntry<KeyboardShortcut> FlightHotKey;

    internal ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription =
            new(
                description.Description +
                (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

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

    internal class AcceptableShortcuts : AcceptableValueBase
    {
        public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
        {
        }

        public override object Clamp(object value) => value;
        public override bool IsValid(object value) => true;

        public override string ToDescriptionString() =>
            "# Acceptable values: " + string.Join(", ", KeyboardShortcut.AllKeyCodes);
    }

    #endregion
}