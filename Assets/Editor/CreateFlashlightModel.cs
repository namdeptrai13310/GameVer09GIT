using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CreateFlashlightModel
{
    [MenuItem("Tools/Generate 3D Flashlight")]
    public static void Generate()
    {
        string dirPath = "Assets/Models/Flashlight";
        if (!AssetDatabase.IsValidFolder("Assets/Models"))
        {
            AssetDatabase.CreateFolder("Assets", "Models");
        }
        if (!AssetDatabase.IsValidFolder(dirPath))
        {
            AssetDatabase.CreateFolder("Assets/Models", "Flashlight");
        }

        // 1. Create Mesh
        Mesh mesh = new Mesh();
        mesh.name = "Flashlight_Mesh";

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> bodyTriangles = new List<int>();
        List<int> lensTriangles = new List<int>();

        int segments = 24;

        // Profile along Z axis (from back z = -0.15 to front z = 0.08)
        // Z coordinates and corresponding Radii
        float[] zCoords = new float[] {
            -0.12f, // 0: Tail cap base
            -0.11f, // 1: Tail cap bevel
            -0.10f, // 2: Tail cap end
            -0.09f, // 3: Grip start
             0.00f, // 4: Grip end
             0.01f, // 5: Neck
             0.04f, // 6: Head expand
             0.07f, // 7: Head cylinder
             0.075f // 8: Front rim
        };

        float[] radii = new float[] {
            0.016f, // 0
            0.018f, // 1
            0.017f, // 2
            0.015f, // 3
            0.015f, // 4
            0.016f, // 5
            0.026f, // 6
            0.026f, // 7
            0.024f  // 8
        };

        // Build rings
        for (int r = 0; r < zCoords.Length; r++)
        {
            float z = zCoords[r];
            float rad = radii[r];
            float vCoord = (float)r / (zCoords.Length - 1);

            for (int s = 0; s <= segments; s++)
            {
                float angle = (float)s / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * rad;
                float y = Mathf.Sin(angle) * rad;

                vertices.Add(new Vector3(x, y, z));
                normals.Add(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized);
                uvs.Add(new Vector2((float)s / segments, vCoord));
            }
        }

        int ringVertCount = segments + 1;

        // Connect rings for body
        for (int r = 0; r < zCoords.Length - 1; r++)
        {
            int rowA = r * ringVertCount;
            int rowB = (r + 1) * ringVertCount;

            for (int s = 0; s < segments; s++)
            {
                int a = rowA + s;
                int b = rowB + s;
                int c = rowA + s + 1;
                int d = rowB + s + 1;

                bodyTriangles.Add(a);
                bodyTriangles.Add(c);
                bodyTriangles.Add(b);

                bodyTriangles.Add(b);
                bodyTriangles.Add(c);
                bodyTriangles.Add(d);
            }
        }

        // Tail cap flat back
        int backCenterIdx = vertices.Count;
        vertices.Add(new Vector3(0f, 0f, zCoords[0]));
        normals.Add(new Vector3(0f, 0f, -1f));
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int s = 0; s < segments; s++)
        {
            int cur = s;
            int next = s + 1;

            bodyTriangles.Add(backCenterIdx);
            bodyTriangles.Add(next);
            bodyTriangles.Add(cur);
        }

        // Lens at the front (recessed slightly at z = 0.07f)
        float lensZ = 0.07f;
        float lensRadius = 0.023f;
        int lensCenterIdx = vertices.Count;
        vertices.Add(new Vector3(0f, 0f, lensZ));
        normals.Add(new Vector3(0f, 0f, 1f));
        uvs.Add(new Vector2(0.5f, 0.5f));

        int lensRingStart = vertices.Count;
        for (int s = 0; s <= segments; s++)
        {
            float angle = (float)s / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * lensRadius;
            float y = Mathf.Sin(angle) * lensRadius;

            vertices.Add(new Vector3(x, y, lensZ));
            normals.Add(new Vector3(0f, 0f, 1f));
            uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
        }

        for (int s = 0; s < segments; s++)
        {
            int cur = lensRingStart + s;
            int next = lensRingStart + s + 1;

            lensTriangles.Add(lensCenterIdx);
            lensTriangles.Add(cur);
            lensTriangles.Add(next);
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(bodyTriangles, 0);
        mesh.SetTriangles(lensTriangles, 1);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        AssetDatabase.CreateAsset(mesh, dirPath + "/FlashlightMesh.asset");

        // 2. Create Materials
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        // Body Material
        Material matBody = new Material(urpLit);
        matBody.name = "M_Flashlight_Body";
        matBody.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.13f, 1f)); // Dark tactical metal
        matBody.SetFloat("_Metallic", 0.85f);
        matBody.SetFloat("_Smoothness", 0.45f);
        AssetDatabase.CreateAsset(matBody, dirPath + "/M_Flashlight_Body.mat");

        // Lens Material
        Material matLens = new Material(urpLit);
        matLens.name = "M_Flashlight_Lens";
        matLens.SetColor("_BaseColor", new Color(0.95f, 0.98f, 1f, 1f));
        matLens.SetFloat("_Metallic", 0.1f);
        matLens.SetFloat("_Smoothness", 0.95f);
        matLens.EnableKeyword("_EMISSION");
        matLens.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.85f) * 2.5f);
        AssetDatabase.CreateAsset(matLens, dirPath + "/M_Flashlight_Lens.mat");

        // 3. Create Prefab
        GameObject go = new GameObject("Flashlight_Model");
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new Material[] { matBody, matLens };

        // Add Light child
        GameObject lightGo = new GameObject("Flashlight_SpotLight");
        lightGo.transform.SetParent(go.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0f, 0.08f);
        lightGo.transform.localRotation = Quaternion.identity;

        Light spot = lightGo.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = 25f;
        spot.spotAngle = 55f;
        spot.innerSpotAngle = 35f;
        spot.color = new Color(1f, 0.96f, 0.88f);
        spot.intensity = 2.2f;
        spot.shadows = LightShadows.Soft;

        PrefabUtility.SaveAsPrefabAsset(go, dirPath + "/Flashlight_Model.prefab");
        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created Flashlight 3D Model Prefab successfully at " + dirPath + "/Flashlight_Model.prefab");
    }
}
