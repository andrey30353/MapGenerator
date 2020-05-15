
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Generator : MonoBehaviour
{
    public Vector2Int MapSize = new Vector2Int(10, 10);

    public int TileSize = 10;

    public Transform RotationVariantsContainer;

    public Tile FirstTile;

    public List<Tile> TilePrefabs;   

    private Tile[,] spawnedTiles;

    public static Generator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    private void Start()
    {
        spawnedTiles = new Tile[MapSize.x, MapSize.y];
        CreateAllRotationVariants();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StopAllCoroutines();

            Clear();
            
            StartCoroutine(Generate());
        }
    }

    private void CreateAllRotationVariants()
    {
        var countNewPrefabs = TilePrefabs.Count(tile => tile.RotationVariants == RotationVariants.Two)
            + TilePrefabs.Count(tile => tile.RotationVariants == RotationVariants.Four) * 3;

        var newTilePrefabs = new List<Tile>(countNewPrefabs);
        var count = 0;
        foreach (var tile in TilePrefabs)
        {
            if (tile.RotationVariants == RotationVariants.Two)
            {
                tile.Weight =tile.Weight /2;
                if (tile.Weight < 1)
                    tile.Weight = 1;

                var position = tile.transform.position + Vector3.back * TileSize * 1.5f;
                var newTilePrefab = Instantiate<Tile>(tile, position, Quaternion.identity, RotationVariantsContainer);
                newTilePrefab.name = tile.name + "_180";
                newTilePrefab.Rotate(RotationType._180);
                newTilePrefabs.Add(newTilePrefab);
                count++;
            }

            if (tile.RotationVariants == RotationVariants.Four)
            {
                tile.Weight = tile.Weight / 4;
                if (tile.Weight < 1)
                    tile.Weight = 1;

                for (int i = 1; i < 4; i++)
                {
                    var position = tile.transform.position + Vector3.back * TileSize * 1.5f * i;
                    //var newPosition = new Vector3(
                    //    position.x + TileSize * 1.2f * count,
                    //    position.y,
                    //    position.z - TileSize * 1.2f * i);
                    var newTilePrefab = Instantiate<Tile>(tile, position, Quaternion.identity, RotationVariantsContainer);
                    newTilePrefab.name = tile.name + (i * 90).ToString();
                    newTilePrefab.Rotate((RotationType)i);
                    newTilePrefabs.Add(newTilePrefab);
                }
                count++;
            }

            //foreach (var tile in TilePrefabs.Where(t => t.RotationVariants == RotationVariants.Four))
            //{

            //}
        }
        TilePrefabs.AddRange(newTilePrefabs);
    }

    private IEnumerator Generate()
    {
        // todo
        if (FirstTile != null)
        {
            var firstTile = Instantiate(FirstTile, Vector3.zero, Quaternion.identity, transform);
            spawnedTiles[0, 0] = firstTile;
        }

        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                // todo remove
                if (FirstTile != null && x == 0 && y == 0)
                    continue;

                var colorsInfo = GetColorsInfo(x, y);
                var availableTiles = GetTilesWithColors(colorsInfo);
                Debug.Log($"[{x},{y}] availableTiles.Count {availableTiles.Count}");
                if (availableTiles.Count == 0)
                    continue;

                var tilePrefab = GetRandomTile(availableTiles);              
                var newTile = Instantiate<Tile>(tilePrefab, transform);
                newTile.transform.position = new Vector3(x * TileSize, 0, y * TileSize);
                newTile.Spawned = true;
                // todo скрипт Tile вроде бы не нужен            
                spawnedTiles[x, y] = newTile;

                yield return new WaitForSeconds(0.1f);
            }
        }
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

    private List<Tile> GetTilesWithColors(ColorsData colorInfo)
    {
        IEnumerable<Tile> result = TilePrefabs;
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

    private List<Tile> GetTilesWithColors(Color color, Direction direction)
    {
        switch (direction)
        {
            case Direction.Right:
                return TilePrefabs.Where(t => t.RightColor0 == color).ToList();

            case Direction.Left:
                return TilePrefabs.Where(t => t.LeftColor0 == color).ToList();

            case Direction.Forward:
                return TilePrefabs.Where(t => t.ForwardColor0 == color).ToList();

            case Direction.Back:
                return TilePrefabs.Where(t => t.BackColor0 == color).ToList();

            default:
                throw new System.ArgumentException($"Unknown direction = {direction}");
        }

    }




    private void Clear()
    {
        foreach (var item in spawnedTiles)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
    }   

    private Tile GetRandomTile(List<Tile> tiles)
    {
        if (tiles.Count == 0)
            throw new System.ArgumentException($"{nameof(tiles)}.Count = 0");

        //return tiles[Random.Range(0, tiles.Count)];

        // c учетом веса
        var weightSum = 0;
        foreach (var tile in tiles)
        {
            weightSum += tile.Weight;
        }

        var randomValue = Random.Range(0, weightSum+1);

        var sum = 0f;
        foreach (var tile in tiles)
        {
            sum += tile.Weight;
            if(randomValue < sum)
            {
                return tile;
            }
        }

        var lastTile = tiles[tiles.Count - 1];
        return lastTile;
    }
}
