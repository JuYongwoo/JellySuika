using UnityEngine;

public class ManagerObject : MonoBehaviour
{
    public static ManagerObject instance;
    public InputManager inputManager = new InputManager();
    public ResourceManager resourceManager = new ResourceManager();

    private void Awake()
    {
        
        if(instance != null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
            DontDestroyOnLoad(gameObject);





    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
