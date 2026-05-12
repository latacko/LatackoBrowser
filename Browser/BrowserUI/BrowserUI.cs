using Browser;

public class BrowserUI
{
    public void Create()
    {
        var _topBar = ObjectsManager.AddObject(BrowserWindow.loadedShader[0], BrowserWindow.primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        _topBar.SetPosition(new(25, 25, UnitType.lvw, UnitType.lvw));
        _topBar.SetSize(new(50, 50, UnitType.lvw, UnitType.lvw));
        _topBar.SetBackgroundColor255(255, 0, 0, 250);

        var _leftBar = ObjectsManager.AddObject(BrowserWindow.loadedShader[0], BrowserWindow.primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        _leftBar.SetPosition(new(0, 0));
        _leftBar.SetSize(new(50, 100, UnitType.lvw, UnitType.lvh));
        _leftBar.SetBackgroundColor255(0, 255, 0, 250);

        var _bottomBar = ObjectsManager.AddObject(BrowserWindow.loadedShader[0], BrowserWindow.primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        _bottomBar.SetPosition(new(0, 50, yType: UnitType.lvh));
        _bottomBar.SetSize(new(100, 50, UnitType.lvw, UnitType.lvh));
        _bottomBar.SetBackgroundColor255(0, 0, 255, 250);
    }
}