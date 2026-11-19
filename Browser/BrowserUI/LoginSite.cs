using System;
using GraphicsCore.Styles;
using Renderer;
using Units;

namespace Browser.BrowserUI;

public class LoginSite: ISiteController
{
    Document document;

    public void SetDocument(Document document)
    {
        this.document = document;
    }

    public void Start()
    {
        var _background = document.CreateElement(DocumentElements.Div)
        .SetInlineStyle(new Style("Backround")
            .SetLayout(layout=>layout
                .SetWidth(new(15, UnitType.lvw))
                .SetHeight(new(15, UnitType.lvh))
            )
            .SetProperties(properties=>properties
                .SetBackgroundColor(1,0,0, 0.25f)
            )
        );

        document.Body.AddChild(_background);

        // var _card = document.CreateElement(DocumentElements.Div)
        // .SetInlineStyle(new Style("Card")
        //     .SetLayout(layout=>layout
        //         .SetWidth(new(25, UnitType.percentageWidth))
        //         .SetHeight(new(25, UnitType.percentageHeight))
        //         )
        //     .SetProperties(properties=>properties
        //         .SetBackgroundColor(0, 1, 0, 0.25f)
        //     )
        // );
        // _background.AddChild(_card);

        // var _form = document.CreateElement(0)
        // .SetStyle(new Style("form")
        //     .SetLayout(layout=>layout
        //         .SetWidth(new(50, UnitType.lvw))
        //         .SetHeight(new(50, UnitType.lvw))
        //     )
        //     .SetProperties(properties=>properties
        //         .SetBackgroundColor(1,1,1,1)
        //     )
        // )
        // .SetEvents(events=>events
        //     .AddOnClick((e) =>
        //     {
        //         Console.WriteLine("Zostałem kliknięty ");
        //     }));
    }

    public void Update()
    {

    }
}
