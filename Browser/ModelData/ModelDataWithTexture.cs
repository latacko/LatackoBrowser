using Browser;
using Browser.DataTypes;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public class ModelDataWithTexture : IDisposable
{
    public Vertex[] Vertices;
    public uint[] Indices;

    public Buffer VertexBuffer;
    public DeviceMemory VertexBufferMemory;

    public Buffer IndexBuffer;
    public DeviceMemory IndexBufferMemory;

    public Image TextureImage;
    public DeviceMemory TextureImageMemory;
    public ImageView TextureImageView;

    public ModelDataWithTexture(Vertex[] vertices, uint[] indices, string texturePath)
    {
        Vertices = vertices;
        Indices = indices;

        BrowserWindow.Instance.CreateTextureImage(texturePath, ref TextureImage, ref TextureImageMemory);
        TextureImageView = BrowserWindow.Instance.CreateTextureImageView(TextureImage);
        Console.WriteLine("Creating vertex buffer");
        Console.WriteLine("Creating index buffer");
        BrowserWindow.Instance.CreateIndexBuffer(Indices, ref IndexBuffer, ref IndexBufferMemory);
        Console.WriteLine("model loaded");
    }

    public void Dispose()
    {
        BrowserWindow.Instance.DestroyTexture(TextureImage, TextureImageMemory, TextureImageView);

        BrowserWindow.Instance.DestroyBuffer(VertexBuffer, VertexBufferMemory);
        BrowserWindow.Instance.DestroyBuffer(IndexBuffer, IndexBufferMemory);
    }
}