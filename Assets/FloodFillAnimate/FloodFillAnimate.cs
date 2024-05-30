using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum FloodfillAlgo
{
    Recursion,
    DFSFloodFill,
    BFSFloodFill
}
public enum ColorComparisonAlgo
{
    Binary,
    Euclidean,
    LumaRec601
}

public class FloodFillAnimate : MonoBehaviour
{
    [SerializeField] Image FloodFillImage;  
    [SerializeField] Color ColorToFill;

    /// <summary>
    /// Threshold value to check if colors are different or not
    /// 0 - not different
    /// 1 - very different
    /// Compare threshold cannot be 0, otherwise algos will add visited cells/pixels to itself
    /// </summary>
    [SerializeField, Range(0f, 1f)] float DifferenceThreshold = 0f;
    [SerializeField] FloodfillAlgo FloodfillType;
    [SerializeField] ColorComparisonAlgo CompareType;

    RectTransform RT;

    public bool autoStep = true;
    public float LogicStepDuration = 0.1f;
    float LogicStepTimer = 0;

    bool isLogicPaused = true;
    bool UnpauseLogic() => !isLogicPaused;

    Coroutine fillCoroutine;

    void Awake()
    {
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

        switch (FloodfillType)
        {
            case FloodfillAlgo.Recursion:
                yield return RecursiveFloodFill(idx, idy, buffer, xSize, ySize, buffer[index], DifferenceThreshold, ColorToFill);
                break;
            case FloodfillAlgo.DFSFloodFill:
                yield return DFSFloodFill(idx, idy, buffer, xSize, ySize, DifferenceThreshold, ColorToFill);
                break;
            case FloodfillAlgo.BFSFloodFill:
                yield return BFSFloodFill(idx, idy, buffer, xSize, ySize, DifferenceThreshold, ColorToFill);
                break;
        }

        yield return null;

        UpdateImageTexture(FloodFillImage, buffer);
    }

    float CompareColor(Color color, Color other)
    {
        switch (CompareType)
        {
            case ColorComparisonAlgo.Binary:
                return ColorExtension.Compare_Binary(color, other);
            case ColorComparisonAlgo.Euclidean:
                return ColorExtension.Compare_Euclidean(color, other);
            case ColorComparisonAlgo.LumaRec601:
                return ColorExtension.Compare_LumaRec601(color, other);
            default:
                return 0.1f;
        }
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
    /// Note method will cause stack overflow due to lots of recursive calls
    /// </summary>
    IEnumerator RecursiveFloodFill(int idx, int idy, Color32[] buffer, int xSize, int ySize,
         Color targetColor, float threshold, Color32 colorToFill)
    {
         bool indexOutOfBounds = idx < 0 || idx > xSize - 1 || idy < 0 || idy > ySize - 1;

        if (indexOutOfBounds) yield break;

        //1. If current node is not Inside return.
        var index = idx + idy * xSize;
        Color32 currentColor = buffer[index];
        bool isVisited = ColorExtension.IsEqualTo(currentColor, colorToFill);
        bool isColorSimilar = CompareColor(currentColor, targetColor) <= threshold;
        if (isVisited || !isColorSimilar) yield break;

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

    /// <summary>
    /// Very Basic Recursive FloodFill BFS using a queue
    /// </summary>
    IEnumerator BFSFloodFill(int idx, int idy, Color32[] buffer, int xSize, int ySize,
        float threshold, Color32 colorToFill)
    {
        isLogicPaused = true;

        //1. Set first node and Target Color
        Queue<(int x, int y)> cellsToCheck = new();
        cellsToCheck.Enqueue((idx, idy));
        var index = idx + idy * xSize;
        Color targetColor = buffer[index];

        //1b. Color the first node
        buffer[index] = colorToFill;
        UpdateImageTexture(FloodFillImage, buffer);
        isLogicPaused = true; yield return new WaitUntil(UnpauseLogic);

        while (cellsToCheck.Count > 0)
        {
            //2. Pop and node from queue
            var currentCell = cellsToCheck.Dequeue();
            index = currentCell.x + currentCell.y * xSize;

            //3. queue Neighbours if color is similar
            var neighbours = FindNeighbours(currentCell, xSize, ySize);
            foreach ((int x, int y) neighbour in neighbours)
            {
                index = neighbour.x + neighbour.y * xSize;
                Color32 currentColor = buffer[index];
                bool isVisited = ColorExtension.IsEqualTo(currentColor, colorToFill);
                bool isColorSimilar = CompareColor(currentColor, targetColor) <= threshold;

                if (!isVisited && isColorSimilar)
                {
                    cellsToCheck.Enqueue(neighbour);

                    //4. set neighbour after queue
                    buffer[index] = colorToFill;
                    UpdateImageTexture(FloodFillImage, buffer);
                    isLogicPaused = true; yield return new WaitUntil(UnpauseLogic);
                }
            }

            //4. Continue loop until no more in queue - cellsToCheck
        }
    }

    /// <summary>
    /// Very Basic FloodFill DFS using a stack
    /// </summary>
    IEnumerator DFSFloodFill(int idx, int idy, Color32[] buffer, int xSize, int ySize,
        float threshold, Color32 colorToFill)
    {
        isLogicPaused = true;

        //1. Set first node and Target Color
        Stack<(int x, int y)> cellsToCheck = new();
        cellsToCheck.Push((idx, idy));
        var index = idx + idy * xSize;
        Color targetColor = buffer[index];

        //1b. Color the first node
        buffer[index] = colorToFill;
        UpdateImageTexture(FloodFillImage, buffer);
        isLogicPaused = true; yield return new WaitUntil(UnpauseLogic);

        while (cellsToCheck.Count > 0)
        {
            //2. Pop and Set the node from stack
            var currentCell = cellsToCheck.Pop();
            index = currentCell.x + currentCell.y * xSize;

            //3. Stack Neighbours if color is similar
            var neighbours = FindNeighbours(currentCell, xSize, ySize);
            foreach ((int x, int y) neighbour in neighbours) 
            {
                index = neighbour.x + neighbour.y * xSize;
                Color32 currentColor = buffer[index];
                bool isVisited = ColorExtension.IsEqualTo(currentColor, colorToFill);
                bool isColorSimilar = CompareColor(currentColor, targetColor) <= threshold;

                if (!isVisited && isColorSimilar)
                {
                    cellsToCheck.Push(neighbour);

                    //4. set neighbour after stack
                    buffer[index] = colorToFill;
                    UpdateImageTexture(FloodFillImage, buffer);
                    isLogicPaused = true; yield return new WaitUntil(UnpauseLogic);
                }
            }

            //4. Continue loop until no more in stack - cellsToCheck
        }
    }

    (int, int)[] FindNeighbours((int x,int y) cell, int sizeX, int sizeY)
    {
        List<(int, int)> result = new List<(int, int)> ();

        (int x, int y) leftCell = (cell.x - 1, cell.y);
        (int x, int y) rightCell = (cell.x + 1, cell.y);
        (int x, int y) upCell = (cell.x, cell.y + 1);
        (int x, int y) downCell = (cell.x, cell.y - 1);

        bool indexOutOfBounds;

        indexOutOfBounds = leftCell.x < 0 || leftCell.x > sizeX - 1 || leftCell.y < 0 || leftCell.y > sizeY - 1;
        if (!indexOutOfBounds) result.Add(leftCell);

        indexOutOfBounds = rightCell.x < 0 || rightCell.x > sizeX - 1 || rightCell.y < 0 || rightCell.y > sizeY - 1;
        if (!indexOutOfBounds) result.Add(rightCell);

        indexOutOfBounds = upCell.x < 0 || upCell.x > sizeX - 1 || upCell.y < 0 || upCell.y > sizeY - 1;
        if (!indexOutOfBounds) result.Add(upCell);

        indexOutOfBounds = downCell.x < 0 || downCell.x > sizeX - 1 || downCell.y < 0 || downCell.y > sizeY - 1;
        if (!indexOutOfBounds) result.Add(downCell);

        return result.ToArray();
    }
}