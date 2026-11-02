using System;

namespace Renderer;

public interface ISiteController
{
    public void SetDocument(Document document);
    public void Start();
    public void Update();
}
