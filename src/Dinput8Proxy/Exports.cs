using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FF13Casino777;

/// <summary>
/// dinput8.dll proxy: every export is forwarded to the real system DLL.
/// On the first call to DirectInput8Create (early during game startup) the 777 patch is applied once.
/// </summary>
public static unsafe class Exports
{
    private const int E_FAIL = unchecked((int)0x80004005);
    private const int S_FALSE = 1;
    private static nint s_real;

    private static nint Real(string name)
    {
        if (s_real == 0)
        {
            // In a 32-bit process Windows transparently redirects "system32" to SysWOW64.
            s_real = Win32.LoadLibraryW(Path.Combine(Environment.SystemDirectory, "dinput8.dll"));
            if (s_real == 0) { Log.Write("ERROR: could not load the system dinput8.dll."); return 0; }
        }
        return Win32.GetProcAddress(s_real, name);
    }

    [UnmanagedCallersOnly(EntryPoint = "DirectInput8Create", CallConvs = [typeof(CallConvStdcall)])]
    public static int DirectInput8Create(nint hinst, uint version, nint riid, nint ppvOut, nint punkOuter)
    {
        Patcher.ApplyOnce();
        var fn = (delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nint, int>)Real("DirectInput8Create");
        return fn == null ? E_FAIL : fn(hinst, version, riid, ppvOut, punkOuter);
    }

    [UnmanagedCallersOnly(EntryPoint = "DllCanUnloadNow", CallConvs = [typeof(CallConvStdcall)])]
    public static int DllCanUnloadNow() => S_FALSE; // the proxy stays loaded

    [UnmanagedCallersOnly(EntryPoint = "DllGetClassObject", CallConvs = [typeof(CallConvStdcall)])]
    public static int DllGetClassObject(nint rclsid, nint riid, nint ppv)
    {
        var fn = (delegate* unmanaged[Stdcall]<nint, nint, nint, int>)Real("DllGetClassObject");
        return fn == null ? E_FAIL : fn(rclsid, riid, ppv);
    }

    [UnmanagedCallersOnly(EntryPoint = "DllRegisterServer", CallConvs = [typeof(CallConvStdcall)])]
    public static int DllRegisterServer()
    {
        var fn = (delegate* unmanaged[Stdcall]<int>)Real("DllRegisterServer");
        return fn == null ? E_FAIL : fn();
    }

    [UnmanagedCallersOnly(EntryPoint = "DllUnregisterServer", CallConvs = [typeof(CallConvStdcall)])]
    public static int DllUnregisterServer()
    {
        var fn = (delegate* unmanaged[Stdcall]<int>)Real("DllUnregisterServer");
        return fn == null ? E_FAIL : fn();
    }

    [UnmanagedCallersOnly(EntryPoint = "GetdfDIJoystick", CallConvs = [typeof(CallConvStdcall)])]
    public static nint GetdfDIJoystick()
    {
        var fn = (delegate* unmanaged[Stdcall]<nint>)Real("GetdfDIJoystick");
        return fn == null ? 0 : fn();
    }
}
