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
        /*return*/ PlaceAllTiles();
    }

    private List<Tile> AvailableTilesHere(int x, int y, List<Tile> variants)
    {      
        IEnumerable<Tile> result = variants;
        if (y - 1 >= 0 && possibleTiles[x, y - 1].Count != 0)
        {
            result = result.Where(t => possibleTiles[x, y - 1].Any(v=> t.BackColor0 == v.ForwardColor0 && t.BackColor1 == v.ForwardColor1));            
        }

        if (y + 1 < MapSize.y && possibleTiles[x, y + 1].Count != 0)
        {
            result = result.Where(t => possibleTiles[x, y + 1].Any(v => t.ForwardColor0 == v.BackColor0 && t.ForwardColor1 == v.BackColor1));
        }

        if (x - 1 >= 0 && possibleTiles[x - 1, y].Count != 0)
        {
            result = result.Where(t => possibleTiles[x - 1, y].Any(v => t.LeftColor0 == v.RightColor0 && t.LeftColor1 == v.RightColor1));
        }

        if (x + 1 < MapSize.x && possibleTiles[x + 1, y].Count != 0)
        {
            result = result.Where(t => possibleTiles[x + 1, y].Any(v => t.RightColor0 == v.LeftColor0 && t.RightColor1 == v.LeftColor1));
        } 

        return result.ToList();
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
                //if (position.x == 0 || position.x == MapSize.x - 1
                //    || position.y == 0 || position.y == MapSize.y - 1)
                //{
                //    continue;
                //}

                var possibleTilesHere = possibleTiles[position.x, position.y];
                var possibleTilesHereNew = AvailableTilesHere(position.x, position.y, possibleTilesHere);

                //Debug.Log($"Was {possibleTilesHere.Count }; now {possibleTilesHereNew.Count}");
                // обновляем возможные значения
                possibleTiles[position.x, position.y] = possibleTilesHereNew;

                if (possibleTilesHere.Count != possibleTilesHereNew.Count)
                {
                    AddNeighborsToQueue(position);

                    if (possibleTilesHereNew.Count == 0)
                    {
                        Debug.LogError("possibleTilesHereNew.Count = 0");
                        // обновляем по новой, мб выйдет другой вариант
                        possibleTilesHereNew.AddRange(TilePrefabs);
                        if(position.x + 1 < MapSize.x)
                            possibleTiles[position.x + 1, position.y] = new List<Tile>(TilePrefabs);

                        if (position.x -1  >= 0)
                            possibleTiles[position.x - 1, position.y] = new List<Tile>(TilePrefabs);

                        if (position.y + 1 < MapSize.y)
                            possibleTiles[position.x, position.y + 1] = new List<Tile>(TilePrefabs);

                        if (position.y - 1 >= 0)
                            possibleTiles[position.x, position.y - 1] = new List<Tile>(TilePrefabs);

                        AddNeighborsToQueue(position);
                        backtracks++;
                    }
  

                                   
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
