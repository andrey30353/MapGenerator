using System;
using UnityEngine;

public enum Direction
{
    Right,
    Left, 
    Forward,
    Back
}

public enum RotationVariants
{
    One,
    Two,
    Four,    
}

public class Tile : MonoBehaviour
{ 
    // todo
    public int TileSize;

    [Range(0.25f, 1)]
    public float Height;

    [Range(1, 100)]
    public int Weight = 50;
       
    public ColorsData ColorsData;

    public RotationVariants RotationVariants;
      
    public TilePaletteData Palette;

    public bool Spawned = false;

    public bool Predefined = false;

    public Color RightColor0 => ColorsData?.RightColor0 ?? Color.clear;
    public Color RightColor1 => ColorsData?.RightColor1 ?? Color.clear;
    public Color LeftColor0 => ColorsData?.LeftColor0 ?? Color.clear;
    public Color LeftColor1 => ColorsData?.LeftColor1 ?? Color.clear;
    public Color ForwardColor0 => ColorsData?.ForwardColor0 ?? Color.clear;
    public Color ForwardColor1 => ColorsData?.ForwardColor1 ?? Color.clear;
    public Color BackColor0 => ColorsData?.BackColor0 ?? Color.clear;
    public Color BackColor1 => ColorsData?.BackColor1 ?? Color.clear;

    public float LevelHeight => transform.position.y;


    public void OnValidate()
    {
        if (Spawned) return;       
        
        var minRotationVariants = DefineRotationVariants();

        if ((int)RotationVariants < (int)minRotationVariants)
            RotationVariants = minRotationVariants;
    }

    public void Rotate(RotationType rotation)
    {
        transform.Rotate(Vector3.up, ((int)rotation) * 90);
        ColorsData = ColorsData.Rotate(rotation);
    }

    [ContextMenu("Центрировать")]
    private void MoveTileToCenterParentTransform()
    {
        transform.GetChild(0).Translate(new Vector3(-TileSize * 0.5f, 0, -TileSize * 0.5f));
    }  

    [ContextMenu("Назвать")]
    public void SetName()
    {
        name = transform.GetChild(0).name;
    }

    [ContextMenu("Сохранить в палитру")]
    private void SaveInPalette()
    {
        if (Palette == null)
        {
            Debug.LogError("Palette is null");
            return;
        }

        if (RightColor0.a != 0 && !Palette.Colors.Contains(RightColor0))
            Palette.Colors.Add(RightColor0);
        if (RightColor1.a != 0 && !Palette.Colors.Contains(RightColor1))
            Palette.Colors.Add(RightColor1);

        if (LeftColor0.a != 0 && !Palette.Colors.Contains(LeftColor0))
            Palette.Colors.Add(LeftColor0);
        if (LeftColor0.a != 1 && !Palette.Colors.Contains(LeftColor1))
            Palette.Colors.Add(LeftColor1);

        if (ForwardColor0.a != 0 && !Palette.Colors.Contains(ForwardColor0))
            Palette.Colors.Add(ForwardColor0);
        if (ForwardColor1.a != 1 && !Palette.Colors.Contains(ForwardColor1))
            Palette.Colors.Add(ForwardColor1);

        if (BackColor0.a != 0 && !Palette.Colors.Contains(BackColor0))
            Palette.Colors.Add(BackColor0);
        if (BackColor1.a != 0 && !Palette.Colors.Contains(BackColor1))
            Palette.Colors.Add(BackColor1);
    }

    [ContextMenu("Определить варианты поворота")]
    private RotationVariants DefineRotationVariants()
    {
        if (RightColor0 == LeftColor0 
            && RightColor0 == ForwardColor0 
            && RightColor0 == BackColor0)
            return RotationVariants.One;
        
        if (RightColor0 == LeftColor0 && ForwardColor0 == BackColor0)
            return RotationVariants.Two;

        return RotationVariants.Four;
    }

    private void OnDrawGizmos()
    {
        if (Spawned) return;

        var positionModifier = Vector3.up * TileSize * 2f;
        var centerPosition = transform.position + positionModifier;
            
        if (ColorsData != null)
        {
            DrawGizmo(centerPosition);
        }
        else
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(centerPosition, TileSize*0.3f);
        }
    }

    public void DrawGizmo(Vector3 centerPosition)
    {
        Gizmos.color = RightColor0;
        Gizmos.DrawCube(centerPosition + Vector3.right * 2f, Vector3.one);
        Gizmos.color = RightColor1;
        Gizmos.DrawCube(centerPosition + Vector3.right * 2f + Vector3.forward, Vector3.one);
      
        Gizmos.color = LeftColor0;
        Gizmos.DrawCube(centerPosition + Vector3.left * 2f, Vector3.one);
        Gizmos.color = LeftColor1;
        Gizmos.DrawCube(centerPosition + Vector3.left * 2f + Vector3.forward, Vector3.one);
     
        Gizmos.color = ForwardColor0;
        Gizmos.DrawCube(centerPosition + Vector3.forward * 2f, Vector3.one);
        Gizmos.color = ForwardColor1;
        Gizmos.DrawCube(centerPosition + Vector3.forward * 2f + Vector3.right, Vector3.one);
              
        Gizmos.color = BackColor0;
        Gizmos.DrawCube(centerPosition + Vector3.back * 2f, Vector3.one);
        Gizmos.color = BackColor1;
        Gizmos.DrawCube(centerPosition + Vector3.back * 2f + Vector3.right, Vector3.one);
       
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(centerPosition, 0.5f);
    }
}
