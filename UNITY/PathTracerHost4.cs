using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PathTracerHost4 : MonoBehaviour
{
    [SerializeField] private ComputeShader hybridShader;
    private Camera _camera;
    private RenderTexture _targetBuffer;
    private const int TargetWidth = 2048;
    private const int TargetHeight = 1088;

    // Cache the shader property ID for efficiency
    private static readonly int ShaderTimeID = Shader.PropertyToID("_Time");

    private void Awake() => _camera = GetComponent<Camera>();

    private void Start() => _camera.depthTextureMode = DepthTextureMode.Depth;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        EnsureRenderBufferCreated();
        if (hybridShader == null) { Graphics.Blit(source, destination); return; }

        int kernelIndex = hybridShader.FindKernel("CSMain");
        hybridShader.SetTexture(kernelIndex, "BackgroundSource", source);
        hybridShader.SetTexture(kernelIndex, "Result", _targetBuffer);
        
        Texture depth = Shader.GetGlobalTexture("_CameraDepthTexture");
        if (depth != null) hybridShader.SetTexture(kernelIndex, "_CameraDepthTexture", depth);

        hybridShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        hybridShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);

        // --- NEW: Pass the changing time variable to the compute shader ---
        hybridShader.SetFloat(ShaderTimeID, Time.time);

    
        //Debug.Log(Time.time);

        hybridShader.Dispatch(kernelIndex, Mathf.CeilToInt(TargetWidth / 32.0f), Mathf.CeilToInt(TargetHeight / 32.0f), 1);
        Graphics.Blit(_targetBuffer, destination);
    }

    private void EnsureRenderBufferCreated()
    {
        if (_targetBuffer == null || _targetBuffer.width != TargetWidth || _targetBuffer.height != TargetHeight)
        {
            if (_targetBuffer != null) _targetBuffer.Release();
            _targetBuffer = new RenderTexture(TargetWidth, TargetHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _targetBuffer.enableRandomWrite = true;
            _targetBuffer.Create();
        }
    }
}