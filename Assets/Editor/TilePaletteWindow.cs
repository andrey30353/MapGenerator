using UnityEngine;
using UnityEditor;

public class TilePaletteWindow : EditorWindow
{
    [MenuItem("Helper/TilePalette")]
    public static void ShowPaletteWindow()
    {
        Object[] selection = Selection.GetFiltered(typeof(TilePaletteData), SelectionMode.Assets);
        if (selection.Length > 0)
        {
            var palette = selection[0] as TilePaletteData;
            if (palette != null)
            {
                var window = GetWindow<TilePaletteWindow>();
                window.palette = palette;
            }
        }
    }
  
    private TilePaletteData palette; 

    public void OnGUI()
    {
        if (palette == null)
            return;

        Refresh();
    }

    private void Refresh()
    {
        var x = 0;
        foreach (var color in palette.Colors)
        {            
            EditorGUI.DrawRect(new Rect(10, x , 200, 20), color);
            x += 20;
        }
    }
}