using System.Collections.Generic;
using AvantajPrim.AbilitiesDemo.Domain;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    /// <summary>World-space UI root with per-entity health canvases and a world TMP root for combat numbers.</summary>
    public sealed class DemoWorldUiCanvas
    {
        private readonly Transform _root;
        private readonly List<EntityHealthBar> _bars = new List<EntityHealthBar>(8);

        private Vector3 _lastCameraPosition;
        private Quaternion _lastCameraRotation;
        private bool _hasCameraSnapshot;

        private Transform _floatingNumbersWorldRoot;

        public Transform Transform => _root;
        public Canvas HealthBarsCanvas { get; private set; }
        public Transform FloatingNumbersWorldRoot
        {
            get
            {
                EnsureRoots();
                return _floatingNumbersWorldRoot;
            }
        }

        public Transform HealthBarsRoot => HealthBarsCanvas != null ? HealthBarsCanvas.transform : _root;
        public Camera Camera { get; private set; }

        public DemoWorldUiCanvas(Transform root)
        {
            _root = root;
        }

        public void Initialize(Camera camera)
        {
            Camera = camera;
            EnsureRoots();
            HealthBarsCanvas.worldCamera = camera;
        }

        public void Register(EntityHealthBar bar)
        {
            if (bar != null && !_bars.Contains(bar))
                _bars.Add(bar);
        }

        public void Unregister(EntityHealthBar bar) => _bars.Remove(bar);

        public void Clear()
        {
            for (int i = _bars.Count - 1; i >= 0; i--)
                _bars[i]?.Dispose();

            _bars.Clear();
            _hasCameraSnapshot = false;
        }

        public void SetGameplayUiVisible(bool visible)
        {
            EnsureRoots();
            HealthBarsCanvas.gameObject.SetActive(visible);
            _floatingNumbersWorldRoot.gameObject.SetActive(visible);

            for (int i = 0; i < _bars.Count; i++)
            {
                EntityHealthBar bar = _bars[i];
                if (bar == null || !bar.IsAlive)
                    continue;

                GameObject barObject = bar.GameObject;
                if (barObject != null && barObject.activeSelf != visible)
                    barObject.SetActive(visible);
            }
        }

        public void LateBillboard()
        {
            if (Camera == null || _bars.Count == 0)
                return;

            Transform cameraTransform = Camera.transform;
            Vector3 cameraPos = cameraTransform.position;
            Quaternion cameraRot = cameraTransform.rotation;
            bool cameraMoved = !_hasCameraSnapshot ||
                               (cameraPos - _lastCameraPosition).sqrMagnitude > DemoConstants.Ui.CameraMoveEpsilonSqr ||
                               Quaternion.Angle(cameraRot, _lastCameraRotation) >
                               DemoConstants.Ui.CameraRotationAngleEpsilon;

            for (int i = _bars.Count - 1; i >= 0; i--)
            {
                EntityHealthBar bar = _bars[i];
                if (bar == null || !bar.IsAlive)
                {
                    _bars.RemoveAt(i);
                    continue;
                }

                if (cameraMoved || bar.NeedsBillboard())
                    bar.Billboard(Camera);
            }

            if (!cameraMoved)
                return;

            _lastCameraPosition = cameraPos;
            _lastCameraRotation = cameraRot;
            _hasCameraSnapshot = true;
        }

        private void EnsureRoots()
        {
            if (HealthBarsCanvas == null)
                HealthBarsCanvas = CreateChildCanvas("HealthBarsCanvas", sortingOrder: 0);

            if (_floatingNumbersWorldRoot != null)
                return;

            var worldRoot = new GameObject("FloatingNumbersWorldRoot");
            worldRoot.transform.SetParent(_root, false);
            _floatingNumbersWorldRoot = worldRoot.transform;
        }

        private Canvas CreateChildCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(_root, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = DemoConstants.Ui.ChildCanvasSize;

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera;
            canvas.sortingOrder = sortingOrder;
            return canvas;
        }
    }
}
