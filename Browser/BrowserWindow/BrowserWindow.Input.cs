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

    RuntimeModelData? currentElement;

    RuntimeModelData? GetElementUnderCursor(RuntimeModelData? parentElement, List<RuntimeModelData> elements, Vector2 pos)
    {
        int _elementsCount = elements.Count;
        for (int i = 0; i < _elementsCount; i++)
        {
            if (BoundsHelper.Contains(elements[i].Layout.Bounds, pos))
            {
                if (elements[i].Children == null)
                    return elements[i];
                else
                    return GetElementUnderCursor(elements[i], elements[i].Children, pos);
            }
        }
        return parentElement;
    }

    private void OnMouseMove(IMouse mouse, Vector2 pos)
    {
        var _focusedElement = GetElementUnderCursor(null, ObjectsManager.ParentElements, pos);
        if (_focusedElement != null)
        {
            if (currentElement == _focusedElement) return;

            currentElement = _focusedElement;
            switch (currentElement.Properties.Cursor)
            {
                case Properties.CursorType.defaultCursor:
                    CursorManager.SetDefault();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.pointer:
                    CursorManager.SetHand();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.text:
                    CursorManager.SetText();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.move:
                    CursorManager.SetMove();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.wait:
                    CursorManager.SetWait();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.cursorHelp:
                    CursorManager.SetNotAllowed();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.notAllowed:
                    CursorManager.SetNotAllowed();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.progress:
                    CursorManager.SetWait();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.crosshair:
                    CursorManager.SetCrosshair();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.grab:
                    CursorManager.SetMove();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.grabbing:
                    CursorManager.SetMove();
                    CursorManager.SetVisible(true);
                    break;
                case Properties.CursorType.none:
                    CursorManager.SetDefault();
                    CursorManager.SetVisible(false);
                    break;
            }
        }
        else if (currentElement != null)
        {
            currentElement = null;
            CursorManager.SetDefault();
            CursorManager.SetVisible(true);
        }
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