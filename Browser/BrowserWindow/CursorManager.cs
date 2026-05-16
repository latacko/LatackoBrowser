using Silk.NET.SDL;

public static class CursorManager
{
    private static Sdl _sdl;
    private static unsafe Cursor* _defaultCursor;
    private static unsafe Cursor* _handCursor;
    private static unsafe Cursor* _currentCursor;

    public static unsafe void Init(Sdl sdl)
    {
        _sdl = sdl;
        _defaultCursor = _sdl.CreateSystemCursor(SystemCursor.SystemCursorArrow);
        _handCursor = _sdl.CreateSystemCursor(SystemCursor.SystemCursorHand);
        _currentCursor = _defaultCursor;
    }

    public static unsafe void SetHand()
    {
        if (_currentCursor == _handCursor) return;
        _currentCursor = _handCursor;
        _sdl.SetCursor(_handCursor);
    }

    public static unsafe void SetDefault()
    {
        if (_currentCursor == _defaultCursor) return;
        _currentCursor = _defaultCursor;
        _sdl.SetCursor(_defaultCursor);
    }

    public static unsafe void Dispose()
    {
        _sdl.FreeCursor(_defaultCursor);
        _sdl.FreeCursor(_handCursor);
    }
}