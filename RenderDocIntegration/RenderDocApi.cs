using System;
using System.Runtime.InteropServices;

namespace RenderDocIntegration;

public static unsafe partial class RenderDocApi
{
    // ---- Platform lookups: find the module RenderDoc already injected ----

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW",
        StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandleW(string lpModuleName);

    // glibc: libdl.so.2 exists on all glibc versions (a stub on 2.34+)
    [LibraryImport("libdl.so.2", EntryPoint = "dlopen",
        StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr dlopen(string fileName, int flags);

    private const int RTLD_NOW = 0x2;
    private const int RTLD_NOLOAD = 0x4; // only return handle if already loaded

    private static IntPtr GetRenderDocModule()
    {
        if (OperatingSystem.IsWindows())
            return GetModuleHandleW("renderdoc.dll");

        if (OperatingSystem.IsLinux())
            return dlopen("librenderdoc.so", RTLD_NOW | RTLD_NOLOAD);

        return IntPtr.Zero; // macOS: RenderDoc not supported
    }

    // ---- RenderDoc API function pointers ----

    private static delegate* unmanaged[Cdecl]<void> _triggerCapture;
    private static delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> _startFrameCapture;
    private static delegate* unmanaged[Cdecl]<IntPtr, IntPtr, uint> _endFrameCapture;
    private static delegate* unmanaged[Cdecl]<uint> _isFrameCapturing;

    public static bool IsAvailable { get; private set; }

    public static bool Init()
    {
        IntPtr module = GetRenderDocModule();
        if (module == IntPtr.Zero)
            return false; // not running under RenderDoc

        if (!NativeLibrary.TryGetExport(module, "RENDERDOC_GetAPI", out IntPtr getApiPtr))
            return false;

        const int eRENDERDOC_API_Version_1_6_0 = 10600;
        IntPtr api;
        var getApi = (delegate* unmanaged[Cdecl]<int, IntPtr*, int>)getApiPtr;
        if (getApi(eRENDERDOC_API_Version_1_6_0, &api) != 1 || api == IntPtr.Zero)
            return false;

        // Function table layout from renderdoc_app.h (RENDERDOC_API_1_6_0)
        _triggerCapture    = (delegate* unmanaged[Cdecl]<void>)Slot(api, 15);
        _startFrameCapture = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)Slot(api, 19);
        _isFrameCapturing  = (delegate* unmanaged[Cdecl]<uint>)Slot(api, 20);
        _endFrameCapture   = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, uint>)Slot(api, 21);

        return IsAvailable = true;
    }

    private static IntPtr Slot(IntPtr api, int index)
        => Marshal.ReadIntPtr(api, index * IntPtr.Size);

    // Capture the next presented frame
    public static void TriggerCapture()
    {
        if (IsAvailable) _triggerCapture();
    }

    // Manually bracket work (IntPtr.Zero = any device/window)
    public static void StartCapture(IntPtr device = default, IntPtr window = default)
    {
        if (IsAvailable) _startFrameCapture(device, window);
    }

    public static bool EndCapture(IntPtr device = default, IntPtr window = default)
        => IsAvailable && _endFrameCapture(device, window) == 1;

    public static bool IsFrameCapturing()
        => IsAvailable && _isFrameCapturing() == 1;
}