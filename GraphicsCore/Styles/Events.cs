namespace GraphicCore;

public struct Events
{
    RuntimeModelData element;
    public delegate void ElementEvent(RuntimeModelData element);
    event ElementEvent OnClick;
    event ElementEvent OnMouseDown;
    event ElementEvent OnMouseUp;
    event ElementEvent OnMouseOut;
    event ElementEvent OnMouseOver;

    public Events(RuntimeModelData element)
    {
        this.element = element;
    }

    #region OnClick
    public Events AddOnClick(ElementEvent elementEvent)
    {
        OnClick += elementEvent;
        return this;
    }

    public Events RemoveOnClick(ElementEvent elementEvent)
    {
        OnClick -= elementEvent;
        return this;
    }

    public readonly void ExecuteOnClick()
    {
        OnClick?.Invoke(element);
    }
    #endregion

    #region OnMouseDown
    public Events AddOnMouseDown(ElementEvent elementEvent)
    {
        OnMouseDown += elementEvent;
        return this;
    }

    public Events RemoveOnMouseDown(ElementEvent elementEvent)
    {
        OnMouseDown -= elementEvent;
        return this;
    }

    public readonly void ExecuteOnMouseDown()
    {
        OnMouseDown?.Invoke(element);
    }
    #endregion

    #region OnMouseUp
    public Events AddOnMouseUp(ElementEvent elementEvent)
    {
        OnMouseUp += elementEvent;
        return this;
    }

    public Events RemoveOnMouseUp(ElementEvent elementEvent)
    {
        OnMouseUp -= elementEvent;
        return this;
    }

    public readonly void ExecuteOnMouseUp()
    {
        OnMouseUp?.Invoke(element);
    }
    #endregion

    #region OnMouseOut
    public Events AddOnMouseOut(ElementEvent elementEvent)
    {
        OnMouseOut += elementEvent;
        return this;
    }

    public Events RemoveOnMouseOut(ElementEvent elementEvent)
    {
        OnMouseOut -= elementEvent;
        return this;
    }

    public readonly void ExecuteOnMouseOut()
    {
        OnMouseOut?.Invoke(element);
    }
    #endregion

    #region OnMouseOver
    public Events AddOnMouseOver(ElementEvent elementEvent)
    {
        OnMouseOver += elementEvent;
        return this;
    }

    public Events RemoveOnMouseOver(ElementEvent elementEvent)
    {
        OnMouseOver -= elementEvent;
        return this;
    }

    public readonly void ExecuteOnMouseOver()
    {
        OnMouseOver?.Invoke(element);
    }
    #endregion
}