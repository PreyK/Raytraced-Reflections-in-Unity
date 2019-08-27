using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RayTracingMaster : MonoBehaviour
{

    public Texture SkyboxTexture;
    public ComputeShader RayTracingShader;
    private RenderTexture _target;
    private Camera _camera;

    private uint _currentSample = 0;
    private Material _addMaterial;
    // MESHES
    private static bool _meshObjectsNeedRebuilding = false;
    private static List<RayTracingObject> _rayTracingObjects = new List<RayTracingObject>();
    //

    // MESH STUFF
    struct MeshObject
    {
        public Matrix4x4 localToWorldMatrix;
        public int indices_offset;
        public int indices_count;
    }
    private static List<MeshObject> _meshObjects = new List<MeshObject>();
    private static List<Vector3> _vertices = new List<Vector3>();
    private static List<int> _indices = new List<int>();
    private ComputeBuffer _meshObjectBuffer;
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _indexBuffer;

    //MESH STUFF
    public static void RegisterObject(RayTracingObject obj)
    {
        _rayTracingObjects.Add(obj);
        _meshObjectsNeedRebuilding = true;
    }
    public static void UnregisterObject(RayTracingObject obj)
    {
        _rayTracingObjects.Remove(obj);
        _meshObjectsNeedRebuilding = true;
    }


    private void RebuildMeshObjectBuffers()
    {
        if (!_meshObjectsNeedRebuilding)
        {
            return;
        }
        _meshObjectsNeedRebuilding = false;
        _currentSample = 0;
        // Clear all lists
        _meshObjects.Clear();
        _vertices.Clear();
        _indices.Clear();
        // Loop over all objects and gather their data
        foreach (RayTracingObject obj in _rayTracingObjects)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            // Add vertex data
            int firstVertex = _vertices.Count;
            _vertices.AddRange(mesh.vertices);
            // Add index data - if the vertex buffer wasn't empty before, the
            // indices need to be offset
            int firstIndex = _indices.Count;
            var indices = mesh.GetIndices(0);
            _indices.AddRange(indices.Select(index => index + firstVertex));
            // Add the object itself
            _meshObjects.Add(new MeshObject()
            {
                localToWorldMatrix = obj.transform.localToWorldMatrix,
                indices_offset = firstIndex,
                indices_count = indices.Length
            });
        }
        CreateComputeBuffer(ref _meshObjectBuffer, _meshObjects, 72);
        CreateComputeBuffer(ref _vertexBuffer, _vertices, 12);
        CreateComputeBuffer(ref _indexBuffer, _indices, 4);
    }


    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
        where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null)
        {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }
        if (data.Count != 0)
        {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }
            // Set data on the buffer
            buffer.SetData(data);
        }
    }
    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            RayTracingShader.SetBuffer(0, name, buffer);
        }
    }
    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }
    private void SetShaderParameters()
    {
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);

        SetComputeBuffer("_MeshObjects", _meshObjectBuffer);
        SetComputeBuffer("_Vertices", _vertexBuffer);
        SetComputeBuffer("_Indices", _indexBuffer);

        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
    }
    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        RebuildMeshObjectBuffers();
        Render(source, destination);
    }
    private void Render(RenderTexture src, RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();
        // Set the target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        // Blit the result texture to the screen



        //Graphics.Blit(_target, destination);
        // Blit the result texture to the screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        //Graphics.Blit(_target, destination, _addMaterial);
        //Graphics.Blit(src, destination);
        
        
        Graphics.Blit(src, destination);
        
        Shader.SetGlobalTexture("_RtReflection", _target);
        _currentSample++;
    }
    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();
            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
            _currentSample = 0;
        }
    }


    private void OnDestroy()
    {
        _meshObjectBuffer.Release();
        _vertexBuffer.Release();
        _indexBuffer.Release();
    }

}