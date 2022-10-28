#### A hammer granting you the power of Thor. Fly, Move faster, attack harder, & call upon lightning

<details><summary><b>Update Information</b></summary>

### v1.4.1

- Update Item Manager and ServerSync internally. Warning, this will mess up your configuration file for this mod. Please reconfigure it.
- 
### v1.4.0

- `Note:` You will need to regenerate your configuration file. The file name has changed to fall in line with my other
  mods. It is now `Azumatt.PvPAlways.cfg`
- The internal GUID has changed. This might mean if you're already using the mod, you would need to delete the potential
  duplicate dll that will be created.
- Update ServerSync
- Add FileWatcher. Can now also live update when the configuration file is changed directly and not through the
  Configuration Manager.
- Fix some of the animations not playing correctly.
- Move to my version of ItemManager.
- Add Localization support.
- Add VersionHandshake. This will allow the mod to kick players that are not using the same version of the mod as the
  server.
- I plan on adding more things to this mod again soon, keep an eye out!

### v1.3.0

- Fix animation bug if using DebugFly and Mjolnir Flight at the same time and it gets toggled off causing you to "fly"
  while walking.
- Add ability to deny flight and give clients ability to change the hotkey that activates it.
- Configurable message when flight is denied. Leave blank to display nothing.
- Add to the config description for each option what is synced with server and what is not.

### v1.2.0 update information

- Fix flight animations after Hearth & Home update

### v1.1.1 update information

- Allow flight even with anticheat present.
- Flying now spends stamina. Flying faster spends more. Running out of stamina or unequipping Mjolnir will disable
  flight.

### v1.1.0 update information

- Add flying animations and ability to enter debug fly mode when holding Mjolnir (Press Z to enter debugfly)

### v1.0.2 update information

- Added more configuration options. These relate to damage and crafting. The hammer defaults to not be craftable. (
  Reminder: You must install client and server side for configuration syncing)

</details>

<details><summary><b>Installation Instructions</b></summary>

### Windows (Steam)

1. Locate your game folder manually or start Steam client and :
   a. Right click the Valheim game in your steam library
   b. "Go to Manage" -> "Browse local files"
   c. Steam should open your game folder
2. Extract the contents of the archive into the BepInEx\plugins folder
3. Locate azumatt.Mjolnir.cfg and azumatt.Mjolnir.Localization.cfg under BepInEx\config and configure the mod to your
   needs

### Server

Must be installed on both the client and the server for syncing to work properly.

1. Locate your BepInEx folder manually and :
   a. Extract the contents of the archive into the BepInEx folder.
   b. Launch your game at least once to generate the config file needed if you haven't already done so.
   c. Locate Azumatt.Mjolnir.cfg under BepInEx\config on your machine and configure the mod to your needs. If you are
   forcing configurations, these settings will be what everyone synchronizes to.
2. Reboot your server. All clients will now sync to the server's config file even if theirs differs. Config Manager mod
   changes will only apply if the person changing the configuration is an Admin on the server. The changes will be
   reflected live if the admin changes them while in game.

</details>

<details><summary><b>Hammer Information</b></summary>

The hammer is NOT craftable by default!!! It's meant to be a god weapon, but you can make it craftable.

`Prefab Name` = Mjolnir

### Default Cost:

- FineWood `(30)`
- Stone `(30)`
- SledgeIron `(1)`
- DragonTear `(3)`

</details>




Feel free to reach out to me on discord if you need manual download assistance.

#

Feel free to add to the repo.
https://github.com/AzumattDev/Mjolnir

### Demonstration Video

[![Mjolnir Demonstration Video](https://img.youtube.com/vi/qp9Vn9hVX5w/0.jpg)](https://youtu.be/qp9Vn9hVX5w)

# Author Information

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/

For Questions or Comments, find me in the Odin Plus Team Discord or in mine:

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/Pb6bVMnFb2)
<a href="https://discord.gg/pdHgy6Bsng"><img src="https://i.imgur.com/Xlcbmm9.png" href="https://discord.gg/pdHgy6Bsng" width="175" height="175"></a>
