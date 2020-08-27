using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    public int idxX; // 타일 인덱스 X
    public int idxY; // 타일 인덱스 Y

    public float posX; // 실제 좌표 X
    public float posY; // 실제 좌표 Y

    public bool isHaveUnit; // 현재 타일에 유닛이 존재하는가?
    public Unit haveUnit; // 존재하는 유닛

    // 타일에 존재하는 유닛 세팅
    public void SetObject(Unit obj)
    {
        haveUnit = obj;
        if (obj == null) isHaveUnit = false;
        else isHaveUnit = true;
    }
}
