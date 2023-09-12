using UnityEngine;

namespace Week01
{
    public class RayTracingRenderer : MonoBehaviour
    {
        // scene elements
        public Camera camera;
        public GameObject screen;
        public int screenPixels = 64; // careful, don't make it too large!
        public Light lightSource;
        
        // all objects will have the same color
        public Color objectColor = Color.white;
        // ray misses will have this color
        public Color backgroundColor = Color.black;
             
        // for recursive ray tracing, we mix the color of the current object by the reflection color with this factor
        [Range(0.0f, 1.0f)]
        public float reflectiveness = 0.5f;
        // maximum number or bouces, so we stop recursion at some point!
        [Range(0, 5)]
        public int maxBounces = 3;

        // should we render the debug lines with all ray hits?
        public bool drawDebugLines = true;

        // private objects to handle Unity texture and our buffer of colors (to store our image until we give it to texture)
        Texture2D texture;
        Color32[] colorBuffer;

        void Start()
        {
            // initialize our private objects
            colorBuffer = new Color32[screenPixels * screenPixels];
            texture = new Texture2D(screenPixels, screenPixels);
            texture.filterMode = FilterMode.Point;

            // set the texture in our screen material
            Material screenMaterial = screen.GetComponent<Renderer>().material;
            screenMaterial.SetTexture("_MainTex", texture);
        }

        void Update()
        {
            ClearBuffers();

            Vector3 step = new Vector3(1f / screenPixels, 1f / screenPixels, 0f);
            for (int i = 0; i < screenPixels; i++)
            {
                for (int j = 0; j < screenPixels; j++)
                {
                    // Find appropriate pixel location in the space of the screen quad
                    // (Assumes quad is 1x1 units, with normal == local -z)
                    var pixelPosition = screen.transform.position + new Vector3(step.x * i - 0.5f, step.y * j - 0.5f, 0);

                    // Create primary ray (from camera to scene, though pixel (i,j))
                    var position = camera.transform.position;
                    var rayDirection = (pixelPosition - position).normalized;
                    var primaryRay = new Ray(position, rayDirection);

                    // Send your ray to the IntersectionTest function
                    var intersectionColor = IntersectionTest(primaryRay);

                    // Set color buffer
                    colorBuffer[j * screenPixels + i] = (Color32) intersectionColor;
                }
            }
            // Set the material texture to our buffer
            texture.SetPixels32(colorBuffer);
            texture.Apply();
        }
        
        Color IntersectionTest(Ray ray, int bounces = 0)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Color color = ComputeLighting(hit.point, hit.normal);
                
                // Optional: Recursive Ray Tracing for reflections
                if (bounces < maxBounces)
                {
                    var reflectionDir = Vector3.Reflect(ray.direction, hit.normal);
                    var reflectionRay = new Ray(hit.point + hit.normal * 0.01f, reflectionDir);
                    var reflectionColor = IntersectionTest(reflectionRay, bounces + 1);
                    color = Color.Lerp(color, reflectionColor, reflectiveness);
                }

                if (drawDebugLines)
                    Debug.DrawLine(ray.origin, hit.point, color);

                return color;
            }
            return backgroundColor;
        }

        Color ComputeLighting(Vector3 position, Vector3 normal)
        {
            // Optional: Shadow Ray
            var shadowRay = new Ray(position + normal * 0.01f, -lightSource.transform.forward);
            if (Physics.Raycast(shadowRay))
            {
                return Color.black;  // Point is in shadow
            }

            var diffuseIntensity = Mathf.Max(0, Vector3.Dot(normal, -lightSource.transform.forward));
            var color = (diffuseIntensity * objectColor * lightSource.color);
            return color;
        }

        // reset the texture color to backgroundColor
        void ClearBuffers()
        {
            for (int i = 0; i < colorBuffer.Length; i++)
            {
                colorBuffer[i] = backgroundColor;
            }
        }

    }
}