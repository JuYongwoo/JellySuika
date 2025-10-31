using JYW.JellySuika.Managers;
using JYW.JellySuika.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JYW.JellySuika.UIs{

public class ScorePanel : MonoBehaviour
{

    private enum ScorePanelObjs
    {
        ScoreText
    }

    private Dictionary<ScorePanelObjs, GameObject> ScorePanelObjsMap;

    private void Awake()
    {
        ScorePanelObjsMap = Util.MapEnumChildObjects<ScorePanelObjs, GameObject>(gameObject);
    }
    private void Start()
    {


        ManagerObject.instance.eventManager.SetScoreTextEvent -= SetText;
        ManagerObject.instance.eventManager.SetScoreTextEvent += SetText;

        //
        //actionmanager에 바인딩
        //

        //
    }

    private void OnDestroy()
    {
        ManagerObject.instance.eventManager.SetScoreTextEvent -= SetText;

    }

    private void SetText(int score)
    {
        ScorePanelObjsMap[ScorePanelObjs.ScoreText].GetComponent<Text>().text = $"Score : {score}";
    }



    //받은 int 점수로 text를 수정하는 함수
    //그 함수를 actionmanager에 -= += 바인딩
    //destory 시 -=로 바인딩 해제

} }
