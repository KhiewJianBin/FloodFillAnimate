using UnityEngine;

public static class Texture2DExtension
{
    public static Texture2D Clone(Texture2D source)
    {
        Texture2D clone = new Texture2D(source.width, source.height);
        clone.filterMode = source.filterMode;
        clone.SetPixels32(source.GetPixels32());
        clone.Apply();
        return clone;
    }
}
