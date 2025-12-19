using UnityEngine;
using System.Threading;

public class TextureScale
{
    public static void Bilinear(Texture2D tex, int newWidth, int newHeight)
    {
        Texture2D newTex = new Texture2D(newWidth, newHeight, tex.format, false);
        float ratioX = 1.0f / ((float)newWidth / (tex.width - 1));
        float ratioY = 1.0f / ((float)newHeight / (tex.height - 1));

        for (int y = 0; y < newHeight; y++)
        {
            int yy = (int)Mathf.Floor(y * ratioY);
            int y1 = Mathf.Min(yy + 1, tex.height - 1);
            float yLerp = y * ratioY - yy;

            for (int x = 0; x < newWidth; x++)
            {
                int xx = (int)Mathf.Floor(x * ratioX);
                int x1 = Mathf.Min(xx + 1, tex.width - 1);
                float xLerp = x * ratioX - xx;

                Color bl = tex.GetPixel(xx, yy);
                Color br = tex.GetPixel(x1, yy);
                Color tl = tex.GetPixel(xx, y1);
                Color tr = tex.GetPixel(x1, y1);

                Color top = Color.Lerp(tl, tr, xLerp);
                Color bottom = Color.Lerp(bl, br, xLerp);
                newTex.SetPixel(x, y, Color.Lerp(bottom, top, yLerp));
            }
        }
        newTex.Apply();

        tex.Reinitialize(newWidth, newHeight);
        tex.SetPixels(newTex.GetPixels());
        tex.Apply();
    }
}
