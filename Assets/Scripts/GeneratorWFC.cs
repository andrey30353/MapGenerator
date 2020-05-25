using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

public class GeneratorWFC : MonoBehaviour
{
    [SerializeField] private Texture2D _texture;

    public Vector2Int MapSize = new Vector2Int(10, 10);

    [SerializeField] private float _heightThreshold;
    [SerializeField] private int _maxHeight;

    public int TileSize = 10;

    [Header("Not set = -1")]
    [SerializeField] private int seed;
    [SerializeField] private bool useWeight = true;

    public Transform RotationVariantsContainer;

    [Range(0f, 1f)]
    [SerializeField] private float spawnDelay = 0f;

    public List<Tile> TilePrefabs;

    [FormerlySerializedAs("AdditionalTilePrefabs")]
    public List<Tile> TransitionsTilePrefabs;

    private Tile[,] spawnedTiles;
    private Tile[,] spawnedTransitionTiles;

    // двумерный массив списков (в каждой клеточке - список возможных тайлов)
    private List<Tile>[,] possibleTiles;
    private List<Tile>[,] possibleTransitionTiles;

    private Queue<Vector2Int> recalcPossibleTilesQueue = new Queue<Vector2Int>();
    private Queue<Vector2Int> recalcPossibleTransitionTilesQueue = new Queue<Vector2Int>();

    private int placedTilesCount = 0;
    private int needPlaceCount;

    private Tile[] predefinedTiles;

    private float _minTileHeight;
    private float _maxTileHeight;

    // карта высот
    private float[,] _heightsMap;

    private void Start()
    {
        if (_texture != null)
            MapSize = new Vector2Int(_texture.width, _texture.height);

        spawnedTiles = new Tile[MapSize.x, MapSize.y];

        //spawnedTransitionTiles = new Tile[MapSize.x, MapSize.y];

        Generator.CreateAllRotationVariants(TilePrefabs, TileSize, RotationVariantsContainer);

        //Generator.CreateAllRotationVariants(TransitionsTilePrefabs, TileSize, RotationVariantsContainer);

        predefinedTiles = GetComponentsInChildren<Tile>(false);
        CorrectColorsDataForPredefinedTiles(predefinedTiles);

        //DefineTileHeights();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StopAllCoroutines();

            Clear();

            StartCoroutine(Generate());
        }
    }

    private IEnumerator Generate()
    {
        if (seed != -1)
            Random.InitState(seed);

        if (_texture != null)
            ProcessTexture();

        ProcessPredefinedTiles();

        if (_texture == null && placedTilesCount == 0)
        {
            var centerTile = GetRandomTile(TilePrefabs);
            var centerPosition = new Vector2Int(MapSize.x / 2, MapSize.y / 2);
            possibleTiles[centerPosition.x, centerPosition.y] = new List<Tile> { centerTile };

            PlaceTile(centerPosition.x, centerPosition.y, centerTile);
            AddNeighborsToQueue(centerPosition);
        }

        yield return GenerateAllPossibleTiles();

        //yield return GenerateAllTranstionPossibleTiles();
    }

    private IEnumerator GenerateAllPossibleTiles()
    {
        var maxIterations = MapSize.x * MapSize.y;
        var iterations = 0;

        while (placedTilesCount < needPlaceCount || iterations++ < maxIterations)
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
                    Debug.LogError($"Не получилось собрать карту ({position.x}:{position.y})");
                    LogPossibleVariants();
                    //break;
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
            //LogPossibleVariants();

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

            var availableTiles = possibleTiles[minEntropyPosition.x, minEntropyPosition.y];
            var tileCollapse = GetRandomTile(availableTiles);
            possibleTiles[minEntropyPosition.x, minEntropyPosition.y] = new List<Tile>() { tileCollapse };

            PlaceTile(minEntropyPosition.x, minEntropyPosition.y, tileCollapse);

            AddNeighborsToQueue(minEntropyPosition);

            if (spawnDelay == 0)
                yield return new WaitForEndOfFrame();
            else
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

    int randomHeight = 0;
    private void PlaceTile(int x, int y, Tile tilePrefab)
    {
        var newTile = Instantiate<Tile>(tilePrefab, transform);

        /*randomHeight = 0;
        if (randomHeight == 0  || Random.value > 0.9f)
        {
            randomHeight = UnityEngine.Random.Range(1, _maxHeight + 1);
        }       
       
        var height = randomHeight * _heightThreshold;
        if (height < tilePrefab.Height)
            height = tilePrefab.Height;
        var positionY = height - tilePrefab.Height;*/
        newTile.transform.position = new Vector3(x * TileSize, /*positionY * TileSize*/0, y * TileSize);

        newTile.Spawned = true;
        // todo скрипт Tile вроде бы не нужен            
        spawnedTiles[x, y] = newTile;

        placedTilesCount++;
    }

    #region by texture

    private void ProcessTexture()
    {
        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                var pixelColor = _texture.GetPixel(x, y);

                if (pixelColor.a == 0)
                    continue;

                // Debug.Log(pixelColor);

                var availableTilesByColor = GetTilesByColor(pixelColor);

                var availableTiles = AvailableTilesHere(x, y, availableTilesByColor);
                // Debug.Log($"[{x},{y}] availableTiles.Count {availableTiles.Count}");

                if (availableTiles.Count == 0)
                {
                    Debug.Log($"Нет тайлов с базовым цветом = {pixelColor}");
                    continue;
                }

                var beforeAvailableCount = possibleTiles[x, y].Count;
                possibleTiles[x, y] = availableTiles;

                if (availableTiles.Count == 1)
                {
                    var tilePrefab = GetRandomTile(availableTiles);
                    var newTile = Instantiate<Tile>(tilePrefab, transform);
                    newTile.transform.position = new Vector3(x * TileSize, 0, y * TileSize);
                    newTile.Spawned = true;
                    // todo скрипт Tile вроде бы не нужен            
                    spawnedTiles[x, y] = newTile;
                }

                if (availableTiles.Count < beforeAvailableCount)
                {
                    AddNeighborsToQueue(new Vector2Int(x, y));
                }
            }
        }

        // LogPossibleVariants();
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

        var result = ScriptableObject.CreateInstance<ColorsData>();
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

    #endregion

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

        /*foreach (var item in spawnedTransitionTiles)
        {
            if (item != null && item.Predefined == false)
                Destroy(item.gameObject);
        }
        spawnedTransitionTiles = new Tile[MapSize.x, MapSize.y];
        */
        PrepareToGenerate();

        //PrepareToGenerateTransitions();
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


    private void ProcessPossibleTransitonTiles()
    {
        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                bool hasTransiton = HasTransition(x, y);
                if (!hasTransiton)
                    continue;

                var pixelColor = _texture.GetPixel(x, y);

                if (pixelColor.a == 0)
                    continue;

                // Debug.Log(pixelColor);

                var availableTilesByColor = GetTilesByColor(pixelColor);

                var availableTiles = AvailableTilesHere(x, y, availableTilesByColor);
                // Debug.Log($"[{x},{y}] availableTiles.Count {availableTiles.Count}");

                if (availableTiles.Count == 0)
                {
                    Debug.Log($"Нет подходяхи тайлов перехода = {pixelColor}");
                    continue;
                }

                var beforeAvailableCount = possibleTiles[x, y].Count;
                possibleTiles[x, y] = availableTiles;

                if (availableTiles.Count == 1)
                {
                    var tilePrefab = GetRandomTile(availableTiles);
                    var newTile = Instantiate<Tile>(tilePrefab, transform);
                    newTile.transform.position = new Vector3(x * TileSize, 0, y * TileSize);
                    newTile.Spawned = true;
                    // todo скрипт Tile вроде бы не нужен            
                    spawnedTiles[x, y] = newTile;
                }

                if (availableTiles.Count < beforeAvailableCount)
                {
                    AddNeighborsToQueue(new Vector2Int(x, y));
                }
            }
        }
    }


    private IEnumerator GenerateAllTranstionPossibleTiles()
    {
        var maxIterations = MapSize.x * MapSize.y;
        var iterations = 0;

        ProcessPossibleTransitonTiles();

        while (/*placedTilesCount < needPlaceCount ||*/ iterations++ < maxIterations)
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
                    Debug.LogError($"Не получилось собрать карту ({position.x}:{position.y})");
                    LogPossibleVariants();
                    //break;
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
            //LogPossibleVariants();

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

            var availableTiles = possibleTiles[minEntropyPosition.x, minEntropyPosition.y];
            var tileCollapse = GetRandomTile(availableTiles);
            possibleTiles[minEntropyPosition.x, minEntropyPosition.y] = new List<Tile>() { tileCollapse };

            PlaceTile(minEntropyPosition.x, minEntropyPosition.y, tileCollapse);

            AddNeighborsToQueue(minEntropyPosition);

            if (spawnDelay == 0)
                yield return new WaitForEndOfFrame();
            else
                yield return new WaitForSeconds(spawnDelay);
        }
        Debug.Log($"Failed, run out of iterations {iterations}");
        // return false;
    }

    private bool HasTransition(int x, int y)
    {
        var currentTile = spawnedTiles[x, y];

        if (x + 1 < MapSize.x)
        {
            if (spawnedTiles[x + 1, y].LevelHeight != currentTile.LevelHeight)
                return true;

        }

        if (x - 1 >= 0)
        {
            if (spawnedTiles[x - 1, y].LevelHeight != currentTile.LevelHeight)
                return true;
        }

        if (y + 1 < MapSize.y)
        {
            if (spawnedTiles[x, y + 1].LevelHeight != currentTile.LevelHeight)
                return true;
        }

        if (y - 1 >= 0)
        {
            if (spawnedTiles[x, y - 1].LevelHeight != currentTile.LevelHeight)
                return true;
        }

        return false;

    }

    private void DefineTileHeights()
    {
        var minHeight = float.MaxValue;
        var maxHeight = 0f;
        foreach (var tile in TilePrefabs)
        {
            if (tile.Height < minHeight)
                minHeight = tile.Height;

            if (tile.Height > maxHeight)
                maxHeight = tile.Height;
        }

        _minTileHeight = minHeight;
        _maxTileHeight = maxHeight;

        Debug.Log($"_minTileHeight = {_minTileHeight}; _maxTileHeight = {_maxTileHeight} ");
    }

    private void PrepareToGenerateTransitions()
    {
        possibleTransitionTiles = new List<Tile>[MapSize.x, MapSize.y];

        // needPlaceCount = MapSize.x * MapSize.y;
        // placedTilesCount = 0;

        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                possibleTransitionTiles[x, y] = new List<Tile>(TransitionsTilePrefabs);
            }
        }

        recalcPossibleTransitionTilesQueue.Clear();
    }
}
