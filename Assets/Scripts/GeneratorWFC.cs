using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GeneratorWFC : MonoBehaviour
{
    public Vector2Int MapSize = new Vector2Int(10, 10);

    public int TileSize = 10;

    [Header("Not set = -1")]
    [SerializeField] private int seed;
    [SerializeField] private bool useWeight = true;

    public Transform RotationVariantsContainer;

    [Range(0.01f, 1f)]
    [SerializeField] private float spawnDelay = 0.1f;

    public List<Tile> TilePrefabs;

    private Tile[,] spawnedTiles;

    // двумерный массив списков (в каждой клеточке - список возможных тайлов)
    private List<Tile>[,] possibleTiles;

    private Queue<Vector2Int> recalcPossibleTilesQueue = new Queue<Vector2Int>();

    // Start is called before the first frame update
    private void Start()
    {
        spawnedTiles = new Tile[MapSize.x, MapSize.y];

        Generator.CreateAllRotationVariants(TilePrefabs, TileSize, RotationVariantsContainer);
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            //StopAllCoroutines();

            Clear();

            if (seed != -1)
                UnityEngine.Random.InitState(seed);

            //StartCoroutine(Generate());
            Generate();
        }
    }

    private void /*IEnumerator*/ Generate()
    {
        possibleTiles = new List<Tile>[MapSize.x, MapSize.y];

        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                possibleTiles[x, y] = new List<Tile>(TilePrefabs);
            }
        }

        var centerTile = GetRandomTile(TilePrefabs);
        var centerPosition = new Vector2Int(MapSize.x / 2, MapSize.y / 2);
        possibleTiles[centerPosition.x, centerPosition.y] = new List<Tile> { centerTile };

        recalcPossibleTilesQueue.Clear();
        AddNeighborsToQueue(centerPosition);

        var success = GenerateAllPossibleTiles();
        /*return*/
        PlaceAllTiles();
    }

    private bool IsTilePossible(int x, int y, Tile tile)
    {
        // тайлы сзади        
        if (y - 1 >= 0 && possibleTiles[x, y - 1].Count != 0)
        {
            // тайл должен подходить хотя бы к одному варианту
            var isPossible = possibleTiles[x, y - 1].Any(t => tile.BackColor0 == t.ForwardColor0 && tile.BackColor1 == t.ForwardColor1);
            if (!isPossible)
                return false;
        }

        if (y + 1 < MapSize.y && possibleTiles[x, y + 1].Count != 0)
        {
            var isPossible = possibleTiles[x, y + 1].Any(t => tile.ForwardColor0 == t.BackColor0 && tile.ForwardColor1 == t.BackColor1);
            if (!isPossible)
                return false;
        }

        if (x - 1 >= 0 && possibleTiles[x - 1, y].Count != 0)
        {
            var isPossible = possibleTiles[x - 1, y].Any(t => tile.LeftColor0 == t.RightColor0 && tile.LeftColor1 == t.RightColor1);
            if (!isPossible)
                return false;
        }

        if (x + 1 < MapSize.x && possibleTiles[x + 1, y].Count != 0)
        {
            var isPossible = possibleTiles[x + 1, y].Any(t => tile.RightColor0 == t.LeftColor0 && tile.RightColor1 == t.LeftColor1);
            if (!isPossible)
                return false;
        }

        return true;
    }

    private bool GenerateAllPossibleTiles()
    {
        var maxIterations = MapSize.x * MapSize.y;
        var iterations = 0;
        int backtracks = 0;

        while (iterations++ < maxIterations)
        {

            var maxInnerIteration = 500;
            var innerIteration = 0;

            while (recalcPossibleTilesQueue.Count > 0 && innerIteration++ < maxInnerIteration)
            {
                var position = recalcPossibleTilesQueue.Dequeue();

                var possibleTilesHere = possibleTiles[position.x, position.y];
                int countRemoved = possibleTilesHere.RemoveAll(t => !IsTilePossible(position.x, position.y, t));

                if (countRemoved > 0)
                    AddNeighborsToQueue(position);          

                if (possibleTilesHere.Count == 0)
                {
                    Debug.LogError("possibleTilesHereNew.Count = 0");
                    // обновляем по новой, мб выйдет другой вариант
                    possibleTilesHere.AddRange(TilePrefabs);
                    if (position.x + 1 < MapSize.x)
                        possibleTiles[position.x + 1, position.y] = new List<Tile>(TilePrefabs);

                    if (position.x - 1 >= 0)
                        possibleTiles[position.x - 1, position.y] = new List<Tile>(TilePrefabs);

                    if (position.y + 1 < MapSize.y)
                        possibleTiles[position.x, position.y + 1] = new List<Tile>(TilePrefabs);

                    if (position.y - 1 >= 0)
                        possibleTiles[position.x, position.y - 1] = new List<Tile>(TilePrefabs);

                    AddNeighborsToQueue(position);
                    backtracks++;
                }
            }

            if (innerIteration == maxInnerIteration)
                break;

            // координаты с максимально возможным количеством тайлов
            var maxTilesCount = possibleTiles[0, 0];
            var maxTileCountPosition = new Vector2Int(0, 0);
            for (int x = 0; x < MapSize.x; x++)
            {
                for (int y = 0; y < MapSize.y; y++)
                {
                    if (possibleTiles[x, y].Count > maxTilesCount.Count)
                    {
                        maxTilesCount = possibleTiles[x, y];
                        maxTileCountPosition = new Vector2Int(x, y);
                    }
                }
            }

            if (maxTilesCount.Count == 1)
            {
                Debug.Log($"Generated for {iterations} iterations, with {backtracks} backtracks");
                return true;
            }

            var tileCollapse = GetRandomTile(maxTilesCount);
            possibleTiles[maxTileCountPosition.x, maxTileCountPosition.y] = new List<Tile>() { tileCollapse };
            AddNeighborsToQueue(maxTileCountPosition);
        }
        Debug.Log($"Failed, run out of iterations with {backtracks} backtracks");
        return false;
    }
    
    private void/*IEnumerator*/ PlaceAllTiles()
    {
        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                // здесь 
                var availableTiles = possibleTiles[x, y];
                // Debug.Log($"[{x},{y}] availableTiles.Count {availableTiles.Count}");

                if (availableTiles.Count == 0)
                    continue;

                var tilePrefab = GetRandomTile(availableTiles);

                var newTile = Instantiate<Tile>(tilePrefab, transform);
                newTile.transform.position = new Vector3(x * TileSize, 0, y * TileSize);
                newTile.Spawned = true;
                // todo скрипт Tile вроде бы не нужен            
                spawnedTiles[x, y] = newTile;

                // yield return new WaitForSeconds(spawnDelay);
            }
        }
    }

    //private bool IsPossibleTileHere(Vector2Int position)
    //{
    //    var requierColors = GetColorsInfo(position.x, position.y);
    //}

    private void AddNeighborsToQueue(Vector2Int position)
    {
        if (position.x + 1 < MapSize.x)
            recalcPossibleTilesQueue.Enqueue(new Vector2Int(position.x + 1, position.y));

        if (position.x - 1 >= 0)
            recalcPossibleTilesQueue.Enqueue(new Vector2Int(position.x - 1, position.y));

        if (position.y + 1 < MapSize.y)
            recalcPossibleTilesQueue.Enqueue(new Vector2Int(position.x, position.y + 1));

        if (position.y - 1 >= 0)
            recalcPossibleTilesQueue.Enqueue(new Vector2Int(position.x, position.y - 1));
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
            var weightSum = 0;
            foreach (var tile in tiles)
            {
                weightSum += tile.Weight;
            }

            var randomValue = UnityEngine.Random.Range(0, weightSum + 1);

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
