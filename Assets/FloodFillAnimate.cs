using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FloodFillAnimate : MonoBehaviour
{
    [SerializeField] Image FloodFillImage;  
    [SerializeField] Color ColorToFill;
    [SerializeField, Range(0f, 1f)] float CompareThreshold = 0.1f;

    RectTransform RT;

    public bool autoStep = true;
    public float LogicStepDuration = 0.1f;
    float LogicStepTimer = 0;

    bool isLogicPaused = true;
    bool UnpauseLogic() => !isLogicPaused;

    Coroutine fillCoroutine;

    void Awake()
    {
        print(ColorExtension.Compare_Euclidean(Color.black, Color.white));
        RT = FloodFillImage.GetComponent<RectTransform>();
    }

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
            var buffer = texture.GetPixels32();
            int idx = (int)(normalizedimagePos.x * texture.width);
            int idy = (int)(normalizedimagePos.y * texture.height);

            if(fillCoroutine != null) StopCoroutine(fillCoroutine);
            fillCoroutine = StartCoroutine(StartFloodFill(buffer, idx, idy, texture.width, texture.height));
        }

        if (autoStep)
        {
            //one step per each LogicStepDuration time frame
            LogicStepTimer += Time.deltaTime;
            if (LogicStepTimer >= LogicStepDuration)
            {
                isLogicPaused = false;
                LogicStepTimer = 0;
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.Space))
            {
                isLogicPaused = false;
            }
        }
    }
    IEnumerator StartFloodFill(Color32[] buffer, int idx, int idy, int xSize, int ySize)
    {
        var index = idx + idy * xSize;

        yield return RecursiveFloodFill(idx, idy, buffer, xSize, ySize, buffer[index], CompareThreshold, ColorToFill);

        yield return null;

        UpdateImageTexture(FloodFillImage, buffer);
    }

    void UpdateImageTexture(Image image, Color32[] pixels)
    {
        if (image == null | image.sprite == null) return;

        Texture2D texture = image.sprite.texture;
        var newTexture = Texture2DExtension.Clone(texture);

        newTexture.SetPixels32(pixels);
        newTexture.Apply();
        image.sprite = Sprite.Create(newTexture, image.sprite.rect, Vector2.zero);
    }

    /// <summary>
    /// Very Basic RecursiveFloodFill
    /// Note this will cause stack overflow due to lots of recursive calls
    /// </summary>
    IEnumerator RecursiveFloodFill(int idx, int idy, Color32[] buffer, int xSize, int ySize,
         Color targetColor, float threshold, Color colorToFill)
    {
         bool indexOutOfBounds = idx < 0 || idx > xSize - 1 || idy < 0 || idy > ySize - 1;

        if (indexOutOfBounds) yield break;

        //1. If current node is not Inside return.
        var index = idx + idy * xSize;
        Color32 currentColor = buffer[index];
        bool isColorSimilar = ColorExtension.Compare_Euclidean(currentColor, targetColor) < threshold;
        if (!isColorSimilar) yield break;

        //2. Set the node
        buffer[index] = colorToFill;

        UpdateImageTexture(FloodFillImage, buffer);
        isLogicPaused = true; yield return new WaitUntil(UnpauseLogic);

        //3. Perform Flood-fill on the neighbours
        yield return RecursiveFloodFill(idx + 1, idy, buffer, xSize, ySize, targetColor, threshold, colorToFill);
        yield return RecursiveFloodFill(idx - 1, idy, buffer, xSize, ySize, targetColor, threshold, colorToFill);
        yield return RecursiveFloodFill(idx, idy + 1, buffer, xSize, ySize, targetColor, threshold, colorToFill);
        yield return RecursiveFloodFill(idx, idy - 1, buffer, xSize, ySize, targetColor, threshold, colorToFill);
    }
}