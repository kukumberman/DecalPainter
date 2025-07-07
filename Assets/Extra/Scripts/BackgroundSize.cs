using UnityEngine;

public static class BackgroundSize
{
    public enum Mode
    {
        Cover,
        Contain,
    }

    public static Rect Calculate(Mode mode, Vector2 imageSize, Vector2 containerSize)
    {
        float imgRatio = imageSize.y / imageSize.x;
        float containerRatio = containerSize.y / containerSize.x;

        float drawWidth,
            drawHeight,
            offsetX,
            offsetY;

        bool condition =
            (imgRatio < containerRatio && mode == Mode.Contain)
            || (imgRatio > containerRatio && mode == Mode.Cover);

        if (condition)
        {
            // Full width, scale height
            drawWidth = containerSize.x;
            drawHeight = drawWidth * imgRatio;
            offsetX = 0;
            offsetY = (containerSize.y - drawHeight) / 2;
        }
        else
        {
            // Full height, scale width
            drawHeight = containerSize.y;
            drawWidth = drawHeight / imgRatio;
            offsetY = 0;
            offsetX = (containerSize.x - drawWidth) / 2;
        }

        return new Rect(offsetX, offsetY, drawWidth, drawHeight);
    }
}
