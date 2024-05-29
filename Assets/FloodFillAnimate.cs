using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FloodFillAnimate : MonoBehaviour
{
    [SerializeField] Image FloodFillImage;
    [SerializeField] Color ColorToFill;
    RectTransform RT;

    void Awake()
    {
        RT = FloodFillImage.GetComponent<RectTransform>();
    }
    // Update is called once per frame
    void Update()
    {
        var mousePos = Input.mousePosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(RT, mousePos, null , out Vector2 localPos);
        Vector2 imageSize = RT.sizeDelta;
        Vector2 normalizedimagePos = new Vector2((localPos.x + imageSize.x * 0.5f) / imageSize.x , (localPos.y + imageSize.y * 0.5f) / imageSize.y);

        bool isMouseWithin = normalizedimagePos.x >= 0 && normalizedimagePos.y >= 0;

        if(isMouseWithin && Input.GetMouseButtonDown(0)) 
        {
            var texture = FloodFillImage.sprite.texture;
            int idx = (int)(normalizedimagePos.x * texture.width);
            int idy = (int)(normalizedimagePos.y * texture.height);

            StartFloodFill(FloodFillImage, idx, idy);
        }
    }
    void StartFloodFill(Image image,int idx,int idy)
    {
        Texture2D texture = image.sprite.texture;
        var newTexture = Texture2DExtension.Clone(texture);

        var buffer = newTexture.GetPixels32();
        var index = idx + idy * newTexture.width;

        RecursiveFloodFill(ref buffer, newTexture.width, newTexture.height,
            idx, idy, buffer[index], 0.1f, ColorToFill);

        newTexture.SetPixels32(buffer);
        newTexture.Apply();

        image.sprite = Sprite.Create(newTexture, image.sprite.rect, Vector2.zero);
    }

    /// <summary>
    /// Very Basic RecursiveFloodFill
    /// Note this will cause stack overflow due to lots of recursive calls
    /// </summary>
    void RecursiveFloodFill(ref Color32[] buffer, int xSize, int ySize,
        int idx, int idy, in Color targetColor, float threshold, Color colorToFill)
    {
        bool indexOutOfBounds = idx < 0 || idx > xSize || idy < 0 || idy > ySize;

        if (indexOutOfBounds) return;

        //1. If current node is not Inside return.
        var index = idx + idy * xSize;
        Color32 currentColor = buffer[index];
        bool isColorSimilar = ColorExtension.Compare_Euclidean(currentColor,targetColor) < threshold;
        if (!isColorSimilar) return;

        //2. Set the node
        buffer[index] = colorToFill;

        //3. Perform Flood-fill on the neighbours
        RecursiveFloodFill(ref buffer, xSize, ySize, idx + 1, idy, targetColor, threshold, colorToFill);
        RecursiveFloodFill(ref buffer, xSize, ySize, idx - 1, idy, targetColor, threshold, colorToFill);
        RecursiveFloodFill(ref buffer, xSize, ySize, idx, idy + 1, targetColor, threshold, colorToFill);
        RecursiveFloodFill(ref buffer, xSize, ySize, idx, idy - 1, targetColor, threshold, colorToFill);

        //4. End
    }
}


public static class Texture2DExtension
{
    public static Texture2D Clone(Texture2D source)
    {
        Texture2D clone = new Texture2D(source.width, source.height);
        clone.SetPixels32(source.GetPixels32());
        clone.Apply();
        return clone;
    }
}
public static class ColorExtension
{
    public static float Compare_Euclidean(Color colorMain, Color colorOther)
    {
        return Vector4.Magnitude(colorMain - colorOther);
    }

    public static float Compare_LumaRec601(Color colorMain, Color colorOther)
    {
        var lumaY_colorMain = 0.299f * colorMain.r + 0.587f * colorMain.g + 0.114f * colorMain.b;
        var lumaY_colorOther = 0.299f * colorOther.r + 0.587f * colorOther.g + 0.114f * colorOther.b;
        return lumaY_colorMain - lumaY_colorOther;
    }
    public static float Compare_LumaRec709(Color colorMain, Color colorOther)
    {
        var lumaY_colorMain = 0.2126f * colorMain.r + 0.7152f * colorMain.g + 0.0722f * colorMain.b;
        var lumaY_colorOther = 0.2126f * colorOther.r + 0.7152f * colorOther.g + 0.0722f * colorOther.b;
        return lumaY_colorMain - lumaY_colorOther;
    }

    public static float Compare_DeltaE_CIE76(Color colorMain, Color colorOther)
    {
        throw new NotImplementedException();
    }

    public static void RGBToXYZ()
    {
        throw new NotImplementedException();
        //http://www.brucelindbloom.com/index.html?Eqn_DeltaE_CIE76.html
    }
    public static LabColor XYZToLAB()
    {
        throw new NotImplementedException();
        //http://www.brucelindbloom.com/index.html?Eqn_DeltaE_CIE76.html

        // conversion algorithm described here: http://www.brucelindbloom.com/index.html?Eqn_XYZ_to_Lab.html
        //double Xr = _targetWhitePoint.X, Yr = _targetWhitePoint.Y, Zr = _targetWhitePoint.Z;

        //double xr = sourceColor.X / Xr, yr = sourceColor.Y / Yr, zr = sourceColor.Z / Zr;

        //var fx = f(xr);
        //var fy = f(yr);
        //var fz = f(zr);

        //var L = 116 * fy - 16;
        //var a = 500 * (fx - fy);
        //var b = 200 * (fy - fz);

        //var targetColor = new LabColor(in L, in a, in b);
        //return targetColor;

        //private static double f(double cr)
        //{
        //    var fc = cr > Epsilon ? Pow(cr, 1 / 3d) : (Kappa * cr + 16) / 116d;
        //    return fc;
        //}
    }

    public struct LabColor
    {
        /// <summary>
        /// L* (Lightness 0 - 100)
        /// </summary>
        public readonly float L;

        /// <summary>
        /// a* (Red/Green from -100 to 100)
        /// </summary>
        public readonly float a;

        /// <summary>
        /// b* (Blue/Yellow from -100 to 100)
        /// </summary>
        public readonly float b;

        /// <param name="l">L* (Lightness 0 - 100)</param>
        /// <param name="a">a* (Red/Green from -100 to 100)</param>
        /// <param name="b">b* (Blue/Yellow from -100 to 100)</param>
        public LabColor(float L, float a, float b)
        {
            this.L = L;
            this.a = a;
            this.b = b;
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "Lab [L={0:0.##}, a={1:0.##}, b={2:0.##}]", L, a, b);
    }
}
