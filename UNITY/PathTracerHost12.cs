using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PathTracerHost12 : MonoBehaviour
{
    public enum LightEmissionMode { MonochromaticLaser, PolychromaticSpectrum }

    [SerializeField] private ComputeShader hybridShader;

    [Header("Laser Settings")]
    [SerializeField] private Vector3 laserOrigin = new Vector3(-2.0f, 0.6f, 3.8f);
    [SerializeField] private Vector3 laserDirection = new Vector3(1.0f, -0.15f, 0.05f);
    [Range(380f, 780f)]
    [SerializeField] private float laserWavelength = 532.0f; 
    [SerializeField] private float laserRadius = 0.012f;

    [Header("Floor Settings")]
    [SerializeField] private float planeY = 0.0f; 

    // --- Exposed fields for our Custom Editor ---
    [HideInInspector] public LightEmissionMode lightMode = LightEmissionMode.PolychromaticSpectrum;
    [HideInInspector] public Vector3 lightCenter = new Vector3(-2.5f, 3.0f, 5.0f);
    [HideInInspector] public float lightRadius = 0.15f;
    [HideInInspector] public float lightIntensity = 150.0f;
    [HideInInspector] public float lightDiscreteWavelength = 532.0f;
    [HideInInspector] public Color lightColorPicker = Color.white;

    [Header("Prism Transform Settings")]
    [SerializeField] private Vector3 prismPosition = new Vector3(0.0f, 1.5f, 5.0f);
    [SerializeField] private Vector3 prismRotation = new Vector3(0.0f, 0.0f, 45.0f); 
    [SerializeField] private float prismSideLength = 1.0f;
    [SerializeField] private float prismLength = 2.0f;

    private Camera _camera;
    private RenderTexture _targetBuffer;
    private RenderTexture _accumulationBuffer; 

    private int _currentSample = 0; 
    private Matrix4x4 _previousCameraMatrix; 
    
    private int _prevLightMode;
    private Vector3 _prevLaserOrigin;
    private Vector3 _prevLaserDirection;
    private float _prevLaserWavelength;
    private float _prevLaserRadius;
    private float _prevPlaneY;
    private Vector3 _prevLightCenter;
    private float _prevLightRadius;
    private float _prevLightWavelength;
    private float _prevLightIntensity;
    private Color _prevLightColor;
    private Vector3 _prevPrismPosition;
    private Vector3 _prevPrismRotation;
    private float _prevPrismSideLength;
    private float _prevPrismLength;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        _camera.depthTextureMode = DepthTextureMode.Depth; 
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        EnsureRenderBuffersCreated(source.width, source.height);

        bool sceneChanged = 
            transform.localToWorldMatrix != _previousCameraMatrix ||
            (int)lightMode != _prevLightMode ||
            laserOrigin != _prevLaserOrigin ||
            laserDirection != _prevLaserDirection ||
            !Mathf.Approximately(laserWavelength, _prevLaserWavelength) ||
            !Mathf.Approximately(laserRadius, _prevLaserRadius) ||
            !Mathf.Approximately(planeY, _prevPlaneY) ||
            lightCenter != _prevLightCenter ||
            !Mathf.Approximately(lightRadius, _prevLightRadius) ||
            !Mathf.Approximately(lightDiscreteWavelength, _prevLightWavelength) ||
            !Mathf.Approximately(lightIntensity, _prevLightIntensity) ||
            lightColorPicker != _prevLightColor ||
            prismPosition != _prevPrismPosition ||
            prismRotation != _prevPrismRotation ||
            !Mathf.Approximately(prismSideLength, _prevPrismSideLength) ||
            !Mathf.Approximately(prismLength, _prevPrismLength);

        if (sceneChanged)
        {
            _currentSample = 0;
            Graphics.Blit(Texture2D.blackTexture, _accumulationBuffer);
            
            _previousCameraMatrix = transform.localToWorldMatrix;
            _prevLightMode = (int)lightMode;
            _prevLaserOrigin = laserOrigin;
            _prevLaserDirection = laserDirection;
            _prevLaserWavelength = laserWavelength;
            _prevLaserRadius = laserRadius;
            _prevPlaneY = planeY;
            _prevLightCenter = lightCenter;
            _prevLightRadius = lightRadius;
            _prevLightWavelength = lightDiscreteWavelength;
            _prevLightIntensity = lightIntensity;
            _prevLightColor = lightColorPicker;
            _prevPrismPosition = prismPosition;
            _prevPrismRotation = prismRotation;
            _prevPrismSideLength = prismSideLength;
            _prevPrismLength = prismLength;
        }

        _currentSample++;
        int kernelIndex = hybridShader.FindKernel("CSMain");

        hybridShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        hybridShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        hybridShader.SetFloat("_Time", Time.time);
        hybridShader.SetInt("_SampleIndex", _currentSample);

        hybridShader.SetVector("_LaserOrigin", laserOrigin);
        hybridShader.SetVector("_LaserDir", laserDirection.normalized);
        hybridShader.SetFloat("_LaserWavelength", laserWavelength);
        hybridShader.SetFloat("_LaserRadius", laserRadius);
        hybridShader.SetFloat("_PlaneY", planeY);

        hybridShader.SetInt("_LightMode", (int)lightMode);
        hybridShader.SetVector("_LightCenter", lightCenter);
        hybridShader.SetFloat("_LightRadius", lightRadius);
        hybridShader.SetFloat("_LightWavelength", lightDiscreteWavelength);
        hybridShader.SetFloat("_LightIntensity", lightIntensity);
        hybridShader.SetVector("_LightColor", (Vector4)lightColorPicker);

        hybridShader.SetVector("_PrismPosition", prismPosition);
        hybridShader.SetFloat("_PrismSideLength", prismSideLength);
        hybridShader.SetFloat("_PrismLength", prismLength);
        
        Matrix4x4 prismMatrix = Matrix4x4.TRS(prismPosition, Quaternion.Euler(prismRotation), Vector3.one);
        hybridShader.SetMatrix("_PrismWorldToLocal", prismMatrix.inverse);
        hybridShader.SetMatrix("_PrismLocalToWorld", prismMatrix);

        hybridShader.SetTexture(kernelIndex, "Result", _targetBuffer);
        hybridShader.SetTexture(kernelIndex, "AccumulationBuffer", _accumulationBuffer);
        hybridShader.SetTexture(kernelIndex, "BackgroundSource", source);

        hybridShader.Dispatch(kernelIndex, Mathf.CeilToInt(source.width / 32.0f), Mathf.CeilToInt(source.height / 32.0f), 1);

        Graphics.Blit(_targetBuffer, destination);
    }

    private void EnsureRenderBuffersCreated(int width, int height)
    {
        if (_targetBuffer == null || _targetBuffer.width != width || _targetBuffer.height != height)
        {
            if (_targetBuffer != null) _targetBuffer.Release();
            _targetBuffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { enableRandomWrite = true };
            _targetBuffer.Create();
        }
        if (_accumulationBuffer == null || _accumulationBuffer.width != width || _accumulationBuffer.height != height)
        {
            if (_accumulationBuffer != null) _accumulationBuffer.Release();
            _accumulationBuffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { enableRandomWrite = true };
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