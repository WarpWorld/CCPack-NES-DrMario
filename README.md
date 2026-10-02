# Dr. Mario

## Pack metadata

- Game identifier: `DrMario`
- Platform: `NES`
- Connector type: `NESConnector`

## What this pack provides
This Crowd Control pack integrates **Dr. Mario** with Crowd Control through its NES pack implementation. Its source defines the game-state checks and effect handling.

## Requirements
- A compatible game ROM. This pack does not provide a ROM.

## Connection context
The pack source contains the platform-specific connection and game-state logic used by Crowd Control; this is explanatory context, not an additional requirement.

## Supported ROMs
Entries are derived from the primary definition. Status is shown only when declared by source metadata.

| ROM name | Checksum | Notes |
| --- | --- | --- |
| Dr. Mario (Japan, USA) (Rev A) | MD5: 8181d696756578fc92e6c4c86da01904 | <span style="color: green">Supported</span> |
| Dr. Mario (Japan, USA) | MD5: d3ec44424b5ac1a4dc77709829f721c9 | <span style="color: green">Supported</span> |
| Dr. Mario (Europe) | MD5: 3f27eda62c6692f96790af9a1d917ef6 | <span style="color: green">Supported</span> |

## Contents
- `DrMario.cs` - primary pack definition and ROM metadata.
