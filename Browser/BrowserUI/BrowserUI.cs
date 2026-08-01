using Browser;
using GraphicCore;
using GraphicCore.Styles;
using GraphicsCore;
using ObjectCore;
using ObjectCore.Textures;
using Silk.NET.Vulkan;
using TextCore;
using Units;
using Vulkan;

public class BrowserUI
{
    public RuntimeObject TopBar;
    public RuntimeObject Karta;
    public RuntimeObject hellothere;
    public RuntimeTextContainer hellothereText;
    public RuntimeTextContainer TextTest;


    public void Create()
    {
        var _texture = TexturesManager.LoadTexture("textures/everythingIsFine.png");
        var _texture2 = TexturesManager.LoadTexture("textures/hellothere.png");


        TopBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0] as ObjectShader, BrowserWindow.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad), null)
        .SetStyle(new Style("Background")
            .SetLayout(
                (layout) => layout
                    .SetLeft(new(0)).SetTop(new(0))
                    .SetWidth(new(100, UnitType.lvw)).SetHeight(new(20, UnitType.lvh))
            )
            .SetProperties(
                (properties) => properties
                    .SetBackgroundColor255(41, 44, 48, 255)
                    .SetTransition(.25f)
                    .SetBorderRadius(new(20, UnitType.px))
            )
        );

        Karta = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0] as ObjectShader, BrowserWindow.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad), null, TopBar)
        .SetStyle(new Style("Karta")
            .SetLayout(layout => layout
                .SetLeft(new(-10)).SetTop(new(-10))
                .SetWidth(new(10, UnitType.lvw)).SetHeight(new(100, UnitType.percentageHeight))
            )
            .SetProperties(properties => properties
                .SetCursor(CursorType.pointer)
                .SetBackgroundColor255(65, 68, 71, 255)
                .SetBorderRadius(new(20))
                .SetTransition(2f)
            )
        ).SetEvents(
            events => events
                .AddOnClick((element) =>
                {
                    Console.WriteLine("Klikam element jakiś");
                })
                .AddOnMouseOver(element =>
                {
                    element.Style.SetProperties(element.Style.Properties
                        .SetBackgroundColor255(60, 60, 78, 255)
                    );
                })
                .AddOnMouseOut(element =>
                {
                    element.Style.SetProperties(element.Style.Properties
                        .SetBackgroundColor255(255, 68, 71, 255)
                    );
                })
        );

        TextTest = TextManager.AddText("OLIIIIIIIII tekst naprawiony w końcu!!!", Karta)
        .SetStyle(new Style("Tekst style")
            .SetFontProperties(properties => properties
                .SetFontSize(new(100, UnitType.percentageHeight))
                .SetCursor(CursorType.text)
            )
        );

        // hellothere = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0] as ObjectShader, BrowserWindow.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad), _texture2)
        // .SetStyle(new Style()
        //     .SetLayout(layout => layout
        //         .SetLeft(new(50, UnitType.lvw)).SetTop(new(0))
        //         .SetWidth(new(50, UnitType.lvw)).SetHeight(new(100, UnitType.px))
        //     )
        //     .SetTransform(transform => transform
        //         .SetTranslate(new UIUnit(25, UnitType.lvw), new())
        //     )
        //     .SetProperties(properties => properties
        //         .SetBorderRadius(new(20, UnitType.px))
        //     )
        // ).SetEvents(events => events
        //     .AddOnClick((e) =>
        //     {
        //         Console.WriteLine("hello there");
        // }));

        // hellothereText = TextManager.AddText(@"Lorem ipsum dolor sit amet, adipiscing ea culpa laborum ipsum duis do. Proident reprehenderit id nostrud aliquip sit velit. Et in excepteur ipsum consequat. Nisi est nostrud nulla velit, ullamco nisi officia sit sunt. Sunt laboris occaecat culpa mollit. Sit nisi proident sunt cupidatat irure sunt dolore, reprehenderit ullamco dolore deserunt dolore ullamco culpa et.", hellothere)
        // .SetStyle(new Style()
        //     .SetFontProperties(properties => properties
        //         .SetFontSize(new(25))
        //         .SetCursor(CursorType.text)
        //     )
        // );

        //         hellothereText = TextManager.AddText(@"Lorem ipsum dolor sit amet, adipiscing ea culpa laborum ipsum duis do. Proident reprehenderit id nostrud aliquip sit velit. Et in excepteur ipsum consequat. Nisi est nostrud nulla velit, ullamco nisi officia sit sunt. Sunt laboris occaecat culpa mollit. Sit nisi proident sunt cupidatat irure sunt dolore, reprehenderit ullamco dolore deserunt dolore ullamco culpa et. Exercitation eiusmod ex dolore sunt duis qui eu, est eu est et et lorem, qui sint non adipiscing quis dolor proident ad. Aute consectetur sit deserunt sunt cillum. Commodo qui veniam est cupidatat. Elit commodo ut anim cupidatat culpa.
        // Commodo occaecat ex exercitation. Ut nulla magna anim cupidatat nulla voluptate. Ex ullamco voluptate ex dolor sit pariatur. Culpa non proident consectetur exercitation sit nisi. Irure ea eu quis ut minim consectetur. Aliquip enim nulla aliqua pariatur irure. Nisi excepteur proident dolor labore, magna aute magna proident deserunt irure cupidatat. Lorem est velit qui esse cillum. Mollit do proident ex consequat.
        // Do anim irure nisi anim. Cillum ad magna adipiscing incididunt. Sint est aliquip esse amet ut aute qui. Laborum mollit est ipsum reprehenderit culpa duis. Minim dolor eu duis reprehenderit eiusmod, sit consequat laboris deserunt amet, aute lorem id sint dolore. Cillum sed consectetur non ut. Velit culpa reprehenderit voluptate adipiscing. Amet adipiscing mollit sunt.
        // Ea nisi dolore ullamco officia nisi. Dolor consectetur irure ex. Amet esse eu cupidatat aute voluptate laboris. Veniam aliquip non sit id. Laborum tempor id esse, laborum exercitation cupidatat aliqua cupidatat aute elit magna. Officia ad eu excepteur ex velit ipsum. Esse lorem aliquip excepteur sunt esse veniam, ea laborum occaecat duis est. Do anim tempor nostrud duis, esse deserunt qui dolore, sed dolor pariatur id deserunt sed qui sunt. Non reprehenderit duis et id incididunt.
        // Irure est lorem dolore voluptate sit voluptate irure. Ad aliquip ullamco eu mollit sint ex. Officia exercitation do in do enim culpa, et veniam dolor cupidatat incididunt irure. Elit non et aliqua sunt elit sint enim. Lorem id dolore id officia occaecat, aute et ex labore sunt.", hellothere)
        //         .SetProperties(properties => properties
        //             .SetFontSize(new(25))
        //             .SetCursor(CursorType.text)
        //         );

        // BottomBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], VulkanManager.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad))
        // .SetPosition(new(0, 45))
        // .SetSize(new(100, 45, UnitType.lvw, UnitType.px))
        // .SetProperties(properties=>properties.SetBackgroundColor255(65, 68, 71, 255));
        // clickableElements = [TopBar, BottomBar];

        // TopBar.SetTransform(TopBar.Transform.SetScale(1).SetRotation(new(0,2,3)));

        // LeftBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], VulkanManager.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        // LeftBar.SetPosition(new(0, 0));
        // LeftBar.SetSize(new(50, 100, UnitType.lvw, UnitType.lvh));
        // LeftBar.SetProperties(LeftBar.Properties.SetBackgroundColor255(0, 255, 0, 255));

        // BottomBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], VulkanManager.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        // BottomBar.SetPosition(new(0, 50, yType: UnitType.lvh));
        // BottomBar.SetSize(new(100, 50, UnitType.lvw, UnitType.lvh));
        // BottomBar.SetProperties(BottomBar.Properties.SetBackgroundColor255(0, 0, 255, 255));
    }
}