using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/TilePalette", fileName = "TilePalette")]
public class TilePaletteData : ScriptableObject
{
    public List<Color> Colors;
    /*
    /// <summary>
    /// Игнорирует альфа канал
    /// </summary>
    /// <returns></returns>
    public bool ContainColor(Color value)
    {
        foreach (var color in Colors)
        {           
            if (value.r == color.r && value.g == color.g && value.b == color.b)
                return true;           
        }
        return false;
    }*/
}
