using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Company : MonoBehaviour
{
    public Platoon targetPlatoon; // 중대 타겟
    public List<Platoon> platoonList; // 중대가 보유한 소대 리스트
    public Platoon companyCommander; // 중대 지휘 소대
    public int companyNum; // 중대 번호
    public GameObject obj;
}