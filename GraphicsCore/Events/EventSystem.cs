using System;

namespace GraphicsCore.Events;

public class EventSystem
{
    public static EventSystem? Instance {get; private set;}

    public readonly Event OnClick = new();
    public readonly Event OnMouseEnter = new();
    public readonly Event OnMouseExit = new();

    public EventSystem(){Instance = this;}
}
