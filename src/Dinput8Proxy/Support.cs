using System.Runtime.InteropServices;

namespace FF13Casino777;

internal static partial class Win32
{
    public const uint PAGE_EXECUTE_READWRITE = 0x40;

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint LoadLibraryW(string path);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint GetProcAddress(nint module, string name);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandleW(string? name);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualProtect(nint addr, nint size, uint newProtect, out uint oldProtect);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FlushInstructionCache(nint process, nint addr, nint size);

    [LibraryImport("kernel32.dll")]
    public static partial nint GetCurrentProcess();
}

/// <summary>Small log file "FF13Casino777.log" next to the game executable (recreated on every game start).</summary>
internal static class Log
{
    private static string? s_path;

    public static void Start(string directory)
    {
        s_path = Path.Combine(directory, "FF13Casino777.log");
        try { File.WriteAllText(s_path, ""); } catch { s_path = null; }
        Write($"FF13Casino777 {BuildInfo.Version} (dinput8 proxy) loaded.");
    }

    public static void Write(string message)
    {
        if (s_path == null) return;
        try { File.AppendAllText(s_path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n"); } catch { }
    }
}
