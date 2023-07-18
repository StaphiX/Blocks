using UnityEngine;
using System.Collections;

public class Main : MonoBehaviour
{
    // Manages Data in the 3d world - scene objects - spacial partitioning
    public static WorldData WorldData { get; set; }

    // Handles interaction with the 3d world - tools - adding - removing
    public static WorldManager WorldManager { get; set; }

    // Also handles world data - should be merged with world data
    public static SceneManager SceneManager { get; set; }

    public static UIManager UIManager { get; set; }

    void Awake()
    {
        WorldData = new WorldData();
        WorldManager = new WorldManager();
        SceneManager = new SceneManager();
        UIManager = new UIManager();
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(SceneManager != null)
            SceneManager.Update();
        if (WorldManager != null)
            WorldManager.Update();
    }
}

