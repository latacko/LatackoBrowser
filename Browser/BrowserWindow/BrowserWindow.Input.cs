using System.Numerics;
using GraphicsCore;
using Renderer;
using Silk.NET.Input;
using TextCore;

namespace Browser;

public partial class BrowserWindow
{
    bool wireFrameRendering = false;
    float fontSize = 16;
    private void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        Console.WriteLine("Key down: " + key);

        if (key == Key.W)
        {
            wireFrameRendering = !wireFrameRendering;
        } else if (key == Key.M)
        {
            TextShader.ShowMSDF = !TextShader.ShowMSDF;
        } else if (key == Key.L)
        {
            TextShader.ShowLOD = !TextShader.ShowLOD;
        } else if (key == Key.Right)
        {
            browserUI.TextTest.Style.SetFontProperties(fontProperties=>fontProperties.SetFontSize(new(++fontSize)));
        } else if (key == Key.Left)
        {
            browserUI.TextTest.Style.SetFontProperties(fontProperties=>fontProperties.SetFontSize(new(--fontSize)));
        }
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int arg3)
    {
        Console.WriteLine("Key up: " + key);
    }

    private void OnMouseClick(IMouse mouse, MouseButton button, System.Numerics.Vector2 pos)
    {
        if (currentElement == null) return;
        currentElement.Events.ExecuteOnClick();
    }

    VisualElement? currentElement;

    VisualElement? GetElementUnderCursor(VisualElement? parentElement, List<VisualElement> elements, Vector2 pos)
    {
        int _elementsCount = elements.Count;
        for (int i = _elementsCount - 1; i >= 0; i--)
        {
            if (BoundsHelper.Contains(elements[i].GetBounds(), pos))
            {
                return elements[i];
            }
        }
        return parentElement;
    }

    private void OnMouseMove(IMouse mouse, Vector2 pos)
    {
        var _focusedElement = GetElementUnderCursor(null, ObjectsManager.Elements, pos);
        if (_focusedElement != null)
        {
            if (currentElement == _focusedElement) return;

            if (currentElement != null)
            {
                ElementMouseOut();
            }

            currentElement = _focusedElement;
            currentElement.Events.ExecuteOnMouseOver();
            switch (currentElement.GetCursorType())
            {
                case GraphicsCore.CursorType.defaultCursor:
                    CursorManager.SetDefault();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.pointer:
                    CursorManager.SetHand();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.text:
                    CursorManager.SetText();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.move:
                    CursorManager.SetMove();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.wait:
                    CursorManager.SetWait();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.cursorHelp:
                    CursorManager.SetNotAllowed();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.notAllowed:
                    CursorManager.SetNotAllowed();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.progress:
                    CursorManager.SetWait();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.crosshair:
                    CursorManager.SetCrosshair();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.grab:
                    CursorManager.SetMove();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.grabbing:
                    CursorManager.SetMove();
                    CursorManager.SetVisible(true);
                    break;
                case GraphicsCore.CursorType.none:
                    CursorManager.SetDefault();
                    CursorManager.SetVisible(false);
                    break;
            }
        }
        else if (currentElement != null)
        {
            ElementMouseOut();
            currentElement = null;
            CursorManager.SetDefault();
            CursorManager.SetVisible(true);
        }
        // throw new NotImplementedException();
    }

    private void ElementMouseOut()
    {
        currentElement.Events.ExecuteOnMouseOut();
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