using System;
using InstanceFinderCore;

namespace GraphicsCore.Events;

[Instance]
public class EventSystem
{

    public readonly Event OnClick = new();
    public readonly Event OnMouseEnter = new();
    public readonly Event OnMouseExit = new();
}
