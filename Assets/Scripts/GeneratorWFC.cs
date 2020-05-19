using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    private int placedTilesCount = 0;
    private int needPlaceCount;

    private Tile[] predefinedTiles;

    // Start is called before the first frame update
    private void Start()
    {
        spawnedTiles = new Tile[MapSize.x, MapSize.y];

        Generator.CreateAllRotationVariants(TilePrefabs, TileSize, RotationVariantsContainer);

        predefinedTiles = GetComponentsInChildren<Tile>(false);
        CorrectColorsDataForPredefinedTiles(predefinedTiles);
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StopAllCoroutines();

            Clear();
               
            StartCoroutine(Generate());
            // Generate();
        }
    }

    private IEnumerator Generate()
    {
        if (seed != -1)
            UnityEngine.Random.InitState(seed);

        ProcessPredefinedTiles();

        if (placedTilesCount == 0)
        {
            var centerTile = GetRandomTile(TilePrefabs);
            var centerPosition = new Vector2Int(MapSize.x / 2, MapSize.y / 2);
            possibleTiles[centerPosition.x, centerPosition.y] = new List<Tile> { centerTile };

            PlaceTile(centerPosition.x, centerPosition.y, centerTile);            
            AddNeighborsToQueue(centerPosition);
        }       

        yield return GenerateAllPossibleTiles();      
    }

    

    private IEnumerator GenerateAllPossibleTiles()
    {
        var maxIterations = MapSize.x * MapSize.y;
        var iterations = 0;
     
        while (placedTilesCount < needPlaceCount ||  iterations < maxIterations)
        {
            // Debug.Log($"placed/need = { placedTilesCount} / {needPlaceCount}");
            var maxInnerIteration = 500;
            var innerIteration = 0;

            while (recalcPossibleTilesQueue.Count > 0 && innerIteration++ < maxInnerIteration)
            {
                var position = recalcPossibleTilesQueue.Dequeue();

                var possibleTilesHere = possibleTiles[position.x, position.y];               
                int countRemoved = possibleTilesHere.RemoveAll(t => !IsTilePossible(position.x, position.y, t));

                // TODO:
                if (possibleTilesHere.Count == 0)
                {
                    // Зашли в тупик
                    Debug.LogError("Не получилось собрать карту");
                    yield break;
                }

                if (possibleTilesHere.Count == 1)
                {
                    // Debug.Log("1 tile");
                    PlaceTile(position.x, position.y, possibleTilesHere[0]);
                }

                if (countRemoved > 0)
                    AddNeighborsToQueue(position);               
            }
            // TODO: del
            // LogSpawnPossibleVariants();
            // LogPossibleVariants();

            if (innerIteration == maxInnerIteration)
                break;

            // координаты с минимальной энтропией
            var minEntropyPosition = DefineMinEntropyPosition(out var minEntropy);
            // Debug.Log("minEntropyPosition  = " + minEntropyPosition);
            
            if (minEntropy == 0)
            {
                Debug.Log($"Generated for {iterations} iterations");
                yield break;//true;
            }

            var tileCollapse = GetRandomTile(possibleTiles[minEntropyPosition.x, minEntropyPosition.y]);
            possibleTiles[minEntropyPosition.x, minEntropyPosition.y] = new List<Tile>() { tileCollapse };

            PlaceTile(minEntropyPosition.x, minEntropyPosition.y, tileCollapse);

            AddNeighborsToQueue(minEntropyPosition);

            iterations++;

            yield return new WaitForSeconds(spawnDelay);
        }
        Debug.Log($"Failed, run out of iterations {iterations}");
        // return false;
    }

    // TODO
    private void ProcessPredefinedTiles()
    {     
        foreach (var tile in predefinedTiles)
        {           
            tile.Predefined = true;

            var positionX = (int)tile.transform.position.x / TileSize;
            var positionY = (int)tile.transform.position.z / TileSize;

            possibleTiles[positionX, positionY] = new List<Tile>() { tile };
            spawnedTiles[positionX, positionY] = tile;
            tile.Spawned = true;            

            placedTilesCount++;
        }

        foreach (var tile in predefinedTiles)
        {
            var positionX = (int)tile.transform.position.x / TileSize;
            var positionY = (int)tile.transform.position.z / TileSize;

            AddNeighborsToQueue(new Vector2Int(positionX, positionY));
        }       
    }

    private void CorrectColorsDataForPredefinedTiles(Tile[] predefinedTiles)
    {
        foreach (var tile in predefinedTiles)
        {
            // Исправить данные о цвете, для повернутых тайлов            
            var angleY = Mathf.RoundToInt(tile.transform.eulerAngles.y);
            if (angleY != 0)
            {
              //  Debug.Log("angleY = " + angleY);
                var rotationType = AngleToRotationType(angleY);
                tile.ColorsData = tile.ColorsData.Rotate(rotationType);
            }
        }
    }

    private RotationType AngleToRotationType(int angleY)
    {
        if (angleY == 0)
            return RotationType._0;

        if (angleY == 90)
            return RotationType._90;

        if (angleY == 180)
            return RotationType._180;

        if (angleY == 270)
            return RotationType._270;

        throw new System.ArgumentException($"{nameof(angleY)} should be 0/90/180/270 but was {angleY}");
    }

    private void PlaceTile(int x, int y, Tile tilePrefab)
    {
        var newTile = Instantiate<Tile>(tilePrefab, transform);
        newTile.transform.position = new Vector3(x * TileSize, 0, y * TileSize);
        newTile.Spawned = true;
        // todo скрипт Tile вроде бы не нужен            
        spawnedTiles[x, y] = newTile;

        placedTilesCount++;
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

    /// <summary>
    /// Определить координаты с минимальной энтропией
    /// </summary>
    private Vector2Int DefineMinEntropyPosition(out float minEntropy)
    {
        minEntropy = 0f;
        Vector2Int min_entropy_coords = Vector2Int.zero;

        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                if (possibleTiles[x, y].Count == 1)
                    continue;

                // Добавляем небольшой шум для небольшого смешивания
                var entropy = CalcShannonEntropy(x, y);
                var entropyPlusNoise = entropy - (Random.value / 1000);

                if (minEntropy == 0 || entropyPlusNoise < minEntropy)
                {
                    minEntropy = entropyPlusNoise;
                    min_entropy_coords = new Vector2Int(x, y);
                }
            }
        }
        return min_entropy_coords;
    }

    /// <summary>
    /// Вычислить энтропию по формуле Шеннона (чем больше разницы весов - тем меньше энтропия)
    /// </summary>    
    private float CalcShannonEntropy(int x, int y)
    {
        var sumOfWeights = 0;
        var sumOfWeightLogWeights = 0f;
        foreach (var tile in possibleTiles[x, y])
        {
            var weight = tile.Weight;
            sumOfWeights += weight;
            sumOfWeightLogWeights += weight * Mathf.Log(weight);
        }

        var result = Mathf.Log(sumOfWeights) - (sumOfWeightLogWeights / sumOfWeights);
       // Debug.Log($"[{x},{y}] = {possibleTiles[x, y].Count} | {result}");
        return result;
    }

    private void AddNeighborsToQueue(Vector2Int position)
    {
        // не нужно добавлять в очередь, где уже один тайл

        if (position.x + 1 < MapSize.x)
        {
            if (possibleTiles[position.x + 1, position.y].Count != 1)
                recalcPossibleTilesQueue.Enqueue(new Vector2Int(position.x + 1, position.y));
        }

        if (position.x - 1 >= 0)
        {
            if (possibleTiles[position.x - 1, position.y].Count != 1)
                recalcPossibleTilesQueue.Enqueue(new Vector2Int(position.x - 1, position.y));
        }

        if (position.y + 1 < MapSize.y)
        {
            if (possibleTiles[position.x, position.y + 1].Count != 1)
                recalcPossibleTilesQueue.Enqueue(new Vector2Int(position.x, position.y + 1));
        }

        if (position.y - 1 >= 0)
        {
            if (possibleTiles[position.x, position.y - 1].Count != 1)
                recalcPossibleTilesQueue.Enqueue(new Vector2Int(position.x, position.y - 1));
        }
    }


    private void Clear()
    {
        foreach (var item in spawnedTiles)
        {
            if (item != null && item.Predefined == false)
                Destroy(item.gameObject);
        }
        spawnedTiles = new Tile[MapSize.x, MapSize.y];

        PrepareToGenerate();
    }

    private void PrepareToGenerate()
    {
        possibleTiles = new List<Tile>[MapSize.x, MapSize.y];

        needPlaceCount = MapSize.x * MapSize.y;
        placedTilesCount = 0;

        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                possibleTiles[x, y] = new List<Tile>(TilePrefabs);
            }
        }

        recalcPossibleTilesQueue.Clear();
    }

    private Tile GetRandomTile(List<Tile> tiles)
    {
        if (tiles.Count == 0)
            throw new System.ArgumentException($"{nameof(tiles)}.Count = 0");

        // c учетом веса
        if (useWeight)
        {
            var totalSum = tiles.Sum(t => t.Weight);
            var randomValue = UnityEngine.Random.Range(0, totalSum + 1);

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

    #region Методы для логирования

    private void LogPossibleVariants()
    {
        var sb = new StringBuilder();
        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                sb.Append(possibleTiles[x, y].Count.ToString("D2") + " ");
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }

    private void LogSpawnPossibleVariants()
    {
        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                if (possibleTiles[x, y].Count > 5 || possibleTiles[x, y].Count == 1)
                    continue;

                var count = 1;
                foreach (var tile in possibleTiles[x, y])
                {
                    var newTile = Instantiate<Tile>(tile, transform);
                    newTile.transform.position = new Vector3(x * TileSize, count * TileSize, y * TileSize);
                    newTile.Spawned = true;
                    // todo скрипт Tile вроде бы не нужен            
                    spawnedTiles[x, y] = newTile;

                    count++;
                }
            }
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        Generator.DrawGizmosArea(MapSize, TileSize, this.transform.position);
    }
}
