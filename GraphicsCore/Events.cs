namespace GraphicCore;

public class Events<T>: EventsBase where T : RuntimeModelData
{
    T element;
    public delegate void ElementEvent(T element);
    event ElementEvent OnClick;
    event ElementEvent OnMouseDown;
    event ElementEvent OnMouseUp;
    event ElementEvent OnMouseOut;
    event ElementEvent OnMouseOver;

    public Events(T element)
    {
        this.element = element;
    }

    #region OnClick
    public Events<T> AddOnClick(ElementEvent elementEvent)
    {
        OnClick += elementEvent;
        return this;
    }

    public Events<T> RemoveOnClick(ElementEvent elementEvent)
    {
        OnClick -= elementEvent;
        return this;
    }

    public override void ExecuteOnClick()
    {
        OnClick?.Invoke(element);
    }
    #endregion

    #region OnMouseDown
    public Events<T> AddOnMouseDown(ElementEvent elementEvent)
    {
        OnMouseDown += elementEvent;
        return this;
    }

    public Events<T> RemoveOnMouseDown(ElementEvent elementEvent)
    {
        OnMouseDown -= elementEvent;
        return this;
    }


    public override void ExecuteOnMouseDown()
    {
        OnMouseDown?.Invoke(element);
    }
    #endregion

    #region OnMouseUp
    public Events<T> AddOnMouseUp(ElementEvent elementEvent)
    {
        OnMouseUp += elementEvent;
        return this;
    }

    public Events<T> RemoveOnMouseUp(ElementEvent elementEvent)
    {
        OnMouseUp -= elementEvent;
        return this;
    }

    public override void ExecuteOnMouseUp()
    {
        OnMouseUp?.Invoke(element);
    }
    #endregion

    #region OnMouseOut
    public Events<T> AddOnMouseOut(ElementEvent elementEvent)
    {
        OnMouseOut += elementEvent;
        return this;
    }

    public Events<T> RemoveOnMouseOut(ElementEvent elementEvent)
    {
        OnMouseOut -= elementEvent;
        return this;
    }

    public override void ExecuteOnMouseOut()
    {
        OnMouseOut?.Invoke(element);
    }
    #endregion

    #region OnMouseOver
    public Events<T> AddOnMouseOver(ElementEvent elementEvent)
    {
        OnMouseOver += elementEvent;
        return this;
    }

    public Events<T> RemoveOnMouseOver(ElementEvent elementEvent)
    {
        OnMouseOver -= elementEvent;
        return this;
    }

    public override void ExecuteOnMouseOver()
    {
        OnMouseOver?.Invoke(element);
    }
    #endregion
}