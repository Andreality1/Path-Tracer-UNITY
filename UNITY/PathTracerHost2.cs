using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PathTracerHost2 : MonoBehaviour
{
    [Header("Shader Resources")]
    [SerializeField] private ComputeShader hybridShader;

    private Camera _camera;
    private RenderTexture _targetBuffer;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        // Force Unity's camera to populate the global high-precision depth buffer texture
        _camera.depthTextureMode = DepthTextureMode.Depth;
        Debug.Log($"Screen Size: {Screen.width}x{Screen.height} ");
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        EnsureRenderBufferCreated();

        if (hybridShader == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        int kernelIndex = hybridShader.FindKernel("CSMain");

        // 1. Pass the rasterized scene data (Color and Depth buffers) down to the GPU kernel
        hybridShader.SetTexture(kernelIndex, "BackgroundSource", source);
        hybridShader.SetTexture(kernelIndex, "Result", _targetBuffer);
        
        Texture nativeDepthTexture = Shader.GetGlobalTexture("_CameraDepthTexture");
        if (nativeDepthTexture != null)
        {
            hybridShader.SetTexture(kernelIndex, "_CameraDepthTexture", nativeDepthTexture);
        }

        // 2. Supply the inversion matrices to map 2D pixels back into 3D Unity World Space
        hybridShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        hybridShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);

        // 3. Dispatch execution blocks (Threads grouped in 32x32 blocks matching screen resolution)
        // CHANGED: Denominator changed from 8.0f to 32.0f.
        // For a 2K screen (1920x1080), this translates exactly to thread groups dispatched.
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 32.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 32.0f);
        
        

        hybridShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);

        // 4. Draw our hybrid texture directly onto the physical screen buffer
        Graphics.Blit(_targetBuffer, destination);
    }

    private void EnsureRenderBufferCreated()
    {
        if (_targetBuffer == null || _targetBuffer.width != Screen.width || _targetBuffer.height != Screen.height)
        {
            if (_targetBuffer != null)
            {
                _targetBuffer.Release();
            }

            _targetBuffer = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _targetBuffer.enableRandomWrite = true; // Essential for RWTexture2D access
            _targetBuffer.Create();
        }
    }

    private void OnDisable()
    {
        if (_targetBuffer != null)
        {
            _targetBuffer.Release();
            _targetBuffer = null;
        }
    }
}