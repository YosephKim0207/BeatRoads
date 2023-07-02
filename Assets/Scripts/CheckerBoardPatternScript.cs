using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckerBoardPatternScript : MonoBehaviour
{

    public Texture2D mainTexture;
    public int mainTextureWidth;
    public int mainTextureHeight;

    // Start is called before the first frame update
    void Start()
    {
        SetMainTexture();
        CreatePattern();
    }

    void SetMainTexture()
    {
        mainTexture = new Texture2D(mainTextureWidth, mainTextureHeight);
    }

    void CreatePattern()
    {
        for(int i = 0; i < mainTextureHeight; i++)
        {
            for(int j = 0; j < mainTextureWidth; j++)
            {
                if((i+j) % 2 == 1)
                {
                    mainTexture.SetPixel(i, j, Color.cyan);
                }
                else
                {
                    mainTexture.SetPixel(i, j, Color.magenta);

                }
            }
        }

        mainTexture.Apply();

        GetComponent<Renderer>().material.mainTexture = mainTexture;
        mainTexture.wrapMode = TextureWrapMode.Clamp;
        mainTexture.filterMode = FilterMode.Point;
    }
}
