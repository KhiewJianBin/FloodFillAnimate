using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum FloodfillAlgo
{
    Recursion,
    DFSFloodFill,
    BFSFloodFill,
    SpanFloodFill,
    Test1
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

    Color32[] buffer;
    void Awake()
    {
        RT = FloodFillImage.GetComponent<RectTransform>();
    }

    void Update()
    {
        var mousePos = Input.mousePosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(RT, mousePos, null, out Vector2 localPos);
        Vector2 imageSize = RT.sizeDelta;
        Vector2 normalizedimagePos = new Vector2((localPos.x + imageSize.x * 0.5f) / imageSize.x, (localPos.y + imageSize.y * 0.5f) / imageSize.y);

        bool isMouseWithin = normalizedimagePos.x >= 0 && normalizedimagePos.y >= 0;

        if (isMouseWithin && Input.GetMouseButtonDown(0))
        {
            var texture = FloodFillImage.sprite.texture;
            buffer = texture.GetPixels32();
            int idx = (int)(normalizedimagePos.x * texture.width);
            int idy = (int)(normalizedimagePos.y * texture.height);

            if (fillCoroutine != null) StopCoroutine(fillCoroutine);
            {
                fillCoroutine = StartCoroutine(StartFloodFill(buffer, idx, idy, texture.width, texture.height));
            }
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
            case FloodfillAlgo.SpanFloodFill:
                yield return SpanFloodFill(idx, idy, buffer, xSize, ySize, DifferenceThreshold, ColorToFill);
                break;
            case FloodfillAlgo.Test1:
                bool[,] array = new bool[xSize, ySize];
                for (int i = 0; i < xSize; i++)
                {
                    for (int j = 0; j < ySize; j++)
                    {
                        var index2 = j + i * xSize;
                        array[i, j] = !buffer[index2].IsEqualTo(Color.white);
                    }
                }
                yield return MyFill(array, idx, idy);

                bool[] b = array.Cast<bool>().ToArray();
                for (int i = 0; i < b.Length; i++)
                {
                    buffer[i] = b[i] ? ColorToFill : Color.white;
                }
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

    IEnumerator SpanFloodFill(int idx, int idy, Color32[] buffer, int xSize, int ySize,
        float threshold, Color32 colorToFill)
    {
        isLogicPaused = true;

        //1. Set first node and Target Color
        Stack<(int x, int y)> cellsToCheck = new();
        cellsToCheck.Push((idx, idy));
        var index = idx + idy * xSize;
        Color targetColor = buffer[index];

        while (cellsToCheck.Count > 0)
        {
            //2. Pop from stack
            var currentCell = cellsToCheck.Pop();

            //3. Color the current cell itself and walk left until hit a wall
            bool isVisited = default;
            bool isColorSimilar = default;
            bool outOfBounds = default;
            var spanMin = currentCell.x;
            index = spanMin + currentCell.y * xSize;
            do
            {
                buffer[index] = colorToFill;
                UpdateImageTexture(FloodFillImage, buffer);
                isLogicPaused = true; yield return new WaitUntil(UnpauseLogic);

                var newxMin = spanMin - 1;
                outOfBounds = newxMin < 0;

                //Early Out
                if (outOfBounds) break;

                spanMin = newxMin;
                index = spanMin + currentCell.y * xSize;
                Color32 currentColor = buffer[index];
                isVisited = ColorExtension.IsEqualTo(currentColor, colorToFill);
                isColorSimilar = CompareColor(currentColor, targetColor) <= threshold;

            } while (!outOfBounds && !isVisited && isColorSimilar);

            //4. Color the cell to the right until hit a wall
            int spanMax = currentCell.x;

            outOfBounds = spanMax + 1 > xSize - 1;
            if (!outOfBounds)
            {
                spanMax = spanMax + 1;
                index = spanMax + currentCell.y * xSize;
                Color32 currentColor = buffer[index];
                isVisited = ColorExtension.IsEqualTo(currentColor, colorToFill);
                isColorSimilar = CompareColor(currentColor, targetColor) <= threshold;
            }

            while (!outOfBounds && !isVisited && isColorSimilar)
            {
                buffer[index] = colorToFill;
                UpdateImageTexture(FloodFillImage, buffer);
                isLogicPaused = true; yield return new WaitUntil(UnpauseLogic);

                var newxMax = spanMax + 1;
                outOfBounds = newxMax > xSize - 1;

                //Early Out
                if (outOfBounds) break;

                spanMax = newxMax;
                index = spanMax + currentCell.y * xSize;
                Color32 currentColor = buffer[index];
                isVisited = ColorExtension.IsEqualTo(currentColor, colorToFill);
                isColorSimilar = CompareColor(currentColor, targetColor) <= threshold;
            }

            //5. Check Up of "Span" to add to stack
            var nextY = currentCell.y + 1;
            outOfBounds = nextY > ySize - 1;
            //Early Out
            if (!outOfBounds)
            {
                bool span_added = false;
                for (int x = spanMin; x <= spanMax; x++)
                {
                    index = x + nextY * xSize;
                    Color32 currentColor = buffer[index];
                    isVisited = ColorExtension.IsEqualTo(currentColor, colorToFill);
                    isColorSimilar = CompareColor(currentColor, targetColor) <= threshold;

                    if (isVisited || !isColorSimilar)
                    {
                        span_added = false;
                    }
                    else if (!span_added)
                    {
                        cellsToCheck.Push((x, nextY));
                        span_added = true;
                    }
                }
            }

            //6. Check Down of "Span" to add to stack
            nextY = currentCell.y - 1;
            outOfBounds = nextY < 0;
            //Early Out
            if (!outOfBounds)
            {
                bool span_added = false;
                for (int x = spanMin; x <= spanMax; x++)
                {
                    index = x + nextY * xSize;
                    Color32 currentColor = buffer[index];
                    isVisited = ColorExtension.IsEqualTo(currentColor, colorToFill);
                    isColorSimilar = CompareColor(currentColor, targetColor) <= threshold;

                    if (isVisited || !isColorSimilar)
                    {
                        span_added = false;
                    }
                    else if (!span_added)
                    {
                        cellsToCheck.Push((x, nextY));
                        span_added = true;
                    }
                }
            }
        }

        //4. Continue loop until no more in stack - cellsToCheck
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



    IEnumerator MyFill(bool[,] array, int x, int y)
    {
        if (!array[y, x]) yield return _MyFill(array, x, y, array.GetLength(1), array.GetLength(0));
    }

    IEnumerator _MyFill(bool[,] array, int x, int y, int width, int height)
    {
        // at this point, we know array[y,x] is clear, and we want to move as far as possible to the upper-left. moving
        // up is much more important than moving left, so we could try to make this smarter by sometimes moving to
        // the right if doing so would allow us to move further up, but it doesn't seem worth the complexity
        while (true)
        {
            int ox = x, oy = y;
            while (y != 0 && !array[y - 1, x]) y--;
            while (x != 0 && !array[y, x - 1]) x--;
            if (x == ox && y == oy) break;
        }
        yield return MyFillCore(array, x, y, width, height);
    }

    IEnumerator MyFillCore(bool[,] array, int x, int y, int width, int height)
    {
        // at this point, we know that array[y,x] is clear, and array[y-1,x] and array[y,x-1] are set.
        // we'll begin scanning down and to the right, attempting to fill an entire rectangular block
        int lastRowLength = 0; // the number of cells that were clear in the last row we scanned
        do
        {
            int rowLength = 0, sx = x; // keep track of how long this row is. sx is the starting x for the main scan below
                                       // now we want to handle a case like |***|, where we fill 3 cells in the first row and then after we move to
                                       // the second row we find the first  | **| cell is filled, ending our rectangular scan. rather than handling
                                       // this via the recursion below, we'll increase the starting value of 'x' and reduce the last row length to
                                       // match. then we'll continue trying to set the narrower rectangular block
            if (lastRowLength != 0 && array[y, x]) // if this is not the first row and the leftmost cell is filled...
            {
                do
                {
                    if (--lastRowLength == 0) yield break; // shorten the row. if it's full, we're done
                } while (array[y, ++x]); // otherwise, update the starting point of the main scan to match
                sx = x;
            }
            // we also want to handle the opposite case, | **|, where we begin scanning a 2-wide rectangular block and
            // then find on the next row that it has     |***| gotten wider on the left. again, we could handle this
            // with recursion but we'd prefer to adjust x and lastRowLength instead
            else
            {
                for (; x != 0 && !array[y, x - 1]; rowLength++, lastRowLength++)
                {
                    array[y, --x] = true; // to avoid scanning the cells twice, we'll fill them and update rowLength here
                                          // if there's something above the new starting point, handle that recursively. this deals with cases
                                          // like |* **| when we begin filling from (2,0), move down to (2,1), and then move left to (0,1).
                                          // the  |****| main scan assumes the portion of the previous row from x to x+lastRowLength has already
                                          // been filled. adjusting x and lastRowLength breaks that assumption in this case, so we must fix it

                    bool[] b = array.Cast<bool>().ToArray();
                    for (int i = 0; i < b.Length; i++)
                    {
                        buffer[i] = b[i] ? ColorToFill : Color.white;
                    }
                    UpdateImageTexture(FloodFillImage, buffer);
                    isLogicPaused = true; yield return new WaitUntil(UnpauseLogic);

                    if (y != 0 && !array[y - 1, x]) yield return _MyFill(array, x, y - 1, width, height); // use _Fill since there may be more up and left
                }
            }

            // now at this point we can begin to scan the current row in the rectangular block. the span of the previous
            // row from x (inclusive) to x+lastRowLength (exclusive) has already been filled, so we don't need to
            // check it. so scan across to the right in the current row
            for (; sx < width && !array[y, sx]; rowLength++, sx++)
            {
                array[y, sx] = true;

                bool[] b = array.Cast<bool>().ToArray();
                for (int i = 0; i < b.Length; i++)
                {
                    buffer[i] = b[i] ? ColorToFill : Color.white;
                }
                UpdateImageTexture(FloodFillImage, buffer);
                isLogicPaused = true; yield return new WaitUntil(UnpauseLogic);
            }
            // now we've scanned this row. if the block is rectangular, then the previous row has already been scanned,
            // so we don't need to look upwards and we're going to scan the next row in the next iteration so we don't
            // need to look downwards. however, if the block is not rectangular, we may need to look upwards or rightwards
            // for some portion of the row. if this row was shorter than the last row, we may need to look rightwards near
            // the end, as in the case of |*****|, where the first row is 5 cells long and the second row is 3 cells long.
            // we must look to the right  |*** *| of the single cell at the end of the second row, i.e. at (4,1)
            if (rowLength < lastRowLength)
            {
                for (int end = x + lastRowLength; ++sx < end;) // 'end' is the end of the previous row, so scan the current row to
                {                                          // there. any clear cells would have been connected to the previous
                    if (!array[y, sx]) yield return MyFillCore(array, sx, y, width, height); // row. the cells up and left must be set so use FillCore
                }
            }
            // alternately, if this row is longer than the previous row, as in the case |*** *| then we must look above
            // the end of the row, i.e at (4,0)                                         |*****|
            else if (rowLength > lastRowLength && y != 0) // if this row is longer and we're not already at the top...
            {
                for (int ux = x + lastRowLength; ++ux < sx;) // sx is the end of the current row
                {
                    if (!array[y - 1, ux]) yield return _MyFill(array, ux, y - 1, width, height); // since there may be clear cells up and left, use _Fill
                }
            }
            lastRowLength = rowLength; // record the new row length
        } while (lastRowLength != 0 && ++y < height); // if we get to a full row or to the bottom, we're done

        yield return null;
    }
}