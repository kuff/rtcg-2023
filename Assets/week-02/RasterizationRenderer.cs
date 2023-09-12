using System.Collections.Generic;
using UnityEngine;

namespace Week02
{
    public class RasterizationRenderer : MonoBehaviour
    {
        // Scene elements
        public Camera camera;

        public Light lightSource;
        // ambient "light" is used to fake global illumination (discussed later in the course)
        [Range(0.0f, 1.0f)]
        public float ambientIntensity = 0.1f;

        // - Objects to be rendered, to make our life easier, we will render everything that has a colider and MeshFilter enabled
        List<GameObject> allObjects;
        // - to make our life easier, all objects will have the same color
        public Color objectColor = Color.white;
        
        public GameObject screen;
        [Range(32, 512)]
        public int screenPixels = 64; // careful, don't make it too large!
        public Color backgroundColor = Color.black; // The screen background will have this color


        // This is just to help us selecting whether we want to render with points or triangles
        // - Line rasterization is not implemented. This is a challenge for you ;)
        public enum RasterizationMode { points =0, lines = 1, tringles = 2 };
        public RasterizationMode rasterizationMode = RasterizationMode.points;

        // private objects to handle Unity texture and our buffers
        // - texture is the generated image in a format that unity can understand
        // - colorBuffer is where we store the current color of a pixel
        // - depthBuffer is where we store the depth of the pixel that is drawn in the colorBuffer.
        //   we use this to determine if a new candidate pixel/fragment is closer (and should be drawn) or
        //   further (and should be discarded) from the camera than the pixel that is currently in the color Buffer
        Texture2D texture;
        Color32[] colorBuffer;
        float[] depthBuffer;




        // Start is called before the first frame update
        void Start()
        {
            // initialize our private objects
            colorBuffer = new Color32[screenPixels * screenPixels];
            depthBuffer = new float[screenPixels * screenPixels];
            texture = new Texture2D(screenPixels, screenPixels);
            texture.filterMode = FilterMode.Point;

            // set the texture in our screen material
            Material screenMaterial = screen.GetComponent<Renderer>().material;
            screenMaterial.SetTexture("_MainTex", texture);

            // make a list of all objects with a collider and meshfilter
            allObjects = new List<GameObject>();
            Collider[] allColliders = UnityEngine.Object.FindObjectsOfType<Collider>();
            for (int i = 0; i < allColliders.Length; i++)
            {
                MeshFilter meshFilter = allColliders[i].gameObject.GetComponent<MeshFilter>();
                if (meshFilter && allColliders[i].enabled)
                    allObjects.Add(allColliders[i].gameObject);
            }
        }

        // Update is called once per frame
        void Update()
        {
            ClearBuffers(); // reset screen colors and depth buffer            

            for (int oi = 0; oi < allObjects.Count; oi++)
            {
                Mesh mesh = allObjects[oi].GetComponent<MeshFilter>().mesh;
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                int[] indices = mesh.triangles;


                // Compute the Model View Projection (MVP) matrix
                Matrix4x4 model = allObjects[oi].transform.localToWorldMatrix; // get the localToWorldMatrix for object transform at index "oi"
                Matrix4x4 view = camera.transform.worldToLocalMatrix; // get the worldToLocalMatrix from the camera transform
                Matrix4x4 projection = camera.projectionMatrix; // get the projectionMatrix from the camera 
                Matrix4x4 MVP = projection * view * model; //multiply the three matrices in the right order

                // special matrix to transform the normals
                Matrix4x4 modelNormal = (model).transpose.inverse;
                

                for (int vi = 0; vi < vertices.Length; vi++)
                {
                    Vector3 vertexIn = vertices[vi];
                    Vector3 normalIn = normals[vi];

                    // Thesse 2 lines are equivalent to the basic functionality of a vertex shader
                    Vector4 vertexOut = MVP * new Vector4(vertexIn.x, vertexIn.y, vertexIn.z, 1f);
                    Vector3 normalOut = modelNormal * normalIn;

                    // perspective division
                    vertexOut.x /= -vertexOut.w;
                    vertexOut.y /= -vertexOut.w;
                    vertexOut.z /= -vertexOut.w;

                    // to screen coordinates
                    vertexOut.x = (vertexOut.x + 1f) * .5f * (float)screenPixels;
                    vertexOut.y = (vertexOut.y + 1f) * .5f * (float)screenPixels;
                    vertexOut.z = (vertexOut.z + 1f) * .5f * (float)screenPixels; // not necessary, here only for visualization purpuses

                    vertices[vi] = vertexOut; // implicit conversion to Vector3
                    normals[vi] = normalOut;
                }

                if (rasterizationMode == RasterizationMode.points)
                {
                    for (int vi = 0; vi < vertices.Length; vi++)
                    {
                        RasterizePoint(vertices[vi], normals[vi]);
                    }
                }

                else if (rasterizationMode == RasterizationMode.lines)
                {
                    for (int ti = 0; ti < indices.Length; ti += 2)
                    {
                        var v1 = vertices[indices[ti]];
                        var v2 = vertices[indices[ti + 1]];
                        var n = (normals[indices[ti]] + normals[indices[ti + 1]]) / 2f;

                        RasterizeLine(v1, v2, n);
                    }
                }

                else if (rasterizationMode == RasterizationMode.tringles)
                {
                    for (int ti = 0; ti < indices.Length; ti += 3)
                    {
                        var v1 = vertices[indices[ti]];
                        var v2 = vertices[indices[ti + 1]];
                        var v3 = vertices[indices[ti + 2]];
                        var n = (normals[indices[ti]] + normals[indices[ti + 1]] + normals[indices[ti + 2]]) / 3f;

                        RasterizeTriangle(v1, v2, v3, n);
                    }
                }
            }

            texture.SetPixels32(colorBuffer);
            texture.Apply();
        }

        void RasterizePoint(Vector3 position, Vector3 normal)
        {
            var vtx = Vector2Int.RoundToInt(position); ;
            if (vtx.x >= 0 && vtx.x < screenPixels && vtx.y >= 0 && vtx.y < screenPixels)
            {
                // z/depth buffer testing
                int idx = vtx.y * screenPixels + vtx.x;
                if (depthBuffer[idx] > position.z)
                {
                    depthBuffer[idx] = position.z;
                    colorBuffer[idx] = ComputeLighting(normal);
                }
            }
        }


        void RasterizeLine(Vector3 v1, Vector3 v2, Vector3 normal)
        {
            // - Line rasterization is not implemented. This is a challenge for you ;)
        }


        void RasterizeTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal)
        {
            // I compute the z/depth as an average of the z/depth of the 3 vertices
            // - the avarage z is a huge simplification of a triangle distance and is will cause visible artifacts
            // - the correct would be to compute the distance of each pixel
            float z = (v1.z + v2.z + v3.z) / 3f;

            // I delimit the range of pixels to check during rasterization
            Vector2 min = Vector2.Min(v1, Vector2.Min(v2, v3));
            Vector2 max = Vector2.Max(v1, Vector2.Max(v2, v3));
            int xmin = Mathf.Max(0, (int)(min.x -1f)); 
            int xmax = Mathf.Min(screenPixels, (int)(max.x + 1f));
            int ymin = Mathf.Max(0, (int)(min.y -1f)); 
            int ymax = Mathf.Min(screenPixels, (int)(max.y + 1f));

            // Iterate all pixels in the square where this triangle may overlap
            for (int x = xmin; x < xmax; x++)
            {
                for (int y = ymin; y < ymax; y++)
                {
                    Vector2 pixelPos = new Vector2(x, y);

                    // Check point/triangle intersection
                    // - intersect will be true if this pixel is inside the triangle
                    float d1 = Sign(pixelPos, v1, v2);
                    float d2 = Sign(pixelPos, v2, v3);
                    float d3 = Sign(pixelPos, v3, v1);

                    bool has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                    bool has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
                    bool intersect = !(has_neg && has_pos);

                    // We skip this pixel/fragment if it's outside the triangle
                    // - continue goes to the next iteration of the inner for loop
                    if (!intersect) continue; 

                    // Z/depth buffer testing
                    // - the z/depth buffer holds information about the closest object on this screen pixel
                    // - if this new pixel/fragment is closer than what is currently stored in the depth buffer, we replace the pixel in the buffers and compute lighting
                    int idx = y * screenPixels + x;
                    if (depthBuffer[idx] > z)
                    {
                        depthBuffer[idx] = z;
                        // we do coloring on visible pixels/fragments, which is equivalent to a pixel/fragment shader
                        colorBuffer[idx] = (Color32)ComputeLighting(normal);
                    }
                }
            }
        }

        float Sign(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            // check if v1 is on the left or right of the line from v3 to v2 (in the xy-plane)
            return (v1.x - v3.x) * (v2.y - v3.y) - (v2.x - v3.x) * (v1.y - v3.y);
        }

        Color ComputeLighting(Vector3 normal)
        {
            // light computation with diffuse and ambient contributions
            float diffuseIntensity = Mathf.Max(0, Vector3.Dot(normal, -lightSource.transform.forward)) * (1f - ambientIntensity);
            Color color = (diffuseIntensity * objectColor * lightSource.color) + (objectColor * ambientIntensity);

            return color;
        }

        void ClearBuffers()
        {
            // This will erase all information in the depth and color buffers, so that we can start rendering new images
            for (int i = 0; i < colorBuffer.Length; i++)
            {
                colorBuffer[i] = backgroundColor;
                depthBuffer[i] = float.MaxValue;
            }
        }

    }
}
