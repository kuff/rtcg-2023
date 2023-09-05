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
                    // TODO
                    // find appropriate pixel location in the space of the screen quad
                    // (assumes quad is 1x1 units, with normal == local -z)

                    // TODO
                    // create primary (from camera to scene, though pixel (i,j) ) ray 

                    // TODO
                    // send your ray to the IntersectionTest function
                    colorBuffer[j * screenPixels + i] = (Color32) backgroundColor;// IntersectionTest(ray, maxBounces);

                }
            }
            // set the material texture to our buffer
            texture.SetPixels32(colorBuffer);
            texture.Apply();
        }


        
        Color IntersectionTest(Ray ray, int bounces = 0) {
            RaycastHit hit;
            // we take advantage of the built in intersection test in Unity physics, so that we don't have to implement
            // our own. The limitation is that this will only support simple geometry.
            if (Physics.Raycast(ray, out hit))
            {
                // hit! Compute color for this intersection
                Color color = ComputeLighting(hit.point, hit.normal);


                // Optional feature: here you can implement recursive intersection test for rendering reflections, until bounces == 0

                // draw the rays on the editor, to help you visualizing and debugging
                if (drawDebugLines)
                    Debug.DrawLine(ray.origin, hit.point, color);

                return color;
            }
            // miss, return background color
            return backgroundColor;
        }


        Color ComputeLighting(Vector3 position, Vector3 normal)
        {
            // Optional feature: shadows can be computer here. With ray tracing, we can use a shadow ray, that is,
            // a ray from the intersection position towards the light. If the light is occluded by geometry,
            // this means that this point is in shadow.


            // Light computation with diffuse and ambient contributions
            float diffuseIntensity = Mathf.Max(0, Vector3.Dot(normal, -lightSource.transform.forward));
            Color color = (diffuseIntensity * objectColor * lightSource.color);

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