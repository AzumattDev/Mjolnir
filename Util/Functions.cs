using BepInEx.Configuration;
using UnityEngine;

namespace Mjolnir.Util;

public class Functions
{
    internal static void ConfigGen()
    {
        Mjolnir.Instance.ServerConfigLocked = Mjolnir.Instance.config("1 - General", "Lock Configuration",
            Mjolnir.Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        Mjolnir.Instance.configSync.AddLockingConfigEntry(Mjolnir.Instance.ServerConfigLocked);

        /* No-Craft */
        Mjolnir.Instance.NoCraft = Mjolnir.Instance.config("1 - General", "Not Craftable", Mjolnir.Toggle.On,
            "Makes the Mjolnir non-craftable");

        /* No-Flight */
        Mjolnir.Instance.NoFlight = Mjolnir.Instance.config("1 - General", "No Flight", Mjolnir.Toggle.Off,
            "Makes the Mjolnir less...Mjolnir. Disable the flight (but why though? It's Mjolnir!)");
        Mjolnir.Instance.NoFlightMessage = Mjolnir.Instance.config("1 - General", "No Flight Message",
            "Your God-Like ability to fly is suppressed by Odin himself",
            "Message to show when flight is denied to the player. Can make blank to hide message.", false);
        Mjolnir.Instance.FlightHotKey = Mjolnir.Instance.config("1 - General", "FlightHotKey",
            new KeyboardShortcut(KeyCode.Z),
            "Personal hotkey to toggle a flight", false);
    }
}