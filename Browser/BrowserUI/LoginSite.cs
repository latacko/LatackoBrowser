using System;
using GraphicsCore.Styles;
using Renderer;
using Units;

namespace Browser.BrowserUI;

public class LoginSite: ISiteController
{
    Document document;

    public void SetSetDocument(Document document)
    {
        this.document = document;
    }

    public void Start()
    {
        var _background = document.CreateElement()
        .SetStyle(new Style("Backround")
            .SetLayout(layout=>layout
                .SetWidth(new(100, UnitType.lvw))
                .SetHeight(new(100, UnitType.lvh))
            )
            .SetProperties(properties=>properties
                .SetBackgroundColor(0,0,0, 0.5f)
            )
        );

        var _form = document.CreateElement()
        .SetStyle(new Style("form")
            .SetLayout(layout=>layout
                .SetWidth(new(50, UnitType.lvw))
                .SetHeight(new(50, UnitType.lvw))
            )
            .SetProperties(properties=>properties
                .SetBackgroundColor(1,1,1,1)
            )
        )
        .SetEvents(events=>events
            .AddOnClick((e) =>
            {
                Console.WriteLine("Zostałem kliknięty ");
            }));
    }

    public void Update()
    {

    }
}
