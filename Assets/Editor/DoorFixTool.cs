using UnityEditor;
using UnityEngine;

// Dat file nay vao 1 folder ten "Editor" (tao moi neu chua co)
// Cach dung: trong Hierarchy, chon (Ctrl/Shift-click) tat ca cac cua bi loi,
// sau do vao menu Tools > Fix Selected Doors
public class DoorFixTool : Editor
{
    [MenuItem("Tools/Fix Selected Doors")]
    private static void FixSelectedDoors()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length == 0)
        {
            Debug.LogWarning("Chua chon cua nao. Hay chon cac object cua truoc trong Hierarchy.");
            return;
        }

        int missingRemoved = 0;
        int scriptAdded = 0;
        int colliderFixed = 0;

        foreach (GameObject door in selected)
        {
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(door);
            missingRemoved += removedCount;

            if (door.GetComponent<ClickDoor>() == null)
            {
                door.AddComponent<ClickDoor>();
                scriptAdded++;
            }

            // Dung MeshCollider thay vi BoxCollider - tranh loi "does not support negative scale"
            // xay ra voi cac cua bi mirror (scale am) de tao ban trai/phai
            Collider col = door.GetComponent<Collider>();

            if (col is BoxCollider badBox)
            {
                // Neu truoc do lo gan BoxCollider tren object bi scale am, xoa di va thay bang MeshCollider
                Object.DestroyImmediate(badBox);
                col = null;
            }

            if (col == null)
            {
                MeshFilter mf = door.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider meshCol = door.AddComponent<MeshCollider>();
                    meshCol.sharedMesh = mf.sharedMesh;
                    meshCol.convex = false;
                    col = meshCol;
                }
                else
                {
                    col = door.AddComponent<BoxCollider>();
                }
                colliderFixed++;
            }

            if (col.isTrigger)
            {
                col.isTrigger = false;
                colliderFixed++;
            }

            EditorUtility.SetDirty(door);
        }

        Debug.Log($"Hoan tat: da xoa {missingRemoved} script loi, gan ClickDoor cho {scriptAdded} cua, sua Collider cho {colliderFixed} cua.");
    }
}