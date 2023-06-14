using BepInEx.Configuration;
using UnityEngine;

namespace Mjolnir.Util;

public class Functions
{
    internal static void ConfigGen()
    {
        MjolnirPlugin.ServerConfigLocked = MjolnirPlugin.context.config("1 - General", "Lock Configuration", MjolnirPlugin.Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        MjolnirPlugin.configSync?.AddLockingConfigEntry(MjolnirPlugin.ServerConfigLocked);

        /* No-Craft */
        MjolnirPlugin.NoCraft = MjolnirPlugin.context.config("1 - General", "Craftable", MjolnirPlugin.Toggle.Off, "If on, Mjolnir is craftable.");

        /* No-Flight */
        MjolnirPlugin.NoFlight = MjolnirPlugin.context.config("1 - General", "No Flight", MjolnirPlugin.Toggle.Off, "Makes the Mjolnir less...Mjolnir. Disable the flight (but why though? It's Mjolnir!)");
        MjolnirPlugin.ShouldUseStamina = MjolnirPlugin.context.config("1 - General", "Flight Use Stamina", MjolnirPlugin.Toggle.On, "If on, flight will use stamina.");
        MjolnirPlugin.NoFlightMessage = MjolnirPlugin.context.config("1 - General", "No Flight Message", "Your God-Like ability to fly is suppressed by Odin himself", "Message to show when flight is denied to the player. Can make blank to hide message.", false);
        MjolnirPlugin.FlightHotKey = MjolnirPlugin.context.config("1 - General", "FlightHotKey", new KeyboardShortcut(KeyCode.Z), new ConfigDescription("Personal hotkey to toggle a flight", new MjolnirPlugin.AcceptableShortcuts()), false);
    }
}