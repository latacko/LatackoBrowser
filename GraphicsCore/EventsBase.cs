using System;

namespace GraphicCore;

public abstract class EventsBase
{
    public abstract void ExecuteOnClick();
    public abstract void ExecuteOnMouseDown();
    public abstract void ExecuteOnMouseUp();
    public abstract void ExecuteOnMouseOut();
    public abstract void ExecuteOnMouseOver();
}