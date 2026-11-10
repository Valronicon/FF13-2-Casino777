# FF13Casino777

**Every slot machine spin in FINAL FANTASY XIII-2 lands on 777.**

A small, self-contained mod for the Steam version of FINAL FANTASY XIII-2. Drop one file next to the game
executable and every spin at the Serendipity slot machines pays out the 777 jackpot – no trainer, no
background tool, no modified game files.

## Features

- Every spin at every slot machine lands on 777, starting with the first spin.
- Works automatically from game start – nothing to run, no hotkeys.
- Does not modify any game file on disk. The patch exists only in memory while the game is running.
- Safe by design: the mod checks the exact game code before patching. If the game was updated and the code
  does not match, it does nothing and the game runs unmodified.
- Compatible with [FF13Fix](https://github.com/rebtd7/FF13Fix) (`d3d9.dll`).

## Requirements

- FINAL FANTASY XIII-2, **Steam version** (Windows)
- Nothing else – no .NET runtime or other installation is needed.

## Installation

1. Download `FF13Casino777-<version>.zip` from the [Releases](../../releases) page.
2. Close the game.
3. Open the game folder: in Steam, right-click **FINAL FANTASY XIII-2** → **Manage** → **Browse local files**.
4. Extract the zip **into that folder**. The DLL ends up next to the game executable:

   ```
   FINAL FANTASY XIII-2\alba_data\prog\win\bin\dinput8.dll
   FINAL FANTASY XIII-2\alba_data\prog\win\bin\ffxiii2img.exe
   ```

5. Start the game as usual through Steam and go to a slot machine in Serendipity.

### Checking that it works

After the game has started, `FF13Casino777.log` appears in the same `bin` folder. It should contain:

```
777 patch active (...). Every slot machine spin lands on 777.
```

If it says *Unknown game version*, your game executable differs from the supported one; the mod then stays
inactive and the game runs normally.

## Uninstallation

Close the game and delete `dinput8.dll` (and `FF13Casino777.log`) from
`FINAL FANTASY XIII-2\alba_data\prog\win\bin\`.

## Notes

- **Save games:** coins you win are stored in your save game like any other coins. Consider backing up your
  saves before using the mod. They are located in `Steam\userdata\<SteamID>\292140\remote\` and synced via
  Steam Cloud.
- **Other `dinput8.dll` mods:** only one `dinput8.dll` can exist in the folder. If another mod (e.g. an ASI
  loader) already uses that file name, the two cannot be installed side by side.
- **Antivirus:** proxy DLLs are a common modding technique but are occasionally flagged by heuristics. The
  full source code is in this repository and every release is built by GitHub Actions from that source.

## How it works

The game reads all input through DirectInput, so Windows loads `dinput8.dll` from the game folder before the
system copy. The mod forwards every DirectInput call to the real `dinput8.dll` and, once at startup, changes
3 bytes in memory:

The retail game still contains an unused developer debug switch for the slot machine ("force win pattern").
When it is enabled, the machine skips its random lottery and shows the configured pattern. The mod enables
this switch with the pattern SEVEN in the slot machine constructor, so every machine you sit down at is in
"always 777" mode.

Full reverse-engineering notes: [docs/slot-analysis.md](docs/slot-analysis.md).

### Supported game version

The mod works with the Steam executable **with or without the 4 GB (Large Address Aware) patch** that
some FF XIII-2 fixes apply. The 4 GB patch only changes 3 bytes in the file header; the game code is
identical, and the mod only checks the game code around its patch site.

| `ffxiii2img.exe` (Steam) | SHA256 |
|---|---|
| original | `575EA0325BE70108B77F84B6F6A96BB7A02AA3ABB151DEAEB324B84D6CB5AF07` |
| with 4 GB patch | `BEA6D43F8C3D4D8184077F89C6D3F6D4ED780F3B073C8F72C0257F10316196E6` |

Other executables (e.g. a future game update) are detected and left untouched.

## Building from source

Requirements: Windows, [.NET 10 SDK](https://dotnet.microsoft.com/download), Visual Studio 2022 or later
with the **Desktop development with C++** workload (needed by NativeAOT for linking).

```powershell
$env:PATH += ";${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"   # vswhere.exe
dotnet publish src\Dinput8Proxy -c Release
```

The DLL is written to `src\Dinput8Proxy\bin\Release\net10.0\win-x86\publish\dinput8.dll` (about 900 KB).

> `src\Dinput8Proxy\bin\Release\net10.0\win-x86\dinput8.dll` (about 13 KB) is an intermediate .NET assembly –
> the game will not start with it.

Releases are created automatically by the [release workflow](.github/workflows/release.yml) when a tag
`v*` is pushed.

## License

[MIT](LICENSE)

FINAL FANTASY is a registered trademark of Square Enix Holdings Co., Ltd. This project is not affiliated with
or endorsed by Square Enix. Use at your own risk.
