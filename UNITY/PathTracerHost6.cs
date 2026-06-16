using UnityEngine;
//a
[RequireComponent(typeof(Camera))]
public class PathTracerHost6 : MonoBehaviour
{
    [SerializeField] private ComputeShader hybridShader;
    private Camera _camera;
    private RenderTexture _targetBuffer;
    private RenderTexture _accumulationBuffer; 

    private int _currentSample = 0; 
    private Matrix4x4 _previousCameraMatrix; 

    private static readonly int ShaderTimeID = Shader.PropertyToID("_Time");
    private static readonly int ShaderSampleIndexID = Shader.PropertyToID("_SampleIndex");

    private void Awake() => _camera = GetComponent<Camera>();

    private void Start() => _camera.depthTextureMode = DepthTextureMode.Depth;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Dynamically match the buffer size to your screen view resolution
        EnsureRenderBuffersCreated(source.width, source.height);
        if (hybridShader == null) { Graphics.Blit(source, destination); return; }

        // If the camera moves or rotates, drop accumulation sample down to 0 to clear noise smearing
        Matrix4x4 currentMatrix = _camera.transform.localToWorldMatrix;
        if (currentMatrix != _previousCameraMatrix)
        {
            _currentSample = 0;
            _previousCameraMatrix = currentMatrix;
        }
        _currentSample++; 

        int kernelIndex = hybridShader.FindKernel("CSMain");
        hybridShader.SetTexture(kernelIndex, "BackgroundSource", source);
        hybridShader.SetTexture(kernelIndex, "Result", _targetBuffer);
        hybridShader.SetTexture(kernelIndex, "AccumulationBuffer", _accumulationBuffer);

        Texture depth = Shader.GetGlobalTexture("_CameraDepthTexture");
        if (depth != null) hybridShader.SetTexture(kernelIndex, "_CameraDepthTexture", depth);

        // Send matrix positions to the compute shader
        hybridShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        hybridShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);

        hybridShader.SetFloat(ShaderTimeID, Time.time);
        hybridShader.SetInt(ShaderSampleIndexID, _currentSample); 

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