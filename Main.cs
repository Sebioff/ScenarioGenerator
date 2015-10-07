using UnityEngine;

namespace NewMod
{
    public class Main : IMod
    {
        private GameObject _go;

        #region Implementation of IMod

        public void onEnabled()
        {
            _go = new GameObject(Name);
            _go.AddComponent<TerrainGeneratorController>();
        }

        public void onDisabled()
        {
            Object.Destroy(_go);
        }

        public string Name
        {
            get { return "Terrain"; }
        }

        public string Description
        {
            get { return "t"; }
        }

        #endregion
    }
}