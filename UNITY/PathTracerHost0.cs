using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PathTracerHost : MonoBehaviour
{
    [Header("Assets")]
    [SerializeField] private ComputeShader pathTracerShader;

    private Camera _camera;
    private RenderTexture _targetBuffer;
    private uint _currentFrameSeed = 0;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        _currentFrameSeed = 0;
    }

    private void Update()
    {
        // Whenever the camera moves or rotates, wipe the seed accumulator to clear ghosting artifacts
        if (transform.hasChanged)
        {
            _currentFrameSeed = 0;
            transform.hasChanged = false;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        EnsureRenderBufferCreated();

        // 1. Package uniform transformations down to the GPU context
        pathTracerShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        pathTracerShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        pathTracerShader.SetInt("_FrameSeed", (int)_currentFrameSeed);

        // 2. Bind buffers and dispatch 2D execution grid
        int kernelIndex = pathTracerShader.FindKernel("CSMain");
        pathTracerShader.SetTexture(kernelIndex, "Result", _targetBuffer);

        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        
        pathTracerShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);

        // 3. Blit (copy) the GPU path-tracer texture outcome directly onto the screen viewport canvas
        Graphics.Blit(_targetBuffer, destination);
        
        _currentFrameSeed++;
    }

    private void EnsureRenderBufferCreated()
    {
        if (_targetBuffer == null || _targetBuffer.width != Screen.width || _targetBuffer.height != Screen.height)
        {
            if (_targetBuffer != null)
                _targetBuffer.Release();

            _targetBuffer = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _targetBuffer.enableRandomWrite = true; // Essential flag to allow arbitrary write indexing in compute kernels
            _targetBuffer.Create();
            
            _currentFrameSeed = 0;
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