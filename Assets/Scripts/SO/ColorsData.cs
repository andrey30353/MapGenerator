using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public enum RotationType
{
    _0 = 0,
    _90 = 1,
    _180 = 2,
    _270 = 3
}


[CreateAssetMenu(menuName = "SO/ColorsData", fileName = "ColorsData")]
public class ColorsData : ScriptableObject
{
    //[FormerlySerializedAs("ForwardColor")]
    [Header("Цвета для соединения тайлов")]
    public Color ForwardColor0;
    public Color ForwardColor1;

    [Space]   
    public Color RightColor0;
    public Color RightColor1;

    [Space] 
    public Color BackColor0;
    public Color BackColor1;

    [Space] 
    public Color LeftColor0;
    public Color LeftColor1;
       
    [Header("Цвет для генерации по текстуре")]
    public Color BaseColor;

    public ColorsData Rotate(RotationType rotation)
    {
        var tempForwardColor0 = ForwardColor0;
        var tempForwardColor1 = ForwardColor1;

      //  var result = ScriptableObject.Instantiate(this);

        switch (rotation)
        {
            case RotationType._0:
                return this;

            case RotationType._90:
                return new ColorsData
                {
                    ForwardColor0 = LeftColor0,
                    ForwardColor1 = LeftColor1,

                    LeftColor0 = BackColor1 != Color.clear ? BackColor1 : BackColor0,
                    LeftColor1 = BackColor1 != Color.clear ? BackColor0 : Color.clear,

                    BackColor0 = RightColor0,
                    BackColor1 = RightColor1,

                    RightColor0 = tempForwardColor1 != Color.clear ? tempForwardColor1 : tempForwardColor0,
                    RightColor1 = tempForwardColor1 != Color.clear ? tempForwardColor0 : Color.clear                    
                };       
                
            case RotationType._180:

                var tempRightColor0 = RightColor0;
                var tempRightColor1 = RightColor1;

                return new ColorsData
                {
                    ForwardColor0 = BackColor1 != Color.clear ? BackColor1 : BackColor0,
                    ForwardColor1 = BackColor1 != Color.clear ? BackColor0 : Color.clear,

                    RightColor0 = LeftColor1 != Color.clear ? LeftColor1 : LeftColor0,
                    RightColor1 = LeftColor1 != Color.clear ? LeftColor0 : Color.clear,                                       

                    BackColor0 = tempForwardColor1 != Color.clear ? tempForwardColor1 : tempForwardColor0,
                    BackColor1 = tempForwardColor1 != Color.clear ? tempForwardColor0 : Color.clear,

                    LeftColor0 = tempRightColor1 != Color.clear ? tempRightColor1 : tempRightColor0,
                    LeftColor1 = tempRightColor1 != Color.clear ? tempRightColor0 : Color.clear
                };
               
            case RotationType._270:
             
                return new ColorsData
                {
                    ForwardColor0 = RightColor1 != Color.clear ? RightColor1 : RightColor0,
                    ForwardColor1 = RightColor1 != Color.clear ? RightColor0 : Color.clear,

                    RightColor0 = BackColor0,
                    RightColor1 = BackColor1,

                    BackColor0 = LeftColor1 != Color.clear ? LeftColor1 : LeftColor0,
                    BackColor1 = LeftColor1 != Color.clear ? LeftColor0 : Color.clear,

                    LeftColor0 = tempForwardColor0,
                    LeftColor1 = tempForwardColor1
                };              

            default:
                throw new ArgumentException("Unknown rotation");
        }
    }

    [ContextMenu("Цвета в лог")]
    private void ColorsToLog()
    {
        Debug.Log($"Forward = {ForwardColor0}");
        Debug.Log($"Right = {RightColor0}");
        Debug.Log($"Back = {BackColor0}");
        Debug.Log($"Left = {LeftColor0}");  
    }

    [ContextMenu("Определить основной цвет")]
    private void DefineBaseColor()
    {
        var colors = new List<Color>()
        {
            ForwardColor0, RightColor0, BackColor0, LeftColor0
        };

        var colorCounts = new Dictionary<Color, int>();
        foreach (var color in colors)
        {
            if (colorCounts.ContainsKey(color))
                colorCounts[color]++;
            else
                colorCounts[color] = 1;

        }
        var max = colorCounts.Max(t => t.Value);
        // todo
        var baseColor = colorCounts.FirstOrDefault(t => t.Value == max).Key;

        BaseColor = baseColor;//(ForwardColor0 + RightColor0 + BackColor0 + LeftColor0) / 4;        
    }

    private void OnValidate()
    {
        if (RightColor0.a == 0)
            RightColor0.a = 1;
        if (RightColor1 != Color.clear)
            RightColor1.a = 1;

        if (LeftColor0.a == 0)
            LeftColor0.a = 1;
        if (LeftColor1 != Color.clear)
            LeftColor1.a = 1;

        if (ForwardColor0.a == 0)
            ForwardColor0.a = 1;
        if (ForwardColor1 != Color.clear)
            ForwardColor1.a = 1;

        if (BackColor0.a == 0)
            BackColor0.a = 1;
        if (BackColor1 != Color.clear)
            BackColor1.a = 1;
    }
}

