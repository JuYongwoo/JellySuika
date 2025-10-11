using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitlePanelUI : MonoBehaviour
{

    enum TitlePanelUIObjs
    {
        TitlePlayBtn,
        HowToPlayBtn,
        HowToPlayImg,
        HowToPlayExitBtn
    }

    private Dictionary<TitlePanelUIObjs, GameObject> titlePanelUIObjMap;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        titlePanelUIObjMap = Util.MapEnumChildObjects<TitlePanelUIObjs, GameObject>(gameObject);
    }

    private void Start()
    {
        titlePanelUIObjMap.TryGetValue(TitlePanelUIObjs.HowToPlayImg, out var howToPlayImg);
        howToPlayImg.SetActive(false); //게임 시작 시 게임방법 창 닫기


        titlePanelUIObjMap.TryGetValue(TitlePanelUIObjs.TitlePlayBtn, out var playBtn);
        playBtn.GetComponent<Button>().onClick.AddListener(     () => { SceneManager.LoadScene("Stage");      }   );
        
        titlePanelUIObjMap.TryGetValue(TitlePanelUIObjs.HowToPlayBtn, out var howToPlayBtn);
        howToPlayBtn.GetComponent<Button>().onClick.AddListener(     () => {
            howToPlayImg.SetActive(true);
        });

        titlePanelUIObjMap.TryGetValue(TitlePanelUIObjs.HowToPlayExitBtn, out var exitBtn);
        exitBtn.GetComponent<Button>().onClick.AddListener(     () => {

            howToPlayImg.SetActive(false);


        });
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
}
