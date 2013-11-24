using UnityEngine;
using System.Collections;

[System.Serializable]
public class MultiTexture2D
{
    public Texture2D[] textureArray = new Texture2D[0];
    public float animationFps = 3.0f;

    public Texture2D this[int index]
    {
        get
        {
            return textureArray[index];
        }

        set
        {
            textureArray[index] = value;
        }
    }

    public int Length
    {
        get
        {
            return textureArray.Length;
        }
    }

    public long LongLength
    {
        get
        {
            return textureArray.LongLength;
        }
    }
}