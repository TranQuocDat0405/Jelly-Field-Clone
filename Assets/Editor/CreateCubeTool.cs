using UnityEditor;
using UnityEngine;

public static class CreateCubeTool
{
    [MenuItem("Tools/Create Cube")]
    public static void CreateCube()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        cube.transform.position = Vector3.zero;

        Selection.activeGameObject = cube;
        SceneView.FrameLastActiveSceneView();

        Debug.Log("Cube created at (0, 0, 0)");
    }
}
