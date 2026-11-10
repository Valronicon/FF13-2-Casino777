namespace FF13Casino777;

/// <summary>
/// In-memory 777 patch (the executable on disk is never modified).
///
/// The slot machine constructor (0x006D34D0) contains at 0x006D368B:
///     89 5E 08    mov [esi+8], ebx      ; ebx = 0 -> developer switch "強制入賞" (forced win pattern) off
/// which is replaced with
///     FF 46 08    inc dword ptr [esi+8] ; the field is provably 0 here -> switch = 1
/// The forced pattern at +0x18 is already set to 0 (= SEVEN) by the constructor.
/// Every slot machine the player sits down at therefore starts in "every spin is 777" mode.
/// Details: docs/slot-analysis.md
/// </summary>
internal static unsafe class Patcher
{
    private const uint StaticImageBase = 0x00400000;
    private const uint SignatureVa = 0x006D3677;
    private const int PatchOffset = 0x14; // 0x006D368B - 0x006D3677

    // Original bytes 0x006D3677..0x006D369F (end of the constructor), used as version/sanity check.
    private static readonly byte[] Original =
    [
        0xC7, 0x86, 0xD0, 0x2E, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, // mov dword ptr [esi+2ED0h],2
        0x89, 0x9E, 0xD4, 0x2E, 0x00, 0x00,                         // mov [esi+2ED4h],ebx
        0xF3, 0x0F, 0x7E, 0x07,                                     // movq xmm0,qword ptr [edi]
        0x89, 0x5E, 0x08,                                           // mov [esi+8],ebx      <- patch site
        0x89, 0x5E, 0x0C,                                           // mov [esi+0Ch],ebx
        0x89, 0x5E, 0x10,                                           // mov [esi+10h],ebx
        0x89, 0x5E, 0x14,                                           // mov [esi+14h],ebx
        0x66, 0x0F, 0xD6, 0x86, 0x20, 0x05, 0x00, 0x00,             // movq qword ptr [esi+520h],xmm0
    ];
    private static readonly byte[] PatchBytes = [0xFF, 0x46, 0x08];

    private static int s_done;

    public static void ApplyOnce()
    {
        if (Interlocked.Exchange(ref s_done, 1) != 0) return;
        try { Apply(); }
        catch (Exception ex) { Log.Write("ERROR: " + ex.Message); } // never let an exception escape into the game
    }

    private static void Apply()
    {
        string exe = Environment.ProcessPath ?? "";
        Log.Start(Path.GetDirectoryName(exe) ?? ".");
        if (!Path.GetFileName(exe).Equals("ffxiii2img.exe", StringComparison.OrdinalIgnoreCase))
        {
            Log.Write($"Not FINAL FANTASY XIII-2 ({Path.GetFileName(exe)}) - no patch, DirectInput is only forwarded.");
            return;
        }

        byte* module = (byte*)Win32.GetModuleHandleW(null);
        uint sizeOfImage = *(uint*)(module + *(int*)(module + 0x3C) + 0x50);
        uint rva = SignatureVa - StaticImageBase;
        if (rva + Original.Length > sizeOfImage) { Log.Write("Unknown game version (image too small) - no patch."); return; }

        var site = new Span<byte>(module + rva, Original.Length);
        var patched = (byte[])Original.Clone();
        PatchBytes.CopyTo(patched, PatchOffset);

        if (site.SequenceEqual(patched)) { Log.Write("777 patch was already active."); return; }
        if (!site.SequenceEqual(Original))
        {
            Log.Write("Unknown game version (bytes at the patch site differ) - no patch, the game runs unmodified.");
            return;
        }

        byte* target = module + rva + PatchOffset;
        if (!Win32.VirtualProtect((nint)target, PatchBytes.Length, Win32.PAGE_EXECUTE_READWRITE, out uint oldProtect))
        {
            Log.Write("ERROR: VirtualProtect failed - no patch.");
            return;
        }
        for (int i = 0; i < PatchBytes.Length; i++) target[i] = PatchBytes[i];
        Win32.VirtualProtect((nint)target, PatchBytes.Length, oldProtect, out _);
        Win32.FlushInstructionCache(Win32.GetCurrentProcess(), (nint)target, PatchBytes.Length);

        Log.Write($"777 patch active (module base 0x{(nint)module:X8}, patch site 0x{(nint)target:X8}). Every slot machine spin lands on 777.");
    }
}
