using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GeneratorByTexture : MonoBehaviour
{
    [SerializeField] private Texture2D _texture;
  
    public int TileSize = 10;

    [Header("Not set = -1")]
    [SerializeField] private int seed;
    [SerializeField] private bool useWeight = true;

    public Transform RotationVariantsContainer;

    [Range(0f , 1f)]
    [SerializeField] private float spawnDelay = 0.1f;

    [SerializeField] private Color[] _colorTilePriority;

    public List<Tile> TilePrefabs;

    private Vector2Int MapSize;//= new Vector2Int(10, 10);

    private Tile[,] spawnedTiles;
      
    // Start is called before the first frame update
    private void Start()
    {
        MapSize = new Vector2Int(_texture.width, _texture.height);      

        spawnedTiles = new Tile[MapSize.x, MapSize.y];

        Generator.CreateAllRotationVariants(TilePrefabs, TileSize, RotationVariantsContainer);      
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StopAllCoroutines();

            Clear();

            if (seed != -1)
                Random.InitState(seed);

            //StartCoroutine(AccurateGenerate());
            StartCoroutine(Generate());
            // AccurateGeneration();
        }
    }

    private IEnumerator AccurateGenerate()
    {       
        for (int x = 0; x < MapSize.x; x++)
        {
           for (int y = 0; y < MapSize.y; y++)
            {              
                var pixelColor = _texture.GetPixel(x, y);

                // Debug.Log(pixelColor);

                var availableTilesByColor = GetTilesByColor(pixelColor);

                var availableTiles = AvailableTilesHere(x, y, availableTilesByColor);
                // Debug.Log($"[{x},{y}] availableTiles.Count {availableTiles.Count}");
                if (availableTiles.Count == 0)
                    continue;

                var tilePrefab = GetRandomTile(availableTiles);
                var newTile = Instantiate<Tile>(tilePrefab, transform);
                newTile.transform.position = new Vector3(x * TileSize, 0, y * TileSize);
                newTile.Spawned = true;
                // todo скрипт Tile вроде бы не нужен            
                spawnedTiles[x, y] = newTile;

                if (spawnDelay == 0)
                    yield return new WaitForEndOfFrame();
                else
                    yield return new WaitForSeconds(spawnDelay);
            }
        }
      
    }
   

    private IEnumerator Generate()
    {
        yield return AccurateGenerate();

        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                if (spawnedTiles[x, y] != null)
                    continue;

                var availableTiles = AvailableTilesHere(x, y, TilePrefabs);
                // Debug.Log($"[{x},{y}] availableTiles.Count {availableTiles.Count}");
                if (availableTiles.Count == 0)
                    continue;

                var tilePrefab = GetRandomTile(availableTiles);
                var newTile = Instantiate<Tile>(tilePrefab, transform);
                newTile.transform.position = new Vector3(x * TileSize, 0, y * TileSize);
                newTile.Spawned = true;
                // todo скрипт Tile вроде бы не нужен            
                spawnedTiles[x, y] = newTile;

                if (spawnDelay == 0)
                    yield return new WaitForEndOfFrame();
                else
                    yield return new WaitForSeconds(spawnDelay);
            }
        }
    }

    private List<Tile> GetTilesByColor(Color color)
    {
        var result = TilePrefabs.Where(t => t.ColorsData.BaseColor == color).ToList();       
        return result;
    }

    private List<Tile> AvailableTilesHere(int x, int y, List<Tile> variants)
    {
        var colorsInfo = GetColorsInfo(x, y);
        var availableTiles = GetTilesWithColors(colorsInfo, variants);
        return availableTiles;
    }

    private ColorsData GetColorsInfo(int x, int y)
    {
        // определяем какие цвета должны быть у тайла
        // смотрим вокруг себя во всех направлениях
        // цвет сзади тайла должен быть равен цвету спереди у сзади стоящего тайла

        var result = new ColorsData();
        if (y - 1 >= 0 && spawnedTiles[x, y - 1] != null)
        {
            result.BackColor0 = spawnedTiles[x, y - 1].ForwardColor0;
            result.BackColor1 = spawnedTiles[x, y - 1].ForwardColor1;
        }

        if (y + 1 < MapSize.y && spawnedTiles[x, y + 1] != null)
        {
            result.ForwardColor0 = spawnedTiles[x, y + 1].BackColor0;
            result.ForwardColor1 = spawnedTiles[x, y + 1].BackColor1;
        }

        if (x - 1 >= 0 && spawnedTiles[x - 1, y] != null)
        {
            result.LeftColor0 = spawnedTiles[x - 1, y].RightColor0;
            result.LeftColor1 = spawnedTiles[x - 1, y].RightColor1;
        }

        if (x + 1 < MapSize.x && spawnedTiles[x + 1, y] != null)
        {
            result.RightColor0 = spawnedTiles[x + 1, y].LeftColor0;
            result.RightColor1 = spawnedTiles[x + 1, y].LeftColor1;
        }

        return result;
    }

    private List<Tile> GetTilesWithColors(ColorsData colorInfo, List<Tile> variants)
    {
        IEnumerable<Tile> result = variants;
        if (colorInfo.RightColor0 != Color.clear)
        {
            result = result.Where(t => t.RightColor0 == colorInfo.RightColor0 && t.RightColor1 == colorInfo.RightColor1);
        }

        if (colorInfo.LeftColor0 != Color.clear)
        {
            result = result.Where(t => t.LeftColor0 == colorInfo.LeftColor0 && t.LeftColor1 == colorInfo.LeftColor1);
        }

        if (colorInfo.ForwardColor0 != Color.clear)
        {
            result = result.Where(t => t.ForwardColor0 == colorInfo.ForwardColor0 && t.ForwardColor1 == colorInfo.ForwardColor1);
        }

        if (colorInfo.BackColor0 != Color.clear)
        {
            result = result.Where(t => t.BackColor0 == colorInfo.BackColor0 && t.BackColor1 == colorInfo.BackColor1);
        }

        return result.ToList();
    }

   

    private void Clear()
    {
        foreach (var item in spawnedTiles)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        spawnedTiles = new Tile[MapSize.x, MapSize.y];
    }

    private Tile GetRandomTile(List<Tile> tiles)
    {
        if (tiles.Count == 0)
            throw new System.ArgumentException($"{nameof(tiles)}.Count = 0");

        // c учетом веса
        if (useWeight)
        {
            // c учетом веса
            var weightSum = 0;
            foreach (var tile in tiles)
            {
                weightSum += tile.Weight;
            }

            var randomValue = Random.Range(0, weightSum + 1);

            var sum = 0f;
            foreach (var tile in tiles)
            {
                sum += tile.Weight;
                if (randomValue < sum)
                {
                    return tile;
                }
            }

            var lastTile = tiles[tiles.Count - 1];
            return lastTile;
        }        
        else
        {
            return tiles[UnityEngine.Random.Range(0, tiles.Count)];
        }
    }
}
