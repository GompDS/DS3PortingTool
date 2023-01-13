# DS3PortingTool

Tool for porting BNDs from other From Software games to Dark Souls 3.

If you use this tool for your mod I'd appreciate it if you would credit me for it.

### Features
- Automatically converts HKX files (although it's a very lengthy process)
- Converts ragdoll (not all Elden Ring Ragdoll)
- Converts cloth (not all cloth)
- Replaces materials and textures with DS3 materials and dummy textures.
- Excludes animations and events from the tae which are not applicable or an annoyance in DS3.
- Remaps animation ids to match their DS3 equivalents.
- Changes all sound events to use the enemy's new character id.
- **Configurability:** Which animations, events, jumpTables, and rumbleCams that are excluded
  or remapped are all configurable from the XML files in the Res folder.

### Supported BND Types
- anibnd
- chrbnd
- objbnd
- geombnd and geomhkxbnd

### Supported Games
- **Sekiro**
- **Elden Ring**

![Example of Divine Dragon ported.](/DS3PortingTool/Assets/Ported_Enemy_Sample.png)
<br /> Models ported from Sekiro and Elden Ring will have this look to them.

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
<br /> The HavokDowngrade folder should look something like this.

### Additional Requirements for porting assets from Sekiro and Elden Ring
- **oo2core_6_win64.dll**
  - Copy it from your Sekiro or Elden Ring game folder to the same directory as
  DS3PortingTool.exe.

------
## Porting Characters
If using from the command line, enter flags first and then the path(s) to the
source DCX file(s). Alternatively you can drag-and-drop DCX file(s) onto the exe.

### Sekiro
DCX file(s) must be anibnd, chrbnd or both.
```
DS3PortingTool.exe c5000.anibnd.dcx c5000.chrbnd.dcx
```
### Elden Ring
DCX file(s) must be anibnd, chrbnd or both. Make sure you select ALL anibnds including the divs.
```
DS3PortingTool.exe c5000.anibnd.dcx c5000_div00.anibnd.dcx c5000_div01.anibnd.dcx c5000.chrbnd.dcx
```

------
## Porting Objects/Assets
If using from the command line, enter flags first and then the path(s) to the
source DCX file(s). Alternatively you can drag-and-drop DCX file(s) onto the exe.

### Sekiro
DCX file must be an objbnd.
```
DS3PortingTool.exe o120100.objbnd.dcx
```
### Elden Ring
DCX File(s) must be geombnd and geomhkxbnd. Make sure you select both the geombnd and geomhkxbnd then drag them
onto the exe at the same time.
```
DS3PortingTool.exe aeg027_025.geombnd.dcx aeg027_025.geomhkxbnd.dcx
```
## Flags
Flags are settings for the conversion process which will alter how the program behaves.

-----
`-i [id]` Specify an id that will override the id of the
original DCX. If this flag is not used and no id can be discerned from the source file names, then c1000 will be used for characters and o100000 for objects.

Examples:
- `DS3PortingTool.exe -i 1050 c1000.anibnd.dcx`
- `DS3PortingTool.exe -i 500500 o200100.objbnd.dcx`
-----
`-o [offsets]` Specify animation offset(s) to exclude.

Examples: `DS3PortingTool.exe -o 0` `DS3PortingTool.exe -o 1,2,4`

-----
`-s` The character id in sound events will not be changed.

Example: `DS3PortingTool.exe -s c7110.anibnd.dcx`

`-s [id]` Replace the character id in sound events with this id instead.

Example: `DS3PortingTool.exe -s 7100 c7110.anibnd.dcx`

-----
`-t` When porting a character or object, only the tae will be ported if there is one.

Example:`DS3PortingTool.exe -t c1000.anibnd.dcx`

-----
`-f` When porting a character or object, only the flver(s) will be ported if there are any.

Example:`DS3PortingTool.exe -f c1000.chrbnd.dcx`

-----
`-x` Runs the program without asking the user to enter flags.

Example:`DS3PortingTool.exe -x c6200.anibnd.dcx`

-----
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

## Configuring XML
Inside the Res folder there are several XML files which determine what is excluded or replaced during the porting process. The CharacterXML folder contains settings for porting characters and the ObjectXML folder contains settings  for porting objects.
### XML File Explanations
#### AllowedSpEffects.xml
> If a TAE event which adds a SpEffect is going to be excluded, but its SpEffectID is  in this list, then it won't be excluded.
#### AnimationRemapping.xml
> If an animation's ID is a key in this dictionary then its ID will be replaced with the corresponding value in the TAE.
#### ExcludedAnimations.xml
> If an animation's ID is in this list, then it will be excluded from the TAE.
#### ExcludedEvents.xml
> If a TAE event's type is in this list, then it will be removed.
#### ExcludedJumpTables.xml
> If a JumpTable's jumpTableID is in this list, then it will be removed.
#### ExcludedRumbleCams.xml
> If a rumbleCam TAE event's rumbleCamID is in this list, then it will be removed.
#### SpEffectRemapping.xml
> If a TAE event which adds a SpEffect's SpEffectID is a key in this dictionary, then its SpEffectID will be replaced with the corresponding value.
### How to Edit XML Files
Find the itemList or itemDictionary of the game you are porting from. 

-----
#### Adding to an itemList
Create a new item in the itemList. Put a numerical id inside the quotation marks.
```
<item id=""/>
```
Create an itemRange which is used to add many ids to an itemList at once. It repeats a certain amount of times and each time it repeats, every item within the itemRange has its id incremented by a specified amount.
```
<itemRange increment="" repeat="">
  <item id=""/>
</itemRange>
```

-----
#### Adding to an itemDictionary
```
<item key="" value=""/>
```
Create an itemRange which is used to add many key-value pairs to an itemDictionary at once. It repeats a certain amount of times and each time it repeats, every item within the itemRange has its key and value incremented by different amounts.
```
<itemRange keyIncrement="" valueIncrement="" repeat="">
  <item key="" value=""/>
</itemRange>
```

## Known Issues
- Object collision HKX will not downgrade successfully.

------
## Special Thanks
- **TKGP/JKAnderson:** SoulsFormats
- **Meowmaritus:** SoulsAssetPipeline, Material Info Bank and TAE Templates
- **Katalash:** Hkxpack-souls
- **The12thAvenger:** DS3HavokConverter, some FbxImporter methods and for helping me a lot in the making of this tool
- **Nordgaren:** Bounding Box Patch Calculator



