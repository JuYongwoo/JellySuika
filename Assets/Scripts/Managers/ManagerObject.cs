using UnityEngine;

public class ManagerObject : MonoBehaviour
{
    public static ManagerObject instance;
    public InputManager inputManager = new InputManager();
    public ResourceManager resourceManager = new ResourceManager();
    public ActionManager actionManager = new ActionManager();
    public SoundManager soundManager = new SoundManager();
    public PoolManager poolManager = new PoolManager();

    private void Awake()
    {
        
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
            DontDestroyOnLoad(gameObject);

        resourceManager.Init();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        inputManager.Update();
    }
}
