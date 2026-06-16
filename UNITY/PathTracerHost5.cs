using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PathTracerHost5 : MonoBehaviour
{
    [SerializeField] private ComputeShader hybridShader;
    private Camera _camera;
    private RenderTexture _targetBuffer;
    private RenderTexture _accumulationBuffer; // NEW: Holds the running sum of all frames

    private const int TargetWidth = 2048;
    private const int TargetHeight = 1088;

    private int _currentSample = 0; // NEW: Tracks how many frames we have accumulated
    private Matrix4x4 _previousCameraMatrix; // NEW: Detects if the camera moved

    // Cached shader IDs
    private static readonly int ShaderTimeID = Shader.PropertyToID("_Time");
    private static readonly int ShaderSampleIndexID = Shader.PropertyToID("_SampleIndex");

    private void Awake() => _camera = GetComponent<Camera>();

    private void Start() => _camera.depthTextureMode = DepthTextureMode.Depth;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        EnsureRenderBuffersCreated();
        if (hybridShader == null) { Graphics.Blit(source, destination); return; }

        // NEW: Reset accumulation if the camera moves or rotates
        Matrix4x4 currentMatrix = _camera.transform.localToWorldMatrix;
        if (currentMatrix != _previousCameraMatrix)
        {
            _currentSample = 0;
            _previousCameraMatrix = currentMatrix;
        }
        _currentSample++; // Increment the sample index count

        int kernelIndex = hybridShader.FindKernel("CSMain");
        hybridShader.SetTexture(kernelIndex, "BackgroundSource", source);
        hybridShader.SetTexture(kernelIndex, "Result", _targetBuffer);
        
        // NEW: Provide the accumulation buffer texture to the shader
        hybridShader.SetTexture(kernelIndex, "AccumulationBuffer", _accumulationBuffer);

        Texture depth = Shader.GetGlobalTexture("_CameraDepthTexture");
        if (depth != null) hybridShader.SetTexture(kernelIndex, "_CameraDepthTexture", depth);

        hybridShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        hybridShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);

        hybridShader.SetFloat(ShaderTimeID, Time.time);
        hybridShader.SetInt(ShaderSampleIndexID, _currentSample); // NEW: Send the current sample weight

        hybridShader.Dispatch(kernelIndex, Mathf.CeilToInt(TargetWidth / 32.0f), Mathf.CeilToInt(TargetHeight / 32.0f), 1);

        // Copy the blended result out to the screen display
        Graphics.Blit(_targetBuffer, destination);
    }

    private void EnsureRenderBuffersCreated()
    {
        if (_targetBuffer == null || _targetBuffer.width != TargetWidth || _targetBuffer.height != TargetHeight)
        {
            if (_targetBuffer != null) _targetBuffer.Release();
            _targetBuffer = new RenderTexture(TargetWidth, TargetHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true
            };
            _targetBuffer.Create();
        }

        // NEW: Instantiate the accumulation texture buffer matching requirements
        if (_accumulationBuffer == null || _accumulationBuffer.width != TargetWidth || _accumulationBuffer.height != TargetHeight)
        {
            if (_accumulationBuffer != null) _accumulationBuffer.Release();
            _accumulationBuffer = new RenderTexture(TargetWidth, TargetHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
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