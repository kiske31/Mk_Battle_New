﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;


// 소대 배열에서 어디에 위치해있는지 이동의 기준이 되는 소대장 유닛(우측 가운데)은 누구인지
public enum UnitPos
{ 
    RIGHT_UP = 0, // 우측 상단 모서리
    RIGHT_DOWN, // 우측 하단 모서리
    CENTER, // 중앙
    LEFT_UP, // 좌측 상단 모서리
    LEFT_DOWN, // 좌측 하단 모서리
    RIGHT, // 우측면 외곽
    LEFT, // 좌측면 외곽
    UP, // 윗면 외곽
    DOWN, // 하면 외곽
    POS_UNKNOW // 아직 위치가 정해지지 않음
}

public class Unit : MonoBehaviour
{
    public MainGame mainGame;

    public int tileIdxX; // 유닛 인덱스 X
    public int tileIdxY; // 유닛 인덱스 Y

    public int platoonIdxX; // 유닛이 소대안에서의 배열 인덱스 X
    public int platoonIdxY; // 유닛이 소대안에서의 배열 인덱스 Y

    public UnitPos platoonPos; // 유닛이 소대의 외곽인지, 내부인지, 소대장인지 배치 디렉션

    public Unit platoonCommander; // 부대장 유닛 (본인일 수 있음)

    public bool isRed; // 레드팀인가? 

    public int platoonNum; // 소대 번호

    public Platoon platoon; // 소속 소대

    // 유닛 초기화
    public void UnitInit(MainGame mainGame, PlatoonMos mos, PlatoonSize size, Platoon platoon)
    {
        this.mainGame = mainGame;
        platoonCommander = null;

        this.platoon = platoon;

        Tile currentTile = mainGame.GetTileByIdx(tileIdxX, tileIdxY);
        currentTile.SetObject(this);
        this.transform.position = new Vector3(currentTile.posX, currentTile.posY, 0);
    }
}