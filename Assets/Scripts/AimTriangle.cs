using UnityEngine;

public class TriangleGenerator : MonoBehaviour
{
    public float baseLength = 1f; // Length of the triangle base
    public float height = 1f; // Height of the triangle
    public float tipAngleDegrees = 30f; // Angle of the triangle's tip in degrees
    public bool _regenerate = false;

    private GameObject generatedTriangle; // Reference to the generated triangle GameObject

    void Start()
    {
        
    }

    void Update()
    {
        // Example: Trigger regeneration of the triangle on a key press (e.g., R key)
        if (_regenerate)
        {
            DestroyTriangle();
            GenerateTriangle();
        }
    }

    public void GenerateTriangle()
    {
        Mesh mesh = new Mesh();

        // Calculate the half-width of the triangle base
        float halfWidth = baseLength / 2f;

        // Calculate the height of the tip of the triangle based on the tip angle
        float tipHeight = Mathf.Tan(Mathf.Deg2Rad * tipAngleDegrees) * halfWidth;

        // Define vertices of the triangle
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-halfWidth, 0f, 0f), // Left vertex
            new Vector3(halfWidth, 0f, 0f), // Right vertex
            new Vector3(0f, tipHeight, 0f) // Top vertex (tip)
        };

        // Define triangles of the triangle
        int[] triangles = new int[]
        {
            0, 1, 2 // Indices of vertices forming the triangle
        };

        // Define UV coordinates
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0f, 0f), // Left vertex
            new Vector2(1f, 0f), // Right vertex
            new Vector2(0.5f, 1f) // Top vertex (tip)
        };

        // Assign vertices, triangles, and UVs to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        // Recalculate normals and bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Create a new GameObject for the triangle
        generatedTriangle = new GameObject("GeneratedTriangle");

        // Assign the mesh to a MeshFilter component
        MeshFilter meshFilter = generatedTriangle.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Add a MeshRenderer component so the triangle is visible
        MeshRenderer meshRenderer = generatedTriangle.AddComponent<MeshRenderer>();

        // Load the material named "Glass" from the Resources folder
        Material glassMaterial = Resources.Load<Material>("Material/Glass");

        // Assign the material to the MeshRenderer component
        meshRenderer.sharedMaterial = glassMaterial;

        // Set the position of the generated triangle
        generatedTriangle.transform.position = transform.position;
        generatedTriangle.transform.rotation = transform.rotation;
        _regenerate = false;
    }

    void DestroyTriangle()
    {
        if (generatedTriangle != null)
        {
            Destroy(generatedTriangle);
        }
    }
}
