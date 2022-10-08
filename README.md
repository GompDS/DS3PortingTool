# DS3PortingTool

Tool for porting enemies from other From Software games to Dark Souls 3.
DS3PortingTool does the heavy lifting for you so you can get on with modding.

If you use this tool for your mod I'd appreciate it if you would credit me for it.

### Features
- Automatically converts HKX files (although it's a very lengthy process)
- Converts ragdoll and some cloth.
- Replaces materials and textures with DS3 materials and dummy textures.
- Excludes animations and events from the tae which are not applicable or an annoyance in DS3.
- Remaps animation ids to match their DS3 equivalents.
- Changes all sound events to use the enemy's new character id.
- **Configurability:** Which animations, events, jumpTables, and rumbleCams that are excluded
  or remapped are all configurable from the XML files in the Res folder.

### Games Supported
- **Sekiro**

![Example of Divine Dragon ported.](/DS3PortingTool/Assets/Ported_Enemy_Sample.png)
Enemies ported from Sekiro and Elden Ring will have this look to them.

------
## Requirements
In order for the tool to function as intended, you will need to place the
following tools unzipped in the 'HavokDowngrade' folder.
- **DS3HavokConverter**
  - Github releases: https://github.com/The12thAvenger/DS3HavokConverter/releases/
- **Hkxpack-souls**
  - Download from ?ServerName? Discord: https://discord.com/channels/529802828278005773/529900741998149643/699509305929629849
- **FileConvert.exe**
  - Obtainable from Havok Content Tools 2018.

![This is what your HavokDowngrade folder should look like.](/DS3PortingTool/Assets/Readme_HavokDowngrade_Model.png)
 The HavokDowngrade folder should look something like this.

### Additional Requirements for porting assets from Sekiro and Elden Ring
- **oo2core_6_win64.dll**
  - Copy it from your Sekiro or Elden Ring game folder to the same directory as
  DS3PortingTool.exe.

------
## Usage
If using from the command line, enter flags first and then the path to the
source DCX file. Alternatively you can drag-and-drop a DCX file onto the exe.

DCX file must be either an anibnd or chrbnd.

In this example, no flags were entered initially so the user was prompted to enter flags. The user
did not want to use any flags and so continued with the program execution.
```
DS3PortingTool.exe c1000.anibnd.dcx
Enter Flags:

<program executes>
```

In this example, at least one flag was entered initially so the user was not prompted to enter flags.
```
DS3PortingTool.exe -t c1000.anibnd.dcx

<program executes>
```

### Flags
`-c [id]` Specify a character id that will override the character id of the
original DCX.

Example:
`DS3PortingTool.exe -c 1050 c1000.anibnd.dcx`

`-o [offsets]` Specify animation offset(s) to exclude.

Examples: `DS3PortingTool.exe -o 0` `DS3PortingTool.exe -o 1,2,4`

`-s` The character id in sound events will not be changed.

Example: `DS3PortingTool.exe -s c7110.anibnd.dcx`

`-s [id]` Replace the character id in sound events with this id instead.

Example: `DS3PortingTool.exe -s 7100 c7110.anibnd.dcx`

`-t` When porting an anibnd, only the tae will be ported.

Example:`DS3PortingTool.exe -t c1000.anibnd.dcx`

`-x` Runs the program without asking the user to enter flags.

Example:`DS3PortingTool.exe -x c6200.anibnd.dcx`

------
## Special Thanks
- **TKGP/JKAnderson:** SoulsFormats
- **Meowmaritus:** SoulsAssetPipeline, Material Info Bank and TAE Templates
- **Katalash:** Hkxpack-souls
- **The12thAvenger:** DS3HavokConverter, some FbxImporter methods and for helping me a lot in the making of this tool



