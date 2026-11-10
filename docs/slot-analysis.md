# FINAL FANTASY XIII-2 – Slot Machine Analysis

Technical background for the FF13Casino777 patch.

Reference binary: `ffxiii2img.exe` (Steam) with 4 GB (Large Address Aware) patch, SHA256
`BEA6D43F8C3D4D8184077F89C6D3F6D4ED780F3B073C8F72C0257F10316196E6`. The original Steam executable
(SHA256 `575EA0325BE70108B77F84B6F6A96BB7A02AA3ABB151DEAEB324B84D6CB5AF07`) differs only in 3 header bytes
(`IMAGE_FILE_LARGE_ADDRESS_AWARE` flag and PE checksum); code and addresses are identical. Image base `0x00400000`, ASLR enabled (all addresses below are static VAs and must be rebased at runtime by
`module base - 0x400000`). No SteamStub, `.text` is not encrypted.

## Input path

The game imports only `DINPUT8.dll!DirectInput8Create` for input (plus `USER32!GetAsyncKeyState`).
There are no XInput, Raw Input or GameInput imports – the Xbox controller is read through DirectInput as well.
This is why a `dinput8.dll` proxy next to the executable is loaded early and reliably.

## Slot machine object

| What | Value |
|---|---|
| Global pointer | `[0x05133EBC]` (0 while no slot machine is active) |
| Created in | `0x006C9C80` (`new` 0x2ED8 bytes, constructor `0x006D34D0`) |
| Released in | `0x006C9F00` (pointer reset to 0) |
| vtable | `[obj+0x00] == 0x00D986F4` |

## Relevant fields (offsets into the slot machine object)

| Offset | Meaning | Initial value |
|---|---|---|
| `+0x0008` | Debug "強制入賞" (forced win pattern) enabled | 0 |
| `+0x0018` | Debug "強制入賞" value (flag enum) | 0 |
| `+0x1690` | Lottery sub-object (vtable `0x00D6EBB4`, ctor `0x006D22E0`) | |
| `+0x1694` | Current lottery result (flag) | 8 |
| `+0x1698` | Pending bonus flag (SEVEN/OCHU), 6 = none | 6 |
| `+0x169C` | Debug "強制ボーナス確定": games until bonus | 0 |
| `+0x16B0` | Set to 5 when a result is forced | 0 |
| `+0x2BF0` | Debug menu object (ctor `0x006DDD00`) | |
| `+0x2E10` | Main state machine state (`0x006D41D0`, 24 states) | 0 |

Flag enum (from `0x006D26B0`): 0 SEVEN, 1 BOMB, 2 CHOCOBO, 3 SABOTENDER, 4 MOOGLE, 5 OCHU, 6 BLANK,
7 SEVENDOUBLE, 8 = not drawn yet.

State machine (`+0x2E10`): 1 → 2 init, **7 = waiting for lever**, 8 = lottery, 10 = reels spinning,
11–15 = stop/payout, back to 7. Leaving the machine: 7 → 18 → 20 → 21 → 22, then the global pointer becomes 0.

## Developer debug menu

The retail executable still contains a Japanese pachislot-style debug menu (Shift-JIS strings, menu builder
around `0x006DDBE0`–`0x006DE050`, 17 items of 0x28 bytes). It is not reachable in the game, but its setters are:

| Setter | Menu entry | Effect |
|---|---|---|
| `0x006D2FF0` | 強制入賞 (force win pattern) | `+0x08 = 1, +0x18 = v, +0x1694 = v` (OFF → `+0x08 = 0, +0x1694 = 8`) |
| `0x006D3020` | 強制フラグ (force lottery flag, one-shot) | `+0x1694 = v, +0x16B0 = 5` |
| `0x006D3050` | 強制ボーナス確定 (guaranteed bonus) | `+0x169C = n` |

## Spin start (`0x006D3D00` → `0x006D2B50`)

```c
if (slot->fixPatEnabled /* +0x08 */) {
    slot->flag /* +0x1694 */ = slot->fixPat /* +0x18 */;
    if (flag == SEVEN || flag == OCHU) slot->bonusFlag /* +0x1698 */ = flag;
    return;                                   // random lottery is skipped
}
lottery(&slot->sub1690, ...);                 // 0x006D25C0 -> 0x006D2520
```

Lottery `0x006D2520`: only if `flag == 8` – if no bonus is pending (`bonusFlag == 6`) a random draw
(`0x006D2460`) happens, otherwise `flag = BLANK`. A SEVEN/OCHU result sets `bonusFlag`.

The reel stop logic reads the `+0x08` switch again: with the switch kept on, 777 is shown on the same spin;
if it is switched off right after the lottery, the pending SEVEN bonus is lined up on the following spin.

The in-game slot tutorial forces its results the same way (`flag = 6` or `flag = 2`, `+0x16B0 = 5`,
then `0x006D3D00`), which shows the mechanism is live in the retail build.

## Runtime verification

Verified by reading/writing the process memory from an external tool before the DLL was written:

| Lottery flag | Reels observed |
|---|---|
| CHOCOBO | 3× Chocobo |
| MOOGLE | 3× Moogle |
| BLANK | mixed, no win |
| SABOTENDER | no triple; sets `+0x169C = 3` (hidden bonus countdown) |
| SEVEN (forced via `+0x08/+0x18`) | 777 on every spin, payout animation, state machine returns to 7 cleanly |

## The patch

Constructor `0x006D34D0` zeroes `+0x08` twice (`movq [esi+8], xmm0` at `0x006D34E5`, and
`mov [esi+8], ebx` at `0x006D368B`). No sub-constructor in between receives `esi` itself, so the field is
0 at `0x006D368B`. The patch replaces

```
0x006D368B  89 5E 08    mov [esi+8], ebx
```

with

```
0x006D368B  FF 46 08    inc dword ptr [esi+8]     ; -> 1
```

`+0x18` stays 0 (= SEVEN). Every newly created slot machine object therefore starts with the forced
win pattern SEVEN enabled. The DLL only applies the patch after verifying 41 original bytes around the
patch site (`0x006D3677`–`0x006D369F`); otherwise it leaves the game untouched.
