using GraphicsCore.Events;
using Units;

namespace GraphicsCore;

public readonly struct EventHelper
{
    readonly EventSystem eventSystem;
    readonly VisualElement visualElement;

    public EventHelper(EventSystem eventSystem, VisualElement visualElement)
    {
        this.eventSystem = eventSystem;
        this.visualElement = visualElement;
    }

    #region OnClick
    public EventHelper AddOnClick(Event.ElementEvent elementEvent)
    {
        eventSystem.OnClick.AddElement(visualElement, elementEvent);
        return this;
    }

    public EventHelper RemoveOnClick(Event.ElementEvent elementEvent)
    {
        eventSystem.OnClick.Removelement(visualElement, elementEvent);
        return this;
    }
    #endregion

    #region OnMouseDown
    public EventHelper AddOnMouseDown(Event.ElementEvent elementEvent)
    {
        return this;
    }

    public EventHelper RemoveOnMouseDown(Event.ElementEvent elementEvent)
    {
        return this;
    }
    #endregion

    #region OnMouseUp
    public EventHelper AddOnMouseUp(Event.ElementEvent elementEvent)
    {
        return this;
    }

    public EventHelper RemoveOnMouseUp(Event.ElementEvent elementEvent)
    {
        return this;
    }
    #endregion

    #region OnMouseOut
    public EventHelper AddOnMouseExit(Event.ElementEvent elementEvent)
    {
        eventSystem.OnMouseExit.AddElement(visualElement, elementEvent);
        return this;
    }

    public EventHelper RemoveOnMouseExit(Event.ElementEvent elementEvent)
    {
        eventSystem.OnMouseExit.Removelement(visualElement, elementEvent);
        return this;
    }
    #endregion

    #region OnMouseOver
    public EventHelper AddOnMouseEnter(Event.ElementEvent elementEvent)
    {
        eventSystem.OnMouseEnter.AddElement(visualElement, elementEvent);
        return this;
    }

    public EventHelper RemoveOnMouseEnter(Event.ElementEvent elementEvent)
    {
        eventSystem.OnMouseEnter.Removelement(visualElement, elementEvent);
        return this;
    }
    #endregion
}