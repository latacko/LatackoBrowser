using Silk.NET.SDL;

namespace Renderer;

public static class CursorManager
{
    private static Sdl _sdl;
    private static unsafe Cursor* defaultCursor;
    private static unsafe Cursor* handCursor;
    private static unsafe Cursor* textCursor;
    private static unsafe Cursor* moveCursor;
    private static unsafe Cursor* waitCursor;
    private static unsafe Cursor* notAllowedCursor;
    private static unsafe Cursor* crosshairCursor;
    private static unsafe Cursor* grabCursor;
    private static unsafe Cursor* currentCursor;
    private static bool CursorVisible = false;

    public static unsafe void Init(Sdl sdl)
    {
        _sdl = sdl;
        defaultCursor = _sdl.CreateSystemCursor(SystemCursor.SystemCursorArrow);
        handCursor = _sdl.CreateSystemCursor(SystemCursor.SystemCursorHand);
        textCursor = _sdl.CreateSystemCursor(SystemCursor.SystemCursorIbeam);
        moveCursor = _sdl.CreateSystemCursor(SystemCursor.SystemCursorSizeall);
        waitCursor = _sdl.CreateSystemCursor(SystemCursor.SystemCursorWait);
        // helpCursor = _sdl.CreateSystemCursor(SystemCursor.);
        notAllowedCursor = _sdl.CreateSystemCursor(SystemCursor.SystemCursorNo);
        // progressCursor = _sdl.CreateSystemCursor(SystemCursor.);
        crosshairCursor = _sdl.CreateSystemCursor(SystemCursor.SystemCursorCrosshair);
        grabCursor = _sdl.CreateSystemCursor(SystemCursor.SystemCursorSizeall);
        currentCursor = defaultCursor;
    }

    public static void SetVisible(bool visible)
    {
        if (CursorVisible == visible) return;
        CursorVisible = visible;
        _sdl.ShowCursor(visible ? 1 : 0);
    }

    public static unsafe void SetHand()
    {
        if (currentCursor == handCursor) return;
        currentCursor = handCursor;
        _sdl.SetCursor(handCursor);
    }

    public static unsafe void SetDefault()
    {
        if (currentCursor == defaultCursor) return;
        currentCursor = defaultCursor;
        _sdl.SetCursor(defaultCursor);
    }

    public static unsafe void SetText()
    {
        if (currentCursor == textCursor) return;
        currentCursor = textCursor;
        _sdl.SetCursor(textCursor);
    }

    public static unsafe void SetMove()
    {
        if (currentCursor == moveCursor) return;
        currentCursor = moveCursor;
        _sdl.SetCursor(moveCursor);
    }

    public static unsafe void SetWait()
    {
        if (currentCursor == waitCursor) return;
        currentCursor = waitCursor;
        _sdl.SetCursor(waitCursor);
    }

    public static unsafe void SetNotAllowed()
    {
        if (currentCursor == notAllowedCursor) return;
        currentCursor = notAllowedCursor;
        _sdl.SetCursor(notAllowedCursor);
    }

    public static unsafe void SetCrosshair()
    {
        if (currentCursor == crosshairCursor) return;
        currentCursor = crosshairCursor;
        _sdl.SetCursor(crosshairCursor);
    }

    public static unsafe void SetGrab()
    {
        if (currentCursor == grabCursor) return;
        currentCursor = grabCursor;
        _sdl.SetCursor(grabCursor);
    }

    public static unsafe void Dispose()
    {
        _sdl.FreeCursor(defaultCursor);
        _sdl.FreeCursor(handCursor);
        _sdl.FreeCursor(textCursor);
        _sdl.FreeCursor(moveCursor);
        _sdl.FreeCursor(waitCursor);
        _sdl.FreeCursor(notAllowedCursor);
        _sdl.FreeCursor(crosshairCursor);
        _sdl.FreeCursor(grabCursor);
    }
}