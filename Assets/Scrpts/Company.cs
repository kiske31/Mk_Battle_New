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
    public List<PathStruct> pathList;
    public bool newPathFind; // 새로운 패스파인드를 할 수 있는가?
    public bool needPathFind; // 패스파인드를 할 필요가 있는가?
    public int companyDestX;
    public int companyDestY;
}

public struct PathStruct
{
    public float H;
    public Tile tile;
    public Platoon platoon;
    public PlatoonMoveDirection dir;
    public int targetY;
    public int targetX;
}