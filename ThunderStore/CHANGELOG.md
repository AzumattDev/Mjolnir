### v1.5.0

- Update for Valheim 0.216.5
### v1.4.4

- Update things
- Add Tool Tier
- Add localization support for Chinese, French, German, Japanese, Korean, Russian, Portuguese, and Spanish

### v1.4.1/1.4.2/v1.4.3

- Update Item Manager and ServerSync internally. Warning, this will mess up your configuration file for this mod. Please reconfigure it.

### v1.4.0

- `Note:` You will need to regenerate your configuration file. The file name has changed to fall in line with my other
  mods. It is now `Azumatt.Mjolnir.cfg`
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