using System;

namespace GraphicsCore.Events;

public class EventSystem
{
    public readonly Event OnClick = new();
    public readonly Event OnMouseEnter = new();
    public readonly Event OnMouseExit = new();
}
