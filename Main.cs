using System.Linq;
using UnityEngine;

namespace NewMod
{
    public class TestGeneratorMod : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(250, 10, 200, 20), "Generate"))
            {
                Generate();
            }
            if (GUI.Button(new Rect(250, 50, 200, 20), "Reset"))
            {
                Reset();
            }
        }

        private void Reset()
        {
            var park = GameController.Instance.park;

            if (park == null) return;

            for (var x = 0; x < 100; x++)
                for (var z = 0; z < 100; z++)
                {
                    var patch = park.getTerrain(x, z);
                    for (var i = 0; i < 4; i++)
                    {
                        var d = 3.0f - patch.h[i];
                        if (d != 0)
                            patch.changeHeight(i, d);
                    }

                    WaterFlooding.unflood(new Vector3(x, 3, z));
                }


        }

        private void Generate()
        {
            const float diverse = 20f;
            const float maxHeight = 4.0f;
            const float maxDepth = 5.0f;
            const float ditchRatio = 0.6f;
            const float roadWidth = 7f;
            const int floodRounds = 15;
            const float gateClearance = 10f;
            const float defHeight = 3;
            var park = GameController.Instance.park;

            if (park == null) return;

            for (var z = 0; z <= 100; z++)
            {
                for (var x = 7; x <= 100; x++)
                {
                    var sample = (Mathf.PerlinNoise(x/diverse, z/diverse)*(1+ditchRatio)) - ditchRatio;
                    sample = sample*(sample < 0 ? maxDepth : maxHeight);
                    sample = Mathf.Round(sample);
                    
                    // Limit near road
                    if (x < roadWidth)
                        continue;
                    var max = x - roadWidth;
                    var min = roadWidth - x;
                    sample = Mathf.Clamp(sample, min, max);

                    // Limit near entrance
                    var distanceToEntrance = Vector3.Distance(park.parkEntrances.First<ParkEntrance>().transform.position,
                        new Vector3(x, 3, z));
                    if (distanceToEntrance < gateClearance)
                        continue;

                    if (sample != 0)
                        for (var cornerIndex = 0; cornerIndex < 4; cornerIndex++)
                        {
                            var ox = cornerIndex == 1 || cornerIndex == 2 ? 1 : 0;
                            var oz = cornerIndex == 2 || cornerIndex == 3 ? 1 : 0;

                            var patch = park.getTerrain(x - ox, z - oz);

                            if (patch != null)
                            {
                                var current = patch.h[cornerIndex] - defHeight;
                                patch.smoothChangeHeight(park, cornerIndex, sample - current);

                            }
                        }
                }
            }

            for (var i = 0; i < floodRounds; i++)
            {
                var x = Mathf.RoundToInt(Random.Range(roadWidth, 100));
                var z = Mathf.RoundToInt(Random.Range(0, 100));

                WaterFlooding.flood(new Vector3(x, defHeight, z));
            }
        }
    }

    public class Main : IMod
    {
        private GameObject _go;

        #region Implementation of IMod

        public void onEnabled()
        {
            _go = new GameObject(Name);
            _go.AddComponent<TestGeneratorMod>();
        }

        public void onDisabled()
        {
            Object.Destroy(_go);
        }

        public string Name { get { return "Terrain"; } }
        public string Description { get { return "t"; } }

        #endregion
    }
}
