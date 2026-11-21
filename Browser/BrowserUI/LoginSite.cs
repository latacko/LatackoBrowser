using System;
using GraphicsCore.Styles;
using Renderer;
using Silk.NET.Vulkan;
using Units;

namespace Browser.BrowserUI;

public class LoginSite : ISiteController
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
            .SetLayout(layout => layout
                .SetWidth(new(15, UnitType.lvw))
                .SetHeight(new(15, UnitType.lvh))
            )
            .SetProperties(properties => properties
                .SetBackgroundColor(1, 0, 0, 0.25f)
            )
        );

        document.Body.AddChild(_background);

        var _background2 = document.CreateElement(DocumentElements.Div)
        .SetInlineStyle(new Style("Background2")
            .SetLayout(layout => layout
                .SetWidth(new(15, UnitType.lvw))
                .SetHeight(new(20, UnitType.lvh))

            )
            .SetProperties(properties => properties
                .SetBackgroundColor(0, 1, 0, 0.25f)
            )
        );

        document.Body.AddChild(_background2);

        var _background3 = document.CreateElement(DocumentElements.Div)
            .SetInlineStyle(new Style("Background3")
                .SetLayout(layout => layout
                    .SetWidth(new(90, UnitType.lvw))
                    .SetHeight(new(15, UnitType.lvh))
                )
                .SetProperties(properties => properties
                    .SetBackgroundColor(0, 1, 1, 0.8f)
                )
            );

        document.Body.AddChild(_background3);

        var _input = document.CreateElement(DocumentElements.Div)
            .SetInlineStyle(new Style("Input")
                .SetLayout(layout=>layout
                    .SetWidth(new UIUnit(50, UnitType.percentageWidth))
                    .SetHeight(new UIUnit(50, UnitType.percentageHeight))
                )
                .SetProperties(properties=>properties
                    .SetBackgroundColor(1,1,1,1)
                )
            );

        _background.AddChild(_input);
        
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
