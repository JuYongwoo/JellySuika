using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitlePanelUI : MonoBehaviour
{

    enum TitlePanelUIObjs
    {
        TitlePlayBtn
    }

    private Dictionary<TitlePanelUIObjs, GameObject> titlePanelUIObjMap;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        titlePanelUIObjMap = Util.MapEnumChildObjects<TitlePanelUIObjs, GameObject>(gameObject);
    }

    private void Start()
    {
        titlePanelUIObjMap.TryGetValue(TitlePanelUIObjs.TitlePlayBtn, out var btn);
        btn.GetComponent<Button>().onClick.AddListener(     () => { SceneManager.LoadScene("Stage");      }   );
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
}
