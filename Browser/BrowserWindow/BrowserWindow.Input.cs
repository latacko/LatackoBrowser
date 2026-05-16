using System.Numerics;
using Silk.NET.Input;

namespace Browser;

public partial class BrowserWindow
{
    private void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        Console.WriteLine("Key down: " + key);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int arg3)
    {
        Console.WriteLine("Key up: " + key);
    }

    private void OnMouseClick(IMouse mouse, MouseButton button, System.Numerics.Vector2 pos)
    {
        Console.WriteLine(button + " pos: " + pos);
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 vector)
    {
        // throw new NotImplementedException();
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel wheel)
    {
        // throw new NotImplementedException();
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        Console.WriteLine(button);
    }
}