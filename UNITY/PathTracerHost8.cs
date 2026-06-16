using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PathTracerHost8 : MonoBehaviour
{
    [SerializeField] private ComputeShader hybridShader;

    [Header("Laser Settings")]
    [SerializeField] private Vector3 laserOrigin = new Vector3(-2.0f, 0.6f, 3.8f);
    [SerializeField] private Vector3 laserDirection = new Vector3(1.0f, -0.15f, 0.05f);
    [Range(380f, 780f)]
    [SerializeField] private float laserWavelength = 532.0f; // 532nm is green
    [SerializeField] private float laserRadius = 0.012f;

    private Camera _camera;
    private RenderTexture _targetBuffer;
    private RenderTexture _accumulationBuffer; 

    private int _currentSample = 0; 
    private Matrix4x4 _previousCameraMatrix; 
    
    // Track previous laser values to reset accumulation buffer when they change
    private Vector3 _previousLaserOrigin;
    private Vector3 _previousLaserDirection;
    private float _previousLaserWavelength;
    private float _previousLaserRadius;

    private static readonly int ShaderTimeID = Shader.PropertyToID("_Time");
    private static readonly int ShaderSampleIndexID = Shader.PropertyToID("_SampleIndex");
    
    // Shader Uniform IDs
    private static readonly int LaserOriginID = Shader.PropertyToID("_LaserOrigin");
    private static readonly int LaserDirID = Shader.PropertyToID("_LaserDir");
    private static readonly int LaserWavelengthID = Shader.PropertyToID("_LaserWavelength");
    private static readonly int LaserRadiusID = Shader.PropertyToID("_LaserRadius");

    private void Awake() => _camera = GetComponent<Camera>();

    private void Start() => _camera.depthTextureMode = DepthTextureMode.Depth;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        EnsureRenderBuffersCreated(source.width, source.height);
        if (hybridShader == null) { Graphics.Blit(source, destination); return; }

        // Reset accumulation if camera moves OR if you adjust the laser via UI
        Matrix4x4 currentMatrix = _camera.transform.localToWorldMatrix;
        if (currentMatrix != _previousCameraMatrix || 
            laserOrigin != _previousLaserOrigin || 
            laserDirection != _previousLaserDirection ||
            laserWavelength != _previousLaserWavelength ||
            laserRadius != _previousLaserRadius)
        {
            _currentSample = 0;
            _previousCameraMatrix = currentMatrix;
            _previousLaserOrigin = laserOrigin;
            _previousLaserDirection = laserDirection;
            _previousLaserWavelength = laserWavelength;
            _previousLaserRadius = laserRadius;
        }
        _currentSample++; 

        int kernelIndex = hybridShader.FindKernel("CSMain");
        
        // Pass Texture Buffers
        hybridShader.SetTexture(kernelIndex, "Result", _targetBuffer);
        hybridShader.SetTexture(kernelIndex, "AccumulationBuffer", _accumulationBuffer);
        hybridShader.SetTexture(kernelIndex, "BackgroundSource", source);
        hybridShader.SetTexture(kernelIndex, "_CameraDepthTexture", Shader.GetGlobalTexture("_CameraDepthTexture"));

        // Pass Camera Transform Matrices
        hybridShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        hybridShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);

        // Pass Time & Sample Data
        hybridShader.SetFloat(ShaderTimeID, Time.time);
        hybridShader.SetInt(ShaderSampleIndexID, _currentSample);
        hybridShader.SetVector("_ZBufferParams", SystemInfo.usesReversedZBuffer ? new Vector4(-1f, 1f, 1f, 1f) : new Vector4(1f, 1f, 1f, 1f));

        // ==========================================
        // PASS UI PARAMETERS TO COMPUTE SHADER
        // ==========================================
        hybridShader.SetVector(LaserOriginID, laserOrigin);
        hybridShader.SetVector(LaserDirID, laserDirection.normalized); // Normalized safely on CPU
        hybridShader.SetFloat(LaserWavelengthID, laserWavelength);
        hybridShader.SetFloat(LaserRadiusID, laserRadius);

        // Dispatch precisely sized grid threads 
        hybridShader.Dispatch(kernelIndex, Mathf.CeilToInt(source.width / 32.0f), Mathf.CeilToInt(source.height / 32.0f), 1);

        // Present the final combined buffer
        Graphics.Blit(_targetBuffer, destination);
    }

    private void EnsureRenderBuffersCreated(int width, int height)
    {
        if (_targetBuffer == null || _targetBuffer.width != width || _targetBuffer.height != height)
        {
            if (_targetBuffer != null) _targetBuffer.Release();
            _targetBuffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true
            };
            _targetBuffer.Create();
        }

        if (_accumulationBuffer == null || _accumulationBuffer.width != width || _accumulationBuffer.height != height)
        {
            if (_accumulationBuffer != null) _accumulationBuffer.Release();
            _accumulationBuffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true
            };
            _accumulationBuffer.Create();
            _currentSample = 0;
        }
    }

    private void OnDisable()
    {
        if (_targetBuffer != null) _targetBuffer.Release();
        if (_accumulationBuffer != null) _accumulationBuffer.Release();
    }
}