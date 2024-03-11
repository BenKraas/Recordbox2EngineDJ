# Recordbox2EngineDJ
This program serves as a personal starting point of fixing the Beatshifting issue with recorbox imports to EngineDJ.

Note that there are other, likely much more successful approaches found elsewhere.

This tool does silence analysis on .mp3 files specified by the .xml to determine a potential initial beatgrid point. 

Your .mp3s and original .xml remain completely unchanged! They are only read for analysis

## Implemented features:
Accepts the Recordbox .xml export file and
- reads through the analyzed tracks
- cross-references them with a UserOverride dictionary json file with TrackUUID as keys
- skips acapellas and any tracks where Inizio is greater than a certain threshold (currently hard coded, this will be moved to a config.json)
- EITHER detects the first Inizio value from the .mp3 files
- OR if an Override value is given in the UserOverride dictionary, apply this instead
- Give visual feedback via colored CLI

## Envisioned features:
- bool variables to determine if an offset value set in the OverrideDataset it is applied to
  - the Beatgrid (applyToBeatgrid = true;) and/or
  - the Hotcues (applyToHotQues) and/or
  - the Memory markers (PositionMarkers with "Num=-1" and without "End" - applyToMemoryMarkers) and/or
  - the MemoryLoops (PositionMarkers with "Num=-1" and "End" - applyToMemoryLoops)
