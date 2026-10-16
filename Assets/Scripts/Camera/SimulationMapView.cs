using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CameraSystems
{
    /// <summary>
    /// Draws the simulation camera into a texture and shows that texture on a UI image.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationMapView : MonoBehaviour
    {
        [SerializeField] private Camera simulationCamera;
        [SerializeField] private RawImage view;
        [SerializeField] private GameObject backdrop;
        [SerializeField] private RenderTexture texture;
        [SerializeField] private SimulationCameraController framing;

        RenderTexture runtime;
        bool ready;

        void Awake()
        {
            if (simulationCamera == null || view == null || backdrop == null ||
                texture == null || framing == null)
            {
                Debug.LogError(
                    "The simulation map view needs a camera, an image, a backdrop, a render texture, and framing.",
                    this);
                return;
            }

            runtime = new RenderTexture(texture)
            {
                name = "Simulation Map",
            };
            runtime.Create();
            simulationCamera.targetTexture = runtime;
            view.texture = runtime;
            ready = true;
            SetShown(true);
        }

        void OnDestroy()
        {
            if (simulationCamera != null)
            {
                simulationCamera.targetTexture = null;
            }

            if (runtime == null)
            {
                return;
            }

            runtime.Release();
            Destroy(runtime);
            runtime = null;
        }

        public void SetShown(bool shown)
        {
            if (simulationCamera == null || view == null || backdrop == null)
            {
                Debug.LogError(
                    "The simulation map view needs a camera, an image, and a backdrop.",
                    this);
                return;
            }

            if (!shown)
            {
                view.gameObject.SetActive(false);
                backdrop.SetActive(true);
                simulationCamera.enabled = false;
                return;
            }

            if (!ready)
            {
                return;
            }

            view.gameObject.SetActive(true);
            backdrop.SetActive(false);
            simulationCamera.enabled = true;

            Canvas.ForceUpdateCanvases();
            FrameToPanel();
        }

        void LateUpdate()
        {
            if (!ready || !simulationCamera.enabled)
            {
                return;
            }

            FrameToPanel();
        }

        void FrameToPanel()
        {
            if (!FitTexture())
            {
                return;
            }

            framing.FrameForView((float)runtime.width / runtime.height);
        }

        bool FitTexture()
        {
            var rect = view.rectTransform.rect;
            var scale = view.canvas != null ? view.canvas.scaleFactor : 1f;
            var width = Mathf.Max(16, Mathf.RoundToInt(rect.width * scale));
            var height = Mathf.Max(16, Mathf.RoundToInt(rect.height * scale));
            if (rect.width < 1f || rect.height < 1f)
            {
                width = Mathf.Max(16, texture.width);
                height = Mathf.Max(16, texture.height);
            }

            var resized = runtime.width != width ||
                runtime.height != height ||
                !runtime.IsCreated();
            if (resized)
            {
                runtime.Release();
                runtime.width = width;
                runtime.height = height;
                runtime.Create();
                simulationCamera.targetTexture = runtime;
            }

            var aspect = (float)width / height;
            return resized || !Mathf.Approximately(simulationCamera.aspect, aspect);
        }
    }
}
