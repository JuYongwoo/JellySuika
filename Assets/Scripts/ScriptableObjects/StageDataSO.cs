using UnityEngine;

[CreateAssetMenu(menuName = "StageDataSO", fileName = "StageDataSO")]
public class StageDataSO : ScriptableObject
{
    [HideInInspector]
    public int Score = 0;
}
