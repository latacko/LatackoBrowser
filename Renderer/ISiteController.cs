using System;

namespace Renderer;

public interface ISiteController
{
    public void SetSetDocument(Document document);
    public void Start();
    public void Update();
}
