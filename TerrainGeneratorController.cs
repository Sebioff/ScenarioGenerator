using System.Linq;
using UnityEngine;

namespace NewMod
{
    public class TerrainGeneratorController : MonoBehaviour
    {
        private const float MinRoadWidth = 7f;
        private const float GroundHeight = 3.0f;
        private const float WaterOffset = 0.05f;
        private const float ParkFenceOffset = 2f;
        private const int DefaultTerrainType = 0;

        private float _ditchRatio = .6f;
        private float _entranceClearance = 7;
        private float _floodRounds = 30;
        private bool _generateTerrainType = true;
        private bool _isMenuOpen;
        private float _maxDepth = 4f;
        private float _maxHeight = 3f;

        private float _plainScale = .15f;
        private float _terrainScale = .5f;
        private float _treeCount = 100f;

        private Rect _windowRectangle = new Rect(50, 50, 1, 1);

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                _isMenuOpen = !_isMenuOpen;

                // If first opening set window to the center
                if (_windowRectangle.width == 1)
                    _windowRectangle = new Rect(Screen.width/2 - 310/2, Screen.height/2 - 500/2, 310, 500);
            }
        }

        private Rect UIRectangle(int index)
        {
            const int elementHeight = 20;
            const int elementHeightWithMargin = elementHeight + 2;

            return new Rect(5, 15 + elementHeightWithMargin*index, 300, elementHeight);
        }

        private void DoWindow(int windowId)
        {
            var index = 0;

            GUI.Label(UIRectangle(index++), "Plain scale: " + _plainScale);
            _plainScale = GUI.HorizontalSlider(UIRectangle(index++), _plainScale, 0.0005f, 1.0f);

            GUI.Label(UIRectangle(index++), "Max height: " + _maxHeight);
            _maxHeight = GUI.HorizontalSlider(UIRectangle(index++), _maxHeight, 0, 20);

            GUI.Label(UIRectangle(index++), "Max depth: " + _maxDepth);
            _maxDepth = GUI.HorizontalSlider(UIRectangle(index++), _maxDepth, 0, 20);

            GUI.Label(UIRectangle(index++), "Ditch ratio: " + _ditchRatio);
            _ditchRatio = GUI.HorizontalSlider(UIRectangle(index++), _ditchRatio, 0, 1);

            GUI.Label(UIRectangle(index++), "Flood rounds: " + _floodRounds);
            _floodRounds = GUI.HorizontalSlider(UIRectangle(index++), _floodRounds, 0, 100);

            GUI.Label(UIRectangle(index++), "Entrance clearance: " + _entranceClearance);
            _entranceClearance = GUI.HorizontalSlider(UIRectangle(index++), _entranceClearance, 0, 50);

            _generateTerrainType = GUI.Toggle(UIRectangle(index++), _generateTerrainType, "Generate Terrain Type");

            GUI.Label(UIRectangle(index++), "Terrain scale: " + _terrainScale);
            _terrainScale = GUI.HorizontalSlider(UIRectangle(index++), _terrainScale, 0.2f, 5.0f);

            GUI.Label(UIRectangle(index++), "Trees: " + _treeCount);
            _treeCount = GUI.HorizontalSlider(UIRectangle(index++), _treeCount, 0, 1000);

            if (GUI.Button(UIRectangle(index++), "Generate"))
            {
                Generate(_plainScale, _maxHeight, _maxDepth, _ditchRatio, _floodRounds, _entranceClearance,
                    _generateTerrainType, _terrainScale, _treeCount);
                _isMenuOpen = false;
            }
            if (GUI.Button(UIRectangle(index++), "Reset")) Reset();
            if (GUI.Button(UIRectangle(index++), "Cancel")) _isMenuOpen = false;

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void OnGUI()
        {
            if (!_isMenuOpen)
                return;

            _windowRectangle = GUI.Window(0, _windowRectangle, DoWindow, "Scenario Generator");
        }

        private void Reset()
        {
            var park = GameController.Instance.park;

            if (park == null) return;

            for (var x = 0; x < park.xSize; x++)
                for (var z = 0; z < park.zSize; z++)
                {
                    var patch = park.getTerrain(x, z);

                    // Remove water if present.
                    if (patch.hasWater())
                        WaterFlooding.unflood(new Vector3(x, 0, z));

                    // Reset height.
                    for (var i = 0; i < 4; i++)
                    {
                        var d = GroundHeight - patch.h[i];
                        if (d != 0)
                            patch.changeHeight(i, d);
                    }

                    // Reset terrain type.
                    patch.TerrainType = DefaultTerrainType;
                }


            foreach (var o in FindObjectsOfType<TreeEntity>())
                o.Kill();
        }

        private void Generate(float diverse, float maxHeight, float maxDepth, float ditchRatio, float floodRounds,
            float clearance, bool generateTerrainType, float terrainScale, float treeCount)
        {
            const float defHeight = 3;
            var park = GameController.Instance.park;

            if (park == null) return;

            // Generate a random seed.
            var seed = Random.Range(-1000, 1000);

            for (var z = 0; z <= park.zSize; z++)
            {
                for (var x = 0; x <= park.xSize; x++)
                {
                    // Calculate height of terrain patch based on perlin noise.
                    var y = (Mathf.PerlinNoise(x/(park.xSize*diverse) + seed, z/(park.zSize*diverse) + seed)*
                             (1 + ditchRatio)) -
                            ditchRatio;
                    if (y < 0 && ditchRatio != 0)
                        y /= ditchRatio;

                    y = y*(y < 0 ? maxDepth : maxHeight);
                    y = y > 0 ? Mathf.FloorToInt(y) : Mathf.CeilToInt(y);

                    // Generate terrain type for the terrain patch based on perlin noise.
                    if (generateTerrainType)
                    {
                        var patch = park.getTerrain(x, z);
                        if (patch != null)
                        {
                            var types = ScriptableSingleton<AssetManager>.Instance.terrainTypes.Length;
                            var terrainTypeIndex = Mathf.PerlinNoise(x/(park.xSize*terrainScale) + seed,
                                z/(park.xSize*terrainScale) + seed);

                            patch.TerrainType = Mathf.FloorToInt(Mathf.Abs(terrainTypeIndex - 0.5f)*types);
                        }
                    }

                    // Limit heights near road.
                    var roadWidth = Mathf.Max(MinRoadWidth, clearance);
                    if (x < roadWidth)
                        continue;

                    y = Mathf.Clamp(y, roadWidth - x, x - roadWidth);

                    // Limit heights near entrance.
                    var distanceToEntrance = Vector3.Distance(park.parkEntrances.First().transform.position,
                        new Vector3(x, GroundHeight, z));

                    if (distanceToEntrance < roadWidth)
                        continue;

                    // If this location should be raised, change the hight of the patch nad the ones around.
                    if (y != 0)
                        for (var cornerIndex = 0; cornerIndex < 4; cornerIndex++)
                        {
                            var ox = cornerIndex == 1 || cornerIndex == 2 ? 1 : 0;
                            var oz = cornerIndex == 2 || cornerIndex == 3 ? 1 : 0;

                            var patch = park.getTerrain(x - ox, z - oz);

                            if (patch != null)
                            {
                                var current = patch.h[cornerIndex] - defHeight;
                                patch.smoothChangeHeight(park, cornerIndex, y - current);
                            }
                        }
                }
            }

            // Randomly flood the map.
            for (var i = 0; i < floodRounds; i++)
            {
                var x = Mathf.RoundToInt(Random.Range(clearance, park.xSize));
                var z = Mathf.RoundToInt(Random.Range(0, park.zSize));

                var patch = park.getTerrain(x, z);
                if (patch == null || patch.hasWater() || patch.getLowestHeight() >= GroundHeight) continue;

                WaterFlooding.flood(new Vector3(x, GroundHeight - WaterOffset, z));
            }

            // Randomly spawn a forrest.
            for (var i = 0; i < treeCount; i++)
            {
                var x = Mathf.RoundToInt(Random.Range(clearance, park.xSize - ParkFenceOffset));
                var z = Mathf.RoundToInt(Random.Range(ParkFenceOffset, park.zSize - ParkFenceOffset));

                var patch = park.getTerrain(x, z);
                if (patch == null || patch.hasWater()) continue;

                var y = patch.getHeighestHeight();
                if (y != patch.getLowestHeight()) continue;

                TreeEntity fir = null;
                foreach (var o in ScriptableSingleton<AssetManager>.Instance.decoObjects)
                    if (o.getName().StartsWith("Fir") && o is TreeEntity) fir = o as TreeEntity;

                if (fir != null)
                {
                    var tree = Instantiate(fir);
                    tree.transform.position = new Vector3(x, y, z);
                    tree.transform.forward = Vector3.forward;
                    tree.Initialize();
                    tree.startAnimateSpawn(Vector3.zero);
                }
            }
        }
    }
}