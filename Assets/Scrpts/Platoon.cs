﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuickPool;
using UnityEditor;
using System.Linq;
using System;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
//using UnityEditor.Experimental.GraphView;
//using UnityEditorInternal;

// 소대 크기
public enum PlatoonSize
{ 
    ONE = 1, // 1명
    THOUSAND = 3, // 천명
    FIVE_THOUSAND = 4, // 5천명
    TEN_THOUSAND = 5 // 만명
}

// 소대 스테이터스
public enum PlatoonStatus
{
    UNIT_IDLE = 0, // 유닛 정지
    UNIT_ATTACK, // 유닛 공격
    UNIT_ATTACK_READY,
    UNIT_FIND_MOVE, // 중대 타겟으로 이동
    UNIT_ATTACK_MOVE, // 소대 타겟으로 이동
    UNIT_MOVE, // 유닛 이동 (부대장 유닛의 타겟도 없기 때문에 부대장 유닛의 무브를 따름)
    UNIT_STATUS_NUM
}

// 소대 병종
public enum PlatoonMos
{
    FOOTMAN = 0, // 보병
    SPEARMAN, // 창병
    ARCHER, // 궁병
    KNIGHT, // 기사
    UP_KNIGHT, // 위쪽 열에 배치된 기사 (기사 병종만 특별 이동 루틴을 사용하므로 따로 처리)
    DOWN_KNIGHT, // 아랫쪽 열에 배치된 기사 (기사 병종만 특별 이동 루틴을 사용하므로 따로 처리)
}

// 소대 이동 속도
public enum PlatoonMoveSpeed
{ 
    TICK_5,
    TICK_10,
    TICK_15
}

// 소대 이동 방향 (8방향)
public enum PlatoonMoveDirection
{
    DIR_RIGHT = 0, // 우방향
    DIR_RIGHT_DOWN, // 우아래 방향
    DIR_DOWN, // 아래 방향
    DIR_LEFT_DOWN, // 좌아래 방향
    DIR_LEFT, // 좌방향
    DIR_LEFT_UP, // 좌상 방향
    DIR_UP, // 윗 방향
    DIR_RIGHT_UP, // 우상 방향
    DIR_DEFAULT // 방향 없음. 보통 제자리?
}

public enum PlatoonPathFind
{ 
    MOVE_PATH_FIND = 0, // 일반적인 이동 상황에서의 패스파인드
    ATTACK_MOVE_PATH_FIND, // 소대 공격 목표를 추적하기 위한 패스파인드
    FIND_MOVE_PATH_FIND // 중대 공격 목표를 추적하기 위한 패스파인드
}

public struct SearchArea
{
    public int tileIdxX;
    public int tileIdxY;
    public UnitPos pos;
}

public struct JPS_Struct
{
    public float g; // 현재 부터 타일까지 가중치
    public float h; // 타일부터 목표까지 가중치
    public float f; // g + h

    public Tile pTile; // 부모 타일
    public Tile tile;

    public PlatoonMoveDirection direciton;
}

public class Platoon : MonoBehaviour
{
    public const int BALANCE_DMG = 1; // 밸런스를 위한 데미지 감소 값

    public GameObject parentObj; // 하이어라키에서 소대들이 중대로 묶일 빈 오브젝트
    public List<Unit> unitList; // 소대의 유닛 리스트

    public MainGame mainGame; // 메인 게임

    public Platoon targetPlatoon; // 소대 타겟

    public int PlatoonTargetIdxX; // 타겟 좌표 X
    public int PlatoonTargetIdxY; // 타겟 좌표 Y

    public Unit platoonCommander; // 소대장 유닛

    public Platoon companyCommander; // 중대장 소대 (본인일 수 있습니다)

    public PlatoonMoveDirection platoonMoveDirection; // 이동 방향

    public PlatoonMoveDirection platoonBackDirection; // 이동 반대 방향

    public Action pathCheckCallback; // 이동 전 길찾기 체크 콜백

    public Action moveCallback; // 이동 후 콜백

    public Action attackCallback; // 공격 후 콜백

    public int platoonNum; // 소대 번호

    public PlatoonStatus platoonStatus; // 소대 스테이터스

    public PlatoonSize platoonSize; // 소대 규모

    public bool isRed; // 소대가 어디 소속인가?

    // public float speed; // 소대 속도

    public float hp; // 소대 체력

    public float mosAtk; // 병종에 따른 공격력
     
    public float atk; // 소대 공격력

    public float range; // 소대 사거리

    public float defaultHp; // 소대 초기 체력

    public float defaultAtk; // 소대 초기 공격력

    public PlatoonMos platoonMos; // 소대 병종

    public int companyNum; // 소속 중대 번호

    public Text companyText; // 소대 텍스트

    public int platoonLine; // 소대 배열 수

    public Dictionary<Tile, int> closeTileDic; // 패스 파인딩을 더 효과적으로 하기 위해서 지나간 길을 담아둘 딕셔너리 (지나갔던 타일, 지나쳤던 턴)

    public PlatoonMoveSpeed platoonMoveSpeed; // 1초에 사용할 이동 틱의 개수 (소대의 이동 속도가 됨)

    public Dictionary<Tile, int> closePathDic;

    List<PathStruct> battleAreaList;

    public bool newPathFind;

    List<PathStruct> dirList;

    List<Unit> checkUnitList;

    public List<PathStruct> platoonPath;

    public int startDestinationX;
    public int startDestinationY;

    public int platoonOffsetY;

    public int battleAreaX;
    public int battleAreaY;

    bool isTargetFind;

    public Tile prevTile;

    public float centerPosX;
    public float centerPosY;

    public int platoonPathCount = 0;

    // 패스파인딩 리스트
    List<SearchArea> searchListLeft;
    List<SearchArea> searchListRight;
    List<SearchArea> searchListUp;
    List<SearchArea> searchListDown;
    List<SearchArea> searchListLeftUp;
    List<SearchArea> searchListLeftDown;
    List<SearchArea> searchListRightUp;
    List<SearchArea> searchListRightDown;

    Dictionary<Tile, JPS_Struct> openDic;
    Dictionary<Tile, JPS_Struct> closeDic;

    bool finishJPS;

    // 소대 이닛 (메인게임, 병종, 소대의 크기, 시작 타일, 소대번호, 레드팀인가?)
    public void PlatoonInit(MainGame mainGame, PlatoonMos PlatoonMos, PlatoonSize num, int armyNum , Tile startTile, int platoonNum, int companyNum, bool isRed= true)
    {
        unitList = new List<Unit>();
        dirList = new List<PathStruct>();
        platoonPath = new List<PathStruct>();
        checkUnitList = new List<Unit>();

        searchListLeft = new List<SearchArea>();
        searchListRight = new List<SearchArea>();
        searchListUp = new List<SearchArea>();
        searchListDown = new List<SearchArea>();
        searchListLeftUp = new List<SearchArea>();
        searchListLeftDown = new List<SearchArea>();
        searchListRightUp = new List<SearchArea>();
        searchListRightDown = new List<SearchArea>();

        closeTileDic = new Dictionary<Tile, int>();
        closePathDic = new Dictionary<Tile, int>();
        battleAreaList = new List<PathStruct>();

        openDic = new Dictionary<Tile, JPS_Struct>();
        closeDic = new Dictionary<Tile, JPS_Struct>();

        string prefabName = "Red"; // 병사 오브젝트 프리펩
        int platoonCount = (int)num; // 배치될 병사의 수
        this.platoonLine = platoonCount; // 소대의 배열 수
        this.platoonNum = platoonNum;
        this.platoonStatus = PlatoonStatus.UNIT_IDLE;
        this.platoonSize = num;
        this.mainGame = mainGame;
        this.isRed = isRed;
        this.platoonMos = PlatoonMos;
        this.companyNum = companyNum;
        this.companyCommander = null;
        this.companyText = null;
        this.targetPlatoon = null;
        this.mosAtk = 2;
        this.newPathFind = true;
        this.isTargetFind = false;

        float tempHp = 10;

        this.range = 1;

        this.battleAreaX = 15;
        this.battleAreaY = 15;

        switch (PlatoonMos)
        {
            case PlatoonMos.FOOTMAN:
                prefabName = "Red";
                this.platoonMoveSpeed = PlatoonMoveSpeed.TICK_10;
                break;
            case PlatoonMos.ARCHER:
                prefabName = "Green";
                tempHp = 7;
                range = 40; // 궁병은 사거리가 김
                this.platoonMoveSpeed = PlatoonMoveSpeed.TICK_10;
                break;
            case PlatoonMos.SPEARMAN:
                prefabName = "Yellow";
                this.platoonMoveSpeed = PlatoonMoveSpeed.TICK_5;
                break;
            case PlatoonMos.DOWN_KNIGHT:
            case PlatoonMos.UP_KNIGHT:
            case PlatoonMos.KNIGHT:
                prefabName = "Blue";
                this.platoonMoveSpeed = PlatoonMoveSpeed.TICK_15;
                this.battleAreaX = 20;
                this.battleAreaY = 20;
                break;
        }

        if (!isRed) prefabName += "_2";

        atk = mosAtk * armyNum;
        hp = tempHp * armyNum;

        defaultAtk = atk;
        defaultHp = hp;

        for (int i = 0; i < platoonCount; i++)
        {
            for (int j = 0; j < platoonCount; j++)
            {
                //if (!isRed) prefabName = "Blue"; // 레드팀이 아니라면 프리펩 이름 변경
                GameObject obj = PoolsManager.Spawn(prefabName, Vector3.zero, Quaternion.identity);
                obj.transform.SetParent(transform);
                Unit unit = obj.GetComponent<Unit>();
                unit.platoonIdxX = i;
                unit.platoonIdxY = j;
                unit.tileIdxX = startTile.idxX + i;
                unit.tileIdxY = startTile.idxY + j;
                unit.isRed = isRed;
                unit.platoonNum = platoonNum;
                unit.UnitInit(mainGame, PlatoonMos, platoonSize, this);
                unit.platoonPos = DecisionUnitPos(num, unit);
                if (unit == platoonCommander)
                {
                    if (!isRed) obj.name = "Blue Platoon Leader / Company : " + companyNum + " / Platoon : " + platoonNum;
                    else obj.name = "Red Platoon Leader / Company : " + companyNum + " / Platoon : " + platoonNum;
                }
                unitList.Add(unit);
            }
        }

        if (isRed)
        {
            startDestinationX = mainGame.tileCountX - 1;
        }
        else
        {
            startDestinationX = 0;
        }
        startDestinationY = platoonCommander.tileIdxY;

        SetPlatoonCommanderToUnit(platoonCommander);

        SetPlatoonCenterPos();
    }

    // 소대에서 병사가 배치되야할 위치를 결정
    public UnitPos DecisionUnitPos(PlatoonSize num, Unit unit)
    {
        UnitPos unitPos = UnitPos.POS_UNKNOW;

        int platoonRight = (int)num - 1; // 부대 총 개수에서 -1한 인덱스

        if (PlatoonSize.ONE == num) // 병사가 한개면 그냥 그 병사가 부대장
        {
            platoonCommander = unit;

            if (unit.isRed) unitPos = UnitPos.CENTER;
            else unitPos = UnitPos.CENTER;
        }
        else
        {
            if (unit.platoonIdxX == 0 && unit.platoonIdxY == 0) // 좌 하단
            {
                unitPos = UnitPos.LEFT_DOWN;
            }
            else if (unit.platoonIdxX == platoonRight && unit.platoonIdxY == 0) // 우 하단
            {
                unitPos = UnitPos.RIGHT_DOWN;
            }
            else if (unit.platoonIdxX == 0 && unit.platoonIdxY == platoonRight) // 좌 상단
            {
                unitPos = UnitPos.LEFT_UP;
            }
            else if (unit.platoonIdxX == platoonRight && unit.platoonIdxY == platoonRight) // 우 상단
            {
                unitPos = UnitPos.RIGHT_UP;
            }
            else
            {
                if (unit.platoonIdxX == 0) // 좌측열
                {
                    unitPos = UnitPos.LEFT;
                }
                else if (unit.platoonIdxX == platoonRight) // 우측열
                {
                    unitPos = UnitPos.RIGHT;
                }
                else if (unit.platoonIdxY == platoonRight) // 윗열
                {
                    unitPos = UnitPos.UP;
                }
                else if (unit.platoonIdxY == 0) // 아랫열
                {
                    unitPos = UnitPos.DOWN;
                }
            }

            if (unitPos == UnitPos.POS_UNKNOW)
            {
                switch (num)
                {
                    case PlatoonSize.THOUSAND:
                        if (unit.platoonIdxX == 1 && unit.platoonIdxY == 1)
                        {
                            platoonCommander = unit;
                            unitPos = UnitPos.CENTER;
                        }
                        break;
                    case PlatoonSize.FIVE_THOUSAND:
                        if (isRed)
                        {
                            if (unit.platoonNum > 2)
                            {
                                if (unit.platoonIdxX == 2 && unit.platoonIdxY == 1)
                                {
                                    platoonCommander = unit;
                                    unitPos = UnitPos.CENTER;
                                }
                            }
                            else
                            {
                                if (unit.platoonIdxX == 2 && unit.platoonIdxY == 2)
                                {
                                    platoonCommander = unit;
                                    unitPos = UnitPos.CENTER;
                                }
                            }
                        }
                        else
                        {
                            if (unit.platoonNum > 1)
                            {
                                if (unit.platoonIdxX == 1 && unit.platoonIdxY == 1)
                                {
                                    platoonCommander = unit;
                                    unitPos = UnitPos.CENTER;
                                }
                            }
                            else
                            {
                                if (unit.platoonIdxX == 1 && unit.platoonIdxY == 2)
                                {
                                    platoonCommander = unit;
                                    unitPos = UnitPos.CENTER;
                                }
                            }
                        }
                        break;
                    case PlatoonSize.TEN_THOUSAND:
                        if (unit.platoonIdxX == 2 && unit.platoonIdxY == 2)
                        {
                            platoonCommander = unit;
                            unitPos = UnitPos.CENTER;
                        }
                        break;
                }
            }

            // 이 조건에도 걸리지 않았다면 부대 외곽이 아닌 내부에 위치
        }

        return unitPos;
    }

    public void Move(Action callBack)
    {
        moveCallback = callBack;

        if (platoonStatus == PlatoonStatus.UNIT_MOVE) // 중대 타겟도 소대 타겟도 없이 한방향 이동일 때 (중대 패스파인딩 우선)
        {
            if (platoonPath.Count > 0)
            {
                platoonPathCount = platoonPath.Count;
                PlatoonTargetIdxX = platoonPath[0].tile.idxX;
                PlatoonTargetIdxY = platoonPath[0].tile.idxY;
                platoonMoveDirection = PlatoonDirectionDecision();

                if (DecisionMoveByDirection(platoonMoveDirection))
                {
                    Tile currentTile = mainGame.GetTileByIdx(platoonCommander.tileIdxX, platoonCommander.tileIdxY);
                    Tile tile = DecisionTileByDirection(platoonMoveDirection, currentTile);
                    // StartMove(platoonMoveDirection);
                    // if (tile == platoonPath[0].tile) platoonPath.RemoveAt(0);

                    if (tile == platoonPath[0].tile)
                    {
                        platoonPath.RemoveAt(0);
                        StartMove(platoonMoveDirection);
                    }
                    else
                    {
                        // StartMove(PathFinding(platoonMoveDirection));
                        mainGame.platoonIdx++;
                        moveCallback();
                    }
                }
                else
                {
                    //StartMove(PathFinding(platoonMoveDirection));

                    // 사실 기획의도는 움직임을 멈추는게 맞는거 같긴한데 이러면 이상함;
                    //platoonStatus = PlatoonStatus.UNIT_IDLE;
                    mainGame.platoonIdx++;
                    moveCallback();
                 }
            }
            else
            {
                platoonPathCount = 0;
                PlatoonMoveDirection direction = PlatoonMoveDirection.DIR_LEFT;
                if (isRed) direction = PlatoonMoveDirection.DIR_RIGHT;

                platoonMoveDirection = direction;

                if (DecisionMoveByDirection(platoonMoveDirection))
                {
                    StartMove(platoonMoveDirection);
                }
                else
                {
                    mainGame.platoonIdx++;
                    moveCallback();
                    // StartMove(PathFinding(platoonMoveDirection));
                }
            }
        }
        else if (platoonStatus == PlatoonStatus.UNIT_ATTACK_MOVE) // 소대 타겟을 향해 이동할 때 (소대별 패스파인딩을 따로 합니다.)
        {
            if (platoonPath.Count > 0)
            {
                platoonPathCount = platoonPath.Count;
                PlatoonTargetIdxX = platoonPath[0].tile.idxX;
                PlatoonTargetIdxY = platoonPath[0].tile.idxY;
                platoonMoveDirection = PlatoonDirectionDecision();

                if (DecisionMoveByDirection(platoonMoveDirection))
                {
                    Tile currentTile = mainGame.GetTileByIdx(platoonCommander.tileIdxX, platoonCommander.tileIdxY);
                    Tile tile = DecisionTileByDirection(platoonMoveDirection, currentTile);
                    //StartMove(platoonMoveDirection);
                    //if (tile == platoonPath[0].tile) platoonPath.RemoveAt(0);

                    if (tile == platoonPath[0].tile)
                    {
                        platoonPath.RemoveAt(0);
                        StartMove(platoonMoveDirection);
                    }
                    else
                    {
                        // StartMove(PathFinding(platoonMoveDirection));

                        mainGame.platoonIdx++;
                        moveCallback();
                    }
                }
                else
                {
                    //StartMove(PathFinding(platoonMoveDirection));

                    // 사실 기획의도는 움직임을 멈추는게 맞는거 같긴한데 이러면 이상함;
                    mainGame.platoonIdx++;
                    moveCallback();
                }
            }
            else
            {
                platoonPathCount = 0;
                PlatoonTargetIdxX = targetPlatoon.platoonCommander.tileIdxX;
                PlatoonTargetIdxY = targetPlatoon.platoonCommander.tileIdxY;
                platoonMoveDirection = PlatoonDirectionDecision();
                
                if (DecisionMoveByDirection(platoonMoveDirection))
                {
                    StartMove(platoonMoveDirection);
                }
                else
                {
                    mainGame.platoonIdx++;
                    moveCallback();
                    // StartMove(PathFinding(platoonMoveDirection));
                }
            }
        }
        else if (platoonStatus == PlatoonStatus.UNIT_FIND_MOVE) // 중대 타겟을 향해 이동할 때 (중대 패스파인딩 우선)
        {
            if (platoonPath.Count > 0)
            {
                platoonPathCount = platoonPath.Count;
                // Unit enemy = mainGame.GetCompanyTarget(companyNum, isRed).platoonCommander;
                // PlatoonTargetIdxX = enemy.tileIdxX;
                // PlatoonTargetIdxY = enemy.tileIdxY;
                // platoonMoveDirection = PlatoonDirectionDecision();

                PlatoonTargetIdxX = platoonPath[0].tile.idxX;
                PlatoonTargetIdxY = platoonPath[0].tile.idxY;
                platoonMoveDirection = PlatoonDirectionDecision();

                if (DecisionMoveByDirection(platoonMoveDirection))
                {
                    Tile currentTile = mainGame.GetTileByIdx(platoonCommander.tileIdxX, platoonCommander.tileIdxY);
                    Tile tile = DecisionTileByDirection(platoonMoveDirection, currentTile);

                    if (tile == platoonPath[0].tile)
                    {
                        platoonPath.RemoveAt(0);
                        StartMove(platoonMoveDirection);
                    }
                    else
                    {
                        // StartMove(PathFinding(platoonMoveDirection));

                        mainGame.platoonIdx++;
                        moveCallback();
                    }
                }
                else
                {
                    //StartMove(PathFinding(platoonMoveDirection));

                    // 사실 기획의도는 움직임을 멈추는게 맞는거 같긴한데 이러면 이상함;
                    mainGame.platoonIdx++;
                    moveCallback();
                }
            }
            else
            {
                platoonPathCount = 0;
                Platoon enemyPlatoon = mainGame.GetCompanyTarget(companyNum, isRed);
                if (enemyPlatoon != null)
                {
                    Unit enemy = mainGame.GetCompanyTarget(companyNum, isRed).platoonCommander;
                    PlatoonTargetIdxX = enemy.tileIdxX;
                    PlatoonTargetIdxY = enemy.tileIdxY;
                    platoonMoveDirection = PlatoonDirectionDecision();

                    if (DecisionMoveByDirection(platoonMoveDirection))
                    {
                        StartMove(platoonMoveDirection);
                    }
                    else
                    {
                        // mainGame.platoonIdx++;
                        // moveCallback();

                        StartMove(PathFinding(platoonMoveDirection));
                    }
                }
                else
                {
                    mainGame.platoonIdx++;
                    moveCallback();
                }
            }
        }
        else
        {
            platoonPathCount = 0;
            mainGame.platoonIdx++;

            if (moveCallback != null)
            {
                moveCallback();
            }
        }

        // 이 외라면 보통 전투 상태일 때입니다.
    }

    public void Attack(Action callBack)
    {
        attackCallback = callBack;

        // 유닛이 전투중이고 소대 목표가 정상이라면?
        if (platoonStatus == PlatoonStatus.UNIT_ATTACK 
            && targetPlatoon != null 
            && IsTargetEnemyNearBy(targetPlatoon)) 
        {
            float offsetAtk = 1; // 병종 상성에 의한 공격력 증감값

            switch (platoonMos)
            {
                case PlatoonMos.FOOTMAN:
                    if (targetPlatoon.platoonMos == PlatoonMos.SPEARMAN) offsetAtk = 1.5f;
                    else if (targetPlatoon.platoonMos == PlatoonMos.KNIGHT) offsetAtk = 0.5f;
                    break;
                case PlatoonMos.KNIGHT:
                    if (targetPlatoon.platoonMos == PlatoonMos.FOOTMAN) offsetAtk = 1.5f;
                    else if (targetPlatoon.platoonMos == PlatoonMos.SPEARMAN) offsetAtk = 0.5f;
                    break;
                case PlatoonMos.SPEARMAN:
                    if (targetPlatoon.platoonMos == PlatoonMos.KNIGHT) offsetAtk = 1.5f;
                    else if (targetPlatoon.platoonMos == PlatoonMos.FOOTMAN) offsetAtk = 0.5f;
                    break;
                case PlatoonMos.ARCHER:
                    GameObject arrow = PoolsManager.Spawn("Black", platoonCommander.transform.position, Quaternion.identity);
                    arrow.transform.DOMove(targetPlatoon.platoonCommander.transform.position, 1, false);
                    arrow.GetComponent<Arrow>().enabled = true;
                    break;
            }

            float damage = atk * offsetAtk;

            if (mainGame.isDamgeReduce)
            {
                damage /= BALANCE_DMG;
            }

            if (damage < 1) damage = 1;

            Debug.Log("공격! : " + isRed + "팀/" + companyNum +", 데미지 : " + damage);

            targetPlatoon.DamgeToPlatoon(damage, this); // 적 소대 체력을 깎습니다.

            if (targetPlatoon.hp <= 0) // 적 소대가 전멸했다면?
            {
                targetPlatoon = null;
                platoonStatus = PlatoonStatus.UNIT_IDLE; // 소대 상태를 기본으로

                if (this == companyCommander) // 본인 소대가 중대장이라면?
                {
                    mainGame.SetCompanyTarget(companyNum, targetPlatoon, isRed); // 중대 목표를 초기화합니다.
                }
            }
        }
        else // 유닛이 전투중이 아니라면
        {
            // Debug.Log("공격안함 : " + isRed + "팀" + companyNum);
            Unit target = null;
            if (platoonMos == PlatoonMos.ARCHER) // 궁병이라면?
            {
                target = CheckEnemyInRange();
            }
            else target = CheckEnemyTile(); // 주변에 적이 있는지 검사

            if (target == null) // 적이 없다면 상태 IDLE
            {
                platoonStatus = PlatoonStatus.UNIT_IDLE;
            }
            else // 적이 존재한다면 공격 준비
            {
                Debug.Log("공격준비~1 : " + isRed + "팀/" + companyNum);
                platoonStatus = PlatoonStatus.UNIT_ATTACK;
                targetPlatoon = target.platoon;
            }
        }

        mainGame.platoonIdx++;

        if (attackCallback != null)
        {
            attackCallback();
        }
    }

    public IEnumerator DespawnArrow(GameObject obj)
    {
        yield return new WaitForSeconds(3);
        PoolsManager.Despawn(obj);
    }

    public PlatoonMoveDirection PlatoonDirectionDecision()
    {
        // 타겟 - 현 좌표 
        Tile currentTile = mainGame.GetTileByIdx(platoonCommander.tileIdxX, platoonCommander.tileIdxY);
        Tile targetTile = mainGame.GetTileByIdx(PlatoonTargetIdxX, PlatoonTargetIdxY);
        Vector3 target = new Vector3(targetTile.idxX, targetTile.idxY, 0) - new Vector3(currentTile.idxX, currentTile.idxY, 0);

        // 180 ~ -180 degree
        float angle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;

        // Debug.Log("소대와 타겟의 앵글 : " + angle);

        if ((0 <= angle && angle < 22.5f) || (-0.1f >= angle && angle > -22.5f))
        {
            platoonMoveDirection = PlatoonMoveDirection.DIR_RIGHT;
            platoonBackDirection = PlatoonMoveDirection.DIR_LEFT;
        }
        else if (angle >= 22.5f && angle < 67.5f)
        {
            platoonMoveDirection = PlatoonMoveDirection.DIR_RIGHT_UP;
            platoonBackDirection = PlatoonMoveDirection.DIR_LEFT_DOWN;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            platoonMoveDirection = PlatoonMoveDirection.DIR_UP;
            platoonBackDirection = PlatoonMoveDirection.DIR_DOWN;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            platoonMoveDirection = PlatoonMoveDirection.DIR_LEFT_UP;
            platoonBackDirection = PlatoonMoveDirection.DIR_RIGHT_DOWN;
        }
        else if ((180 >= angle && angle >= 157.5f) || (-180.0f <= angle && angle <= -157.5f))
        {
            platoonMoveDirection = PlatoonMoveDirection.DIR_LEFT;
            platoonBackDirection = PlatoonMoveDirection.DIR_RIGHT;
        }
        else if (angle <= -112.5f && angle > -157.5f)
        {
            platoonMoveDirection = PlatoonMoveDirection.DIR_LEFT_DOWN;
            platoonBackDirection = PlatoonMoveDirection.DIR_RIGHT_UP;
        }
        else if (angle <= -67.5f && angle > -112.5f)
        {
            platoonMoveDirection = PlatoonMoveDirection.DIR_DOWN;
            platoonBackDirection = PlatoonMoveDirection.DIR_UP;
        }
        else if (angle <= -22.5f && angle > -67.5f)
        {
            platoonMoveDirection = PlatoonMoveDirection.DIR_RIGHT_DOWN;
            platoonBackDirection = PlatoonMoveDirection.DIR_LEFT_UP;
        }

        return platoonMoveDirection;
    }

    public PlatoonMoveDirection DirectionDecisionToTarget(Tile currentTile, Tile targetTile, bool backDirection = false)
    {
        PlatoonMoveDirection direction = PlatoonMoveDirection.DIR_LEFT;

        // 타겟 - 현 좌표 
        Vector3 target = new Vector3(targetTile.idxX, targetTile.idxY, 0) - new Vector3(currentTile.idxX, currentTile.idxY, 0);

        // 180 ~ -180 degree
        float angle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;

        // Debug.Log("소대와 타겟의 앵글 : " + angle);

        if ((0 <= angle && angle < 22.5f) || (-0.1f >= angle && angle > -22.5f))
        {
            if (!backDirection) direction = PlatoonMoveDirection.DIR_RIGHT;
            else direction = PlatoonMoveDirection.DIR_LEFT;
        }
        else if (angle >= 22.5f && angle < 67.5f)
        {
            if (!backDirection) direction = PlatoonMoveDirection.DIR_RIGHT_UP;
            else direction = PlatoonMoveDirection.DIR_LEFT_DOWN;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            if (!backDirection) direction = PlatoonMoveDirection.DIR_UP;
            else direction = PlatoonMoveDirection.DIR_DOWN;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            if (!backDirection) direction = PlatoonMoveDirection.DIR_LEFT_UP;
            else direction = PlatoonMoveDirection.DIR_RIGHT_DOWN;
        }
        else if ((180 >= angle && angle >= 157.5f) || (-180.0f <= angle && angle <= -157.5f))
        {
            if (!backDirection) direction = PlatoonMoveDirection.DIR_LEFT;
            else direction = PlatoonMoveDirection.DIR_RIGHT;
        }
        else if (angle <= -112.5f && angle > -157.5f)
        {
            if (!backDirection) direction = PlatoonMoveDirection.DIR_LEFT_DOWN;
            else direction = PlatoonMoveDirection.DIR_RIGHT_UP;
        }
        else if (angle <= -67.5f && angle > -112.5f)
        {
            if (!backDirection) direction = PlatoonMoveDirection.DIR_DOWN;
            else direction = PlatoonMoveDirection.DIR_UP;
        }
        else if (angle <= -22.5f && angle > -67.5f)
        {
            if (!backDirection) direction = PlatoonMoveDirection.DIR_RIGHT_DOWN;
            else direction = PlatoonMoveDirection.DIR_LEFT_UP;
        }

        return direction;
    }

    public bool DecisionMoveByDirection(PlatoonMoveDirection direction, bool usePathFind = false)
    {
        checkUnitList.Clear();

        if (platoonSize == PlatoonSize.ONE) checkUnitList.Add(unitList[0]);
        else if (platoonLine <= 1)
        {
            for (int i = 0; i < unitList.Count; i++)
            {
                checkUnitList.Add(unitList[i]);
            }
        }
        else
        {
            switch (direction)
            {
                case PlatoonMoveDirection.DIR_LEFT:
                    {
                        for (int i = 0; i < unitList.Count; i++)
                        {
                            if (unitList[i].platoonPos == UnitPos.LEFT ||
                                unitList[i].platoonPos == UnitPos.LEFT_DOWN ||
                                unitList[i].platoonPos == UnitPos.LEFT_UP)
                            {
                                checkUnitList.Add(unitList[i]);
                            }
                        }
                    }
                    break;
                case PlatoonMoveDirection.DIR_RIGHT:
                    {
                        for (int i = 0; i < unitList.Count; i++)
                        {
                            if (unitList[i].platoonPos == UnitPos.RIGHT ||
                                unitList[i].platoonPos == UnitPos.RIGHT_DOWN ||
                                unitList[i].platoonPos == UnitPos.RIGHT_UP)
                            {
                                checkUnitList.Add(unitList[i]);
                            }
                        }
                    }
                    break;
                case PlatoonMoveDirection.DIR_UP:
                    {
                        for (int i = 0; i < unitList.Count; i++)
                        {
                            if (unitList[i].platoonPos == UnitPos.UP ||
                                unitList[i].platoonPos == UnitPos.LEFT_UP ||
                                unitList[i].platoonPos == UnitPos.RIGHT_UP)
                            {
                                checkUnitList.Add(unitList[i]);
                            }
                        }
                    }
                    break;
                case PlatoonMoveDirection.DIR_DOWN:
                    {
                        for (int i = 0; i < unitList.Count; i++)
                        {
                            if (unitList[i].platoonPos == UnitPos.DOWN ||
                                unitList[i].platoonPos == UnitPos.LEFT_DOWN ||
                                unitList[i].platoonPos == UnitPos.RIGHT_DOWN)
                            {
                                checkUnitList.Add(unitList[i]);
                            }
                        }
                    }
                    break;
                case PlatoonMoveDirection.DIR_LEFT_DOWN:
                    {
                        for (int i = 0; i < unitList.Count; i++)
                        {
                            if (unitList[i].platoonPos == UnitPos.LEFT ||
                                unitList[i].platoonPos == UnitPos.LEFT_DOWN ||
                                unitList[i].platoonPos == UnitPos.LEFT_UP ||
                                unitList[i].platoonPos == UnitPos.DOWN ||
                                unitList[i].platoonPos == UnitPos.RIGHT_DOWN)
                            {
                                checkUnitList.Add(unitList[i]);
                            }
                        }
                    }
                    break;
                case PlatoonMoveDirection.DIR_LEFT_UP:
                    {
                        for (int i = 0; i < unitList.Count; i++)
                        {
                            if (unitList[i].platoonPos == UnitPos.LEFT ||
                                unitList[i].platoonPos == UnitPos.LEFT_DOWN ||
                                unitList[i].platoonPos == UnitPos.LEFT_UP ||
                                unitList[i].platoonPos == UnitPos.UP ||
                                unitList[i].platoonPos == UnitPos.RIGHT_UP)
                            {
                                checkUnitList.Add(unitList[i]);
                            }
                        }
                    }
                    break;
                case PlatoonMoveDirection.DIR_RIGHT_DOWN:
                    {
                        for (int i = 0; i < unitList.Count; i++)
                        {
                            if (unitList[i].platoonPos == UnitPos.RIGHT ||
                                unitList[i].platoonPos == UnitPos.RIGHT_DOWN ||
                                unitList[i].platoonPos == UnitPos.RIGHT_UP ||
                                unitList[i].platoonPos == UnitPos.DOWN ||
                                unitList[i].platoonPos == UnitPos.LEFT_DOWN)
                            {
                                checkUnitList.Add(unitList[i]);
                            }
                        }
                    }
                    break;
                case PlatoonMoveDirection.DIR_RIGHT_UP:
                    {
                        for (int i = 0; i < unitList.Count; i++)
                        {
                            if (unitList[i].platoonPos == UnitPos.RIGHT ||
                                unitList[i].platoonPos == UnitPos.RIGHT_DOWN ||
                                unitList[i].platoonPos == UnitPos.RIGHT_UP ||
                                unitList[i].platoonPos == UnitPos.UP ||
                                unitList[i].platoonPos == UnitPos.LEFT_UP)
                            {
                                checkUnitList.Add(unitList[i]);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        if (checkUnitList.Count == 0) return false;

        for (int i = 0; i < checkUnitList.Count; i++)
        {
            Tile tile = DecisionTileByDirection(direction, mainGame.GetTileByIdx(checkUnitList[i].tileIdxX, checkUnitList[i].tileIdxY));

            if (tile == null) return false;
            if (tile.isHaveUnit && tile.haveUnit.platoon != this)
            {
                if (!usePathFind)
                {
                    if (tile.haveUnit.platoon != this) return false;
                }
                else
                {
                    if (tile.haveUnit.isRed == isRed)
                    {
                        if (tile.haveUnit.platoon != null)
                        {
                            if (tile.haveUnit.platoon.companyNum != companyNum)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    public Tile DecisionTileByDirection(PlatoonMoveDirection direction, Tile tile)
    {
        Tile moveTile = null;
        int addIdxX = 0, addIdxY = 0;

        switch (direction)
        {
            case PlatoonMoveDirection.DIR_DOWN:
                addIdxY = -1;
                break;
            case PlatoonMoveDirection.DIR_UP:
                addIdxY = 1;
                break;
            case PlatoonMoveDirection.DIR_LEFT:
                addIdxX = -1;
                break;
            case PlatoonMoveDirection.DIR_RIGHT:
                addIdxX = 1;
                break;
            case PlatoonMoveDirection.DIR_LEFT_DOWN:
                addIdxX = -1;
                addIdxY = -1;
                break;
            case PlatoonMoveDirection.DIR_LEFT_UP:
                addIdxX = -1;
                addIdxY = 1;
                break;
            case PlatoonMoveDirection.DIR_RIGHT_UP:
                 addIdxX = 1;
                 addIdxY = 1;
                break;
            case PlatoonMoveDirection.DIR_RIGHT_DOWN:
                addIdxX = 1;
                addIdxY = -1;
                break;
            case PlatoonMoveDirection.DIR_DEFAULT:
                break;
        }

        if (tile.idxX + addIdxX < 0) return null;
        if (tile.idxX + addIdxX >= mainGame.tileCountX) return null;
        if (tile.idxY + addIdxY < 0) return null;
        if (tile.idxY + addIdxY >= mainGame.tileCountY) return null;

        moveTile = mainGame.GetTileByIdx(tile.idxX + addIdxX, tile.idxY + addIdxY);

        return moveTile;
    }

    public Unit GetUnitByPos(UnitPos pos)
    {
        for (int i = 0; i < unitList.Count; i++)
        {
            if (unitList[i].platoonPos == pos) return unitList[i];
        }

        return null;
    }

    public void StartMove(PlatoonMoveDirection direction)
    {
        Dictionary<Tile, Unit> tileDic = new Dictionary<Tile, Unit>(); // 지나간 타일들의 정보를 담아둘 딕셔너리
        if (direction != PlatoonMoveDirection.DIR_DEFAULT)
        {
            Tile startTile = mainGame.GetTileByIdx(platoonCommander.tileIdxX, platoonCommander.tileIdxY);
            Tile destinationTile = startTile;

            for (int i = 0; i < unitList.Count; i++)
            {
                Tile currentTile = mainGame.GetTileByIdx(unitList[i].tileIdxX, unitList[i].tileIdxY);
                Tile moveTile = DecisionTileByDirection(direction, currentTile);
                if (moveTile == null)
                {
                    Debug.Log("moveTile is null!!!!!!!");
                    continue;
                }

                if (!tileDic.ContainsKey(currentTile)) // 지금 타일이 누가 먼저 올라간 타일이 아니라면? 타일 초기화
                {
                    currentTile.SetObject(null);
                }
                else
                {
                    currentTile.SetObject(tileDic[currentTile]);
                }

                if (unitList[i] == platoonCommander)
                {
                    destinationTile = moveTile;

                    if (closeTileDic.ContainsKey(moveTile))
                    {
                        closeTileDic[moveTile] = mainGame.tick;
                    }
                    else
                    {
                        closeTileDic.Add(moveTile, mainGame.tick);
                    }
                }

                prevTile = moveTile;

                moveTile.SetObject(unitList[i]);
                tileDic.Add(moveTile, unitList[i]);
                unitList[i].tileIdxX = moveTile.idxX;
                unitList[i].tileIdxY = moveTile.idxY;
                unitList[i].gameObject.transform.position = new Vector3(moveTile.posX, moveTile.posY, 0); // 이동합시다
            }

            centerPosX += destinationTile.idxX - startTile.idxX;
            centerPosY += destinationTile.idxY - startTile.idxY;
        }
        else platoonStatus = PlatoonStatus.UNIT_IDLE;

        mainGame.platoonIdx++;

        Unit target = null;
        if (platoonMos == PlatoonMos.ARCHER) // 궁병이라면?
        {
            target = CheckEnemyInRange();
        }
        else target = CheckEnemyTile(); // 주변에 적이 있는지 검사

        if (target != null)
        {
            Debug.Log("공격준비~2 : " + isRed + "팀/" + companyNum);
            platoonStatus = PlatoonStatus.UNIT_ATTACK_READY;
            targetPlatoon = target.platoon;
        }

        if (moveCallback != null)
        {
            moveCallback();
        }
    }

    // 원래는 가야했어야할 방향을 제외한 4방향 타일을 검사한 뒤에 최적 비용 타일을 리턴, 없다면 제자리입니다.
    public PlatoonMoveDirection PathFinding(PlatoonMoveDirection direction)
    {
        int directionNum = (int)direction;

        dirList.Clear();

        for (int i = directionNum - 3; i <= directionNum + 4; i++)
        {
            int idx = i;

            if (i < 0) idx = i + 8;
            else if (i > 7) idx = i - 8;

            if (idx == directionNum) continue; // 원래 가려했던 방향이라면 패스

            if (!DecisionMoveByDirection((PlatoonMoveDirection)idx)) continue; // 가능 방향으로 갈 수 없다면 패스

            Tile tile = DecisionTileByDirection((PlatoonMoveDirection)idx, mainGame.GetTileByIdx(platoonCommander.tileIdxX, platoonCommander.tileIdxY));

            // if ((PlatoonMoveDirection)directionNum != platoonBackDirection && closeTileDic.ContainsKey(tile))

            if (tile == prevTile) continue;

            /*
            if (closeTileDic.ContainsKey(tile))
            {
                if (mainGame.tick - closeTileDic[tile] < 60) continue;
            }
            */

            //Tile targetTile = mainGame.GetTileByIdx(PlatoonTargetIdxX, PlatoonTargetIdxY);

            int targetX;
            int targetY;

            if (targetPlatoon != null && targetPlatoon.hp <= 0)
            {
                targetX = targetPlatoon.platoonCommander.tileIdxX;
                targetY = targetPlatoon.platoonCommander.tileIdxY;
                // targetTile = mainGame.GetTileByIdx(targetPlatoon.platoonCommander.tileIdxX, targetPlatoon.platoonCommander.tileIdxY);
            }
            else
            {
                targetX = startDestinationX;
                targetY = platoonCommander.tileIdxY;
                // targetTile = mainGame.GetTileByIdx(startDestinationX, platoonCommander.tileIdxY);
            }

            float distance = Vector3.Distance(new Vector3(targetX, targetY, 0), new Vector3(tile.idxX, tile.idxY));

            PathStruct dir = new PathStruct();
            dir.H = distance;
            dir.dir = (PlatoonMoveDirection)idx;
            dir.tile = tile;
            dirList.Add(dir);
        }

        if (dirList.Count > 0)
        {
            List<PathStruct> list = dirList.OrderBy(i => i.H).ToList(); // 타일의 H 거리값 정렬
            platoonMoveDirection = list[0].dir; // 가장 빠른 타일

            /*
            for (int i = 1; i < list.Count; i++)
            {
                if (!closeTileDic.ContainsKey(list[i].tile))
                {
                    closeTileDic.Add(list[i].tile, mainGame.tick);
                } 
            }
            */
        }
        else platoonMoveDirection = PlatoonMoveDirection.DIR_DEFAULT; // 없다면 제자리

        return platoonMoveDirection;
    }

    // 소대 인접 외곽 타일에 적이 위치한지 체크하자!
    public Unit CheckEnemyTile()
    {
        Unit target = null;

        if (platoonLine <= 1)
        {
            for (int i = 0; i < unitList.Count; i++)
            {
                UnitPos pos = unitList[i].platoonPos;

                Tile currentTile = mainGame.GetTileByIdx(unitList[i].tileIdxX, unitList[i].tileIdxY);

                switch (pos)
                {
                    case UnitPos.LEFT:
                    case UnitPos.RIGHT:
                        target  = CheckTargetNearBy(PlatoonMoveDirection.DIR_LEFT, currentTile);
                        if (target == null) target = CheckTargetNearBy(PlatoonMoveDirection.DIR_RIGHT, currentTile);
                        break;
                    case UnitPos.UP:
                    case UnitPos.LEFT_UP:
                    case UnitPos.RIGHT_UP:
                        target = CheckTargetNearBy(PlatoonMoveDirection.DIR_UP, currentTile);
                        if (target == null) target = CheckTargetNearBy(PlatoonMoveDirection.DIR_LEFT_UP, currentTile);
                        if (target == null) target = CheckTargetNearBy(PlatoonMoveDirection.DIR_RIGHT_UP, currentTile);
                        break;
                    case UnitPos.DOWN:
                    case UnitPos.LEFT_DOWN:
                    case UnitPos.RIGHT_DOWN:
                        target = CheckTargetNearBy(PlatoonMoveDirection.DIR_DOWN, currentTile);
                        if (target == null) target = CheckTargetNearBy(PlatoonMoveDirection.DIR_LEFT_DOWN, currentTile);
                        if (target == null) target = CheckTargetNearBy(PlatoonMoveDirection.DIR_RIGHT_DOWN, currentTile);
                        break;
                }

                if (target != null) break;
            }
        }
        else
        {
            for (int i = 0; i < unitList.Count; i++)
            {
                UnitPos pos = unitList[i].platoonPos;

                if (pos == UnitPos.CENTER || pos == UnitPos.POS_UNKNOW) continue;

                Tile currentTile = mainGame.GetTileByIdx(unitList[i].tileIdxX, unitList[i].tileIdxY);

                switch (pos)
                {
                    case UnitPos.LEFT:
                        target = CheckTargetNearBy(PlatoonMoveDirection.DIR_LEFT, currentTile);
                        break;
                    case UnitPos.RIGHT:
                        target = CheckTargetNearBy(PlatoonMoveDirection.DIR_RIGHT, currentTile);
                        break;
                    case UnitPos.UP:
                        target = CheckTargetNearBy(PlatoonMoveDirection.DIR_UP, currentTile);
                        break;
                    case UnitPos.DOWN:
                        target = CheckTargetNearBy(PlatoonMoveDirection.DIR_DOWN, currentTile);
                        break;
                    case UnitPos.LEFT_DOWN:
                        target = CheckTargetNearBy(PlatoonMoveDirection.DIR_LEFT_DOWN, currentTile);
                        break;
                    case UnitPos.LEFT_UP:
                        target = CheckTargetNearBy(PlatoonMoveDirection.DIR_LEFT_UP, currentTile);
                        break;
                    case UnitPos.RIGHT_UP:
                        target = CheckTargetNearBy(PlatoonMoveDirection.DIR_RIGHT_UP, currentTile);
                        break;
                    case UnitPos.RIGHT_DOWN:
                        target = CheckTargetNearBy(PlatoonMoveDirection.DIR_RIGHT_DOWN, currentTile);
                        break;
                }

                if (target != null) break;
            }
        }

        return target;
    }

    bool IsTargetEnemyNearBy(Platoon target)
    {
        if (platoonMos == PlatoonMos.ARCHER)
        {
            if (CheckEnemyInRange() != null) return true;
        }
        else
        {
            for (int i = 0; i < unitList.Count; i++)
            {
                UnitPos pos = unitList[i].platoonPos;

                if (pos == UnitPos.CENTER || pos == UnitPos.POS_UNKNOW) continue;

                Tile currentTile = mainGame.GetTileByIdx(unitList[i].tileIdxX, unitList[i].tileIdxY);
                Tile targetTile = null;

                switch (pos)
                {
                    case UnitPos.LEFT:
                        targetTile = DecisionTileByDirection(PlatoonMoveDirection.DIR_LEFT, currentTile);
                        break;
                    case UnitPos.RIGHT:
                        targetTile = DecisionTileByDirection(PlatoonMoveDirection.DIR_RIGHT, currentTile);
                        break;
                    case UnitPos.UP:
                        targetTile = DecisionTileByDirection(PlatoonMoveDirection.DIR_UP, currentTile);
                        break;
                    case UnitPos.DOWN:
                        targetTile = DecisionTileByDirection(PlatoonMoveDirection.DIR_DOWN, currentTile);
                        break;
                    case UnitPos.LEFT_DOWN:
                        targetTile = DecisionTileByDirection(PlatoonMoveDirection.DIR_LEFT_DOWN, currentTile);
                        break;
                    case UnitPos.LEFT_UP:
                        targetTile = DecisionTileByDirection(PlatoonMoveDirection.DIR_LEFT_UP, currentTile);
                        break;
                    case UnitPos.RIGHT_UP:
                        targetTile = DecisionTileByDirection(PlatoonMoveDirection.DIR_RIGHT_UP, currentTile);
                        break;
                    case UnitPos.RIGHT_DOWN:
                        targetTile = DecisionTileByDirection(PlatoonMoveDirection.DIR_RIGHT_DOWN, currentTile);
                        break;
                }

                if (targetTile != null && targetTile.isHaveUnit)
                {
                    if (targetTile.haveUnit.platoon == target) return true; // 이게 일치한다면 근처에 목표 적 소대가 있습니다.
                }
            }
        }

        return false;
    }

    public Unit CheckEnemyInRange()
    {
        if (targetPlatoon == null) return null;
        
        float distance = Vector3.Distance(new Vector3(targetPlatoon.platoonCommander.tileIdxX, targetPlatoon.platoonCommander.tileIdxY, 0), new Vector3(centerPosX, centerPosY, 0));

        if (distance <= range) return targetPlatoon.platoonCommander;
        else return null;
    }
    
    public void DespawnPlatoon()
    {
        for (int i = 0; i < unitList.Count; i++)
        {
            Tile tile = mainGame.GetTileByIdx(unitList[i].tileIdxX, unitList[i].tileIdxY);
            tile.SetObject(null);

            PoolsManager.Despawn(unitList[i].gameObject);
        }

        unitList.Clear();

        PoolsManager.Despawn(companyText.gameObject);
        companyText = null;
    }

    // 소대 배열의 개수가 줄어드는 피해를 입었는지 체크하는 함수입니다.
    public bool CheckReducePlatoon()
    {
        int sizeNum = (int)platoonSize; // 일단 소대 사이즈를 받습니다.

        float tempHp = defaultHp / sizeNum;
        float remainHp = hp;

        if (remainHp <= 0) return false; // 소대가 전멸했다면 체킹할 필요가 없습니다.
        if (remainHp > defaultHp - tempHp) return false; // 남은 체력이 총 체력 - 나눌 양보다 크다면 체킹할 필요가 없습니다.
        
        // 몇개의 열이 사라져야하는지 계산합니다.
        int count = 1;

        for (int i = 1; i <= sizeNum; i++)
        {
            count = i;
            if (tempHp * i > remainHp) break;
        }

        if (count == platoonLine) return false; // 현재 소대 열 개수와 비교해서 같다면 리턴

        int loopCount = platoonLine - count;

        platoonLine = count; // 소대 열 개수를 없어져야할 소대 열 개수로 뺍니다.

        bool commanderChange = false;

        List<Unit> prevUnitList = new List<Unit>();

        for (int i = 0; i < loopCount; i++)
        {
            int idx = 0;
            while (idx < unitList.Count)
            {
                Unit currentUnit = unitList[idx];

                if (isRed)
                {
                    if (currentUnit.platoonPos == UnitPos.RIGHT
                    || currentUnit.platoonPos == UnitPos.RIGHT_UP
                    || currentUnit.platoonPos == UnitPos.RIGHT_DOWN)
                    {
                        prevUnitList.Add(currentUnit);
                        unitList.RemoveAt(idx);
                    }
                    else
                    {
                        ++idx;
                    }
                }
                else
                {
                    if (currentUnit.platoonPos == UnitPos.LEFT
                    || currentUnit.platoonPos == UnitPos.LEFT_UP
                    || currentUnit.platoonPos == UnitPos.LEFT_DOWN)
                    {
                        prevUnitList.Add(currentUnit);
                        unitList.RemoveAt(idx);
                    }
                    else
                    {
                        ++idx;
                    }
                }
            }
        }

        if (prevUnitList.Count == 0)
        {
            Debug.Log("소대를 지워야하는데 소대가 없다?!!!!!!!!!!");
        }

        for (int i = 0; i < prevUnitList.Count; i++)
        {
            Tile currentTile = mainGame.GetTileByIdx(prevUnitList[i].tileIdxX, prevUnitList[i].tileIdxY);

            PlatoonMoveDirection direction = PlatoonMoveDirection.DIR_LEFT;

            if (!isRed) direction = PlatoonMoveDirection.DIR_RIGHT;

            Unit nextUnit = DecisionTileByDirection(direction, currentTile).haveUnit;
            nextUnit.platoonPos = prevUnitList[i].platoonPos;

            if (prevUnitList[i] == platoonCommander)
            {
                platoonCommander = nextUnit;
                commanderChange = true;
            }

            currentTile.SetObject(null);
            PoolsManager.Despawn(prevUnitList[i].gameObject);
        }

        if (commanderChange) SetPlatoonCommanderToUnit(platoonCommander);

        SetPlatoonCenterPos();

        return true;
    }

    public void SetPlatoonCommanderToUnit(Unit commander)
    {
        for (int i = 0; i < unitList.Count; i++)
        {
            unitList[i].platoonCommander = commander;
        }
    }

    public void DamgeToPlatoon(float dmg, Platoon enemy)
    {
        hp -= dmg; // 소대의 체력을 깎고

        if (targetPlatoon == null)
        {
            targetPlatoon = enemy;
            // platoonStatus = PlatoonStatus.UNIT_FIND_MOVE;
            /*
            if (this == companyCommander) // 본인 소대가 중대장이라면?
            {
                mainGame.SetCompanyTarget(companyNum, targetPlatoon, isRed); // 중대 목표를 초기화합니다.
            }
            */
        }
    }

    public void CalcAtk()
    {
        float platoonNum = hp * 0.1f;

        atk = mosAtk * platoonNum; // 줄어든 병사수만큼 소대 공격력을 감소 시킵니다.

        if (atk < 1) atk = 1;

        // Debug.Log("공격력 감소 : " + isRed + "팀, 공격력 : " + atk);

        if (CheckReducePlatoon()) // 적 소대의 병사 배열이 줄어드는지 체킹합니다.
        {
            // platoonStatus = PlatoonStatus.UNIT_ATTACK_MOVE;
        }
    }

    public void Update()
    {
        if (companyCommander == this && companyNum == 0 && Input.GetKeyDown(KeyCode.Space))
        {
             JumpPointSearch(mainGame.GetTileByIdx(mainGame.tileCountX - 3, platoonCommander.tileIdxY));
        }
    }
    public void LateUpdate()
    {
        if (companyText != null)
        {
            var screenPos = mainGame.mainCamera.WorldToScreenPoint(platoonCommander.transform.position);

            if (screenPos.z < 0)
            {
                screenPos *= -1;
            }

            var localPos = Vector2.zero;

            RectTransform canvasRect = mainGame.uiCanvas.GetComponent<RectTransform>();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, mainGame.uiCamera, out localPos);

            companyText.transform.localPosition = localPos;
        }
    }

    // 특정 방향으로 타깃없이 이동할 때 사용되는 패스파인딩
    public List<PathStruct> SetCompanyPathFindListToDirection(PlatoonMoveDirection direction)
    {
        PlatoonMoveDirection targetDirection = direction;

        int directionNum = (int)direction;

        dirList.Clear();

        List<PathStruct> pathFindList = new List<PathStruct>();

        closePathDic.Clear();

        int currentTileX = platoonCommander.tileIdxX;
        int currentTileY = platoonCommander.tileIdxY;

        int destinationX = 0; 

        if (isRed) destinationX = mainGame.tileCountX - 1;

        SetSearchList();

        Tile targetTile = mainGame.GetTileByIdx(destinationX, companyCommander.platoonCommander.tileIdxY);

        while (true)
        {
            for (int i = directionNum - 3; i <= directionNum + 4; i++)
            {

                int idx = i;

                if (i < 0) idx = i + 8;
                else if (i > 7) idx = i - 8;

                int offsetX = currentTileX - platoonCommander.tileIdxX;
                int offsetY = currentTileY - platoonCommander.tileIdxY;

                if (!DecisionMoveBySearchListForMovePath((PlatoonMoveDirection)idx, offsetX, offsetY)) continue; // 가능 방향으로 갈 수 없다면 패스

                Tile tile = DecisionTileByDirection((PlatoonMoveDirection)idx, mainGame.GetTileByIdx(currentTileX, currentTileY));

                if (tile == null) continue;

                if (tile == targetTile)
                {
                    return pathFindList;
                }

                if (closePathDic.ContainsKey(tile)) continue; // 지나간 타일이었다면 패스

                float distance = Vector3.Distance(new Vector3(targetTile.idxX, targetTile.idxY, 0), new Vector3(tile.idxX, tile.idxY, 0));

                PathStruct dir = new PathStruct();
                dir.H = distance;
                dir.dir = (PlatoonMoveDirection)idx;
                dir.tile = tile;
                dir.idxX = destinationX;
                dir.idxY = platoonCommander.tileIdxY;
                dirList.Add(dir);
            }

            if (dirList.Count > 0)
            {
                List<PathStruct> list = dirList.OrderBy(i => i.H).ToList(); // 타일의 H 거리값 정렬
                directionNum = (int)list[0].dir;
                pathFindList.Add(list[0]); // 가장 빠른 타일 방향
                closePathDic.Add(mainGame.GetTileByIdx(currentTileX, currentTileY), 0);
                currentTileX = list[0].tile.idxX;
                currentTileY = list[0].tile.idxY;
                dirList.Clear();
            }
            else
            {
                // 길찾기 실패입니다.
                //pathFindList.Clear();
                return pathFindList;
            }
        }
    }

    // 소대 공격 범위 내 패스파인딩
    public List<PathStruct> SetPlatoonPathFindListToEnemyPlatoon(Platoon enemyPlatoon)
    {

        Tile targetTile = mainGame.GetTileByIdx(enemyPlatoon.platoonCommander.tileIdxX, enemyPlatoon.platoonCommander.tileIdxY);
        int directionNum = 0;

        dirList.Clear();

        List<PathStruct> pathFindList = new List<PathStruct>();

        closePathDic.Clear();

        int currentTileX = platoonCommander.tileIdxX;
        int currentTileY = platoonCommander.tileIdxY;

        int friendlyXLeft = platoonCommander.tileIdxX - (battleAreaX + 4);
        int friendlyXRight = platoonCommander.tileIdxX + battleAreaX;
        int friendlyYUp = platoonCommander.tileIdxY + battleAreaY + 2;
        int friendlyYDown = platoonCommander.tileIdxY - battleAreaY - 2;

        int destinationX = 0;

        if (isRed) destinationX = mainGame.tileCountX - 1;

        SetSearchList();

        isTargetFind = false;

        while (true)
        {
            for (int i = directionNum - 3; i <= directionNum + 4; i++)
            {

                int idx = i;

                if (i < 0) idx = i + 8;
                else if (i > 7) idx = i - 8;

                // 이 조건에 걸린다는 것은 패스 파인딩 루트가 전투 범위를 초과했다는 말입니다.
                if (currentTileX < friendlyXLeft) break;
                else if (currentTileX > friendlyXRight) break;
                else if (currentTileY < friendlyYDown) break;
                else if (currentTileY > friendlyYUp) break;

                int offsetX = currentTileX - platoonCommander.tileIdxX;
                int offsetY = currentTileY - platoonCommander.tileIdxY;

                if (!DecisionMoveBySearchListForFindAttackMovePath((PlatoonMoveDirection)idx, offsetX, offsetY)) continue; // 가능 방향으로 갈 수 없다면 패스

                if (isTargetFind)
                {
                    return pathFindList; // 길찾기 성공
                }

                Tile tile = DecisionTileByDirection((PlatoonMoveDirection)idx, mainGame.GetTileByIdx(currentTileX, currentTileY));

                if (tile == null) continue;

                if (tile == targetTile)
                {
                    return pathFindList; // 길찾기 성공
                }

                if (tile.isHaveUnit && tile.haveUnit.platoon == enemyPlatoon)
                {
                    return pathFindList; // 길찾기 성공
                }

                if (closePathDic.ContainsKey(tile)) continue; // 지나간 타일이었다면 패스

                float distance = Vector3.Distance(new Vector3(targetTile.idxX, targetTile.idxY, 0), new Vector3(tile.idxX, tile.idxY, 0));

                PathStruct dir = new PathStruct();
                dir.H = distance;
                dir.dir = (PlatoonMoveDirection)idx;
                dir.tile = tile;
                dir.idxX = destinationX;
                dir.idxY = platoonCommander.tileIdxY;
                dirList.Add(dir);
            }

            if (dirList.Count > 0)
            {
                List<PathStruct> list = dirList.OrderBy(i => i.H).ToList(); // 타일의 H 거리값 정렬
                directionNum = (int)list[0].dir;
                pathFindList.Add(list[0]); // 가장 빠른 타일 방향
                closePathDic.Add(mainGame.GetTileByIdx(currentTileX, currentTileY), 0);
                currentTileX = list[0].tile.idxX;
                currentTileY = list[0].tile.idxY;
                dirList.Clear();
            }
            else
            {
                // 길찾기 실패입니다.
                pathFindList.Clear();
                return pathFindList;
            }
        }
    }

    // 인식 범위에서 부대 타겟을 향해 나아갈 때 사용되는 패스파인딩
    public List<PathStruct> SetCompanyPathFindListToEnemyPlatoon(Platoon enemyPlatoon)
    {

        Tile targetTile = mainGame.GetTileByIdx(enemyPlatoon.platoonCommander.tileIdxX, enemyPlatoon.platoonCommander.tileIdxY);
        int directionNum = 0;

        dirList.Clear();

        List<PathStruct> pathFindList = new List<PathStruct>();

        closePathDic.Clear();

        int currentTileX = platoonCommander.tileIdxX;
        int currentTileY = platoonCommander.tileIdxY;

        int destinationX = 0;

        if (isRed) destinationX = mainGame.tileCountX - 1;

        SetSearchList();

        isTargetFind = false;

        while (true)
        {
            for (int i = directionNum - 3; i <= directionNum + 4; i++)
            {

                int idx = i;

                if (i < 0) idx = i + 8;
                else if (i > 7) idx = i - 8;

                int offsetX = currentTileX - platoonCommander.tileIdxX;
                int offsetY = currentTileY - platoonCommander.tileIdxY;

                if (!DecisionMoveBySearchListForFindAttackMovePath((PlatoonMoveDirection)idx, offsetX, offsetY)) continue; // 가능 방향으로 갈 수 없다면 패스

                if (isTargetFind)
                {
                    return pathFindList; // 길찾기 성공
                }

                Tile tile = DecisionTileByDirection((PlatoonMoveDirection)idx, mainGame.GetTileByIdx(currentTileX, currentTileY));

                if (tile == null) continue;

                if (tile == targetTile)
                {
                    return pathFindList; // 길찾기 성공
                }

                if (tile.isHaveUnit && tile.haveUnit.platoon == enemyPlatoon)
                {
                    return pathFindList; // 길찾기 성공
                }

                if (closePathDic.ContainsKey(tile)) continue; // 지나간 타일이었다면 패스

                float distance = Vector3.Distance(new Vector3(targetTile.idxX, targetTile.idxY, 0), new Vector3(tile.idxX, tile.idxY, 0));

                PathStruct dir = new PathStruct();
                dir.H = distance;
                dir.dir = (PlatoonMoveDirection)idx;
                dir.tile = tile;
                dir.idxX = tile.idxX;
                dir.idxY = tile.idxY;
                dirList.Add(dir);
            }

            if (dirList.Count > 0)
            {
                List<PathStruct> list = dirList.OrderBy(i => i.H).ToList(); // 타일의 H 거리값 정렬
                directionNum = (int)list[0].dir;
                pathFindList.Add(list[0]); // 가장 빠른 타일 방향
                closePathDic.Add(mainGame.GetTileByIdx(currentTileX, currentTileY), 0);
                currentTileX = list[0].idxX;
                currentTileY = list[0].idxY;
                dirList.Clear();
            }
            else
            {
                // 길찾기 실패입니다.
                //pathFindList.Clear();
                return pathFindList;
            }
        }
    }

    // 특정 타일을 향한 패스파인딩 (중대 패스파인딩의 시작점을 목표로 소대별 추가 루트를 만들어내는 용도)
    public List<PathStruct> SetCompanyPathFindListToTile(Tile targetTile)
    {
        int directionNum = 0;

        dirList.Clear();

        List<PathStruct> pathFindList = new List<PathStruct>();

        closePathDic.Clear();

        int currentTileX = platoonCommander.tileIdxX;
        int currentTileY = platoonCommander.tileIdxY;

        int destinationX = 0;

        if (isRed) destinationX = mainGame.tileCountX - 1;

        SetSearchList();

        while (true)
        {
            for (int i = directionNum - 3; i <= directionNum + 4; i++)
            {

                int idx = i;

                if (i < 0) idx = i + 8;
                else if (i > 7) idx = i - 8;

                int offsetX = currentTileX - platoonCommander.tileIdxX;
                int offsetY = currentTileY - platoonCommander.tileIdxY;

                if (!DecisionMoveBySearchListForMovePath((PlatoonMoveDirection)idx, offsetX, offsetY)) continue; // 가능 방향으로 갈 수 없다면 패스

                Tile tile = DecisionTileByDirection((PlatoonMoveDirection)idx, mainGame.GetTileByIdx(currentTileX, currentTileY));

                if (tile == null)
                {
                    // 길찾기 실패입니다.
                    pathFindList.Clear();
                    return pathFindList;
                }

                if (tile == targetTile)
                {
                    return pathFindList;
                }

                if (closePathDic.ContainsKey(tile)) continue; // 지나간 타일이었다면 패스

                float distance = Vector3.Distance(new Vector3(targetTile.idxX, targetTile.idxY, 0), new Vector3(tile.idxX, tile.idxY, 0));

                PathStruct dir = new PathStruct();
                dir.H = distance;
                dir.dir = (PlatoonMoveDirection)idx;
                dir.tile = tile;
                dir.idxX = tile.idxX;
                dir.idxY = tile.idxY;
                dirList.Add(dir);
            }

            if (dirList.Count > 0)
            {
                List<PathStruct> list = dirList.OrderBy(i => i.H).ToList(); // 타일의 H 거리값 정렬
                directionNum = (int)list[0].dir;
                pathFindList.Add(list[0]); // 가장 빠른 타일 방향
                closePathDic.Add(mainGame.GetTileByIdx(currentTileX, currentTileY), 0);
                currentTileX = list[0].idxX;
                currentTileY = list[0].idxY;
                dirList.Clear();
            }
            else
            {
                // 길찾기 실패입니다.
                pathFindList.Clear();
                return pathFindList;
            }
        }
    }

    public void SetSearchList()
    {
        searchListLeft.Clear();
        searchListRight.Clear();
        searchListUp.Clear();
        searchListDown.Clear();
        searchListLeftUp.Clear();
        searchListLeftDown.Clear();
        searchListRightUp.Clear();
        searchListRightDown.Clear();

        for (int i = 0; i < unitList.Count; i++)
        {
            if (unitList[i].platoonPos == UnitPos.CENTER || unitList[i].platoonPos == UnitPos.POS_UNKNOW) continue;

            SearchArea search = new SearchArea();
            search.tileIdxX = unitList[i].tileIdxX;
            search.tileIdxY = unitList[i].tileIdxY;
            search.pos = unitList[i].platoonPos;

            switch (unitList[i].platoonPos)
            {
                case UnitPos.LEFT:
                    searchListLeft.Add(search);
                    searchListLeftDown.Add(search);
                    searchListLeftUp.Add(search);
                    break;
                case UnitPos.RIGHT:
                    searchListRight.Add(search);
                    searchListRightDown.Add(search);
                    searchListRightUp.Add(search);
                    break;
                case UnitPos.UP:
                    searchListUp.Add(search);
                    searchListLeftUp.Add(search);
                    searchListRightUp.Add(search);
                    break;
                case UnitPos.DOWN:
                    searchListDown.Add(search);
                    searchListLeftDown.Add(search);
                    searchListRightDown.Add(search);
                    break;
                case UnitPos.LEFT_DOWN:
                    searchListLeft.Add(search);
                    searchListDown.Add(search);
                    searchListLeftDown.Add(search);
                    searchListLeftUp.Add(search);
                    searchListRightDown.Add(search);
                    break;
                case UnitPos.LEFT_UP:
                    searchListLeft.Add(search);
                    searchListUp.Add(search);
                    searchListLeftDown.Add(search);
                    searchListLeftUp.Add(search);
                    searchListRightUp.Add(search);
                    break;
                case UnitPos.RIGHT_DOWN:
                    searchListRight.Add(search);
                    searchListDown.Add(search);
                    searchListLeftDown.Add(search);
                    searchListRightDown.Add(search);
                    searchListRightUp.Add(search);
                    break;
                case UnitPos.RIGHT_UP:
                    searchListRight.Add(search);
                    searchListUp.Add(search);
                    searchListLeftUp.Add(search);
                    searchListRightDown.Add(search);
                    searchListRightUp.Add(search);
                    break;
            }
        }
    }

    public bool DecisionMoveBySearchListForMovePath(PlatoonMoveDirection direction, int offsetX, int offsetY)
    {
        List<SearchArea> searchList = null;
        switch (direction)
        {
            case PlatoonMoveDirection.DIR_LEFT:
                searchList = searchListLeft;
                break;
            case PlatoonMoveDirection.DIR_RIGHT:
                searchList = searchListRight;
                break;
            case PlatoonMoveDirection.DIR_UP:
                searchList = searchListUp;
                break;
            case PlatoonMoveDirection.DIR_DOWN:
                searchList = searchListDown;
                break;
            case PlatoonMoveDirection.DIR_LEFT_DOWN:
                searchList = searchListLeftDown;
                break;
            case PlatoonMoveDirection.DIR_LEFT_UP:
                searchList = searchListLeftUp;
                break;
            case PlatoonMoveDirection.DIR_RIGHT_DOWN:
                searchList = searchListRightDown;
                break;
            case PlatoonMoveDirection. DIR_RIGHT_UP:
                searchList = searchListRightUp;
                break;
        }

        for (int i = 0; i < searchList.Count; i++)
        {
            int tileX = searchList[i].tileIdxX + offsetX;
            int tileY = searchList[i].tileIdxY + offsetY;

            if (tileX < 0 || tileX >= mainGame.tileCountX) return false;
            if (tileY < 0 || tileY >= mainGame.tileCountY) return false;

            Tile tile = DecisionTileByDirection(direction, mainGame.GetTileByIdx(tileX, tileY));

            if (tile == null) return false;
            if (tile.isHaveUnit && tile.haveUnit.platoon != this)
            {
                if (tile.haveUnit.isRed == isRed && tile.haveUnit.platoon.companyNum != companyNum) return false;
            }
        }

        return true;
    }

    public bool DecisionMoveBySearchListForFindAttackMovePath(PlatoonMoveDirection direction, int offsetX, int offsetY, Platoon target = null)
    {
        List<SearchArea> searchList = null;
        switch (direction)
        {
            case PlatoonMoveDirection.DIR_LEFT:
                searchList = searchListLeft;
                break;
            case PlatoonMoveDirection.DIR_RIGHT:
                searchList = searchListRight;
                break;
            case PlatoonMoveDirection.DIR_UP:
                searchList = searchListUp;
                break;
            case PlatoonMoveDirection.DIR_DOWN:
                searchList = searchListDown;
                break;
            case PlatoonMoveDirection.DIR_LEFT_DOWN:
                searchList = searchListLeftDown;
                break;
            case PlatoonMoveDirection.DIR_LEFT_UP:
                searchList = searchListLeftUp;
                break;
            case PlatoonMoveDirection.DIR_RIGHT_DOWN:
                searchList = searchListRightDown;
                break;
            case PlatoonMoveDirection.DIR_RIGHT_UP:
                searchList = searchListRightUp;
                break;
        }

        for (int i = 0; i < searchList.Count; i++)
        {
            int tileX = searchList[i].tileIdxX + offsetX;
            int tileY = searchList[i].tileIdxY + offsetY;

            if (tileX < 0 || tileX >= mainGame.tileCountX) return false;
            if (tileY < 0 || tileY >= mainGame.tileCountY) return false;

            Tile tile = DecisionTileByDirection(direction, mainGame.GetTileByIdx(tileX, tileY));

            if (tile == null) return false;
            if (tile.isHaveUnit && tile.haveUnit.platoon != this)
            {
                if (tile.haveUnit.isRed == isRed) return false;
                else if (target != null && tile.haveUnit.platoon == target)
                {
                    isTargetFind = true;
                    return true;
                }
            }
        }

        return true;
    }

    public void SetPlatoonOffset()
    {
        if (companyCommander == this)
        {
            platoonOffsetY = 0;
        }
        else
        {
            platoonOffsetY = companyCommander.platoonCommander.tileIdxY - platoonCommander.tileIdxY;
        }
    }

    public bool NeedsToPathFind(PlatoonPathFind pathFind)
    {
        PlatoonMoveDirection direction = PlatoonMoveDirection.DIR_DEFAULT;

        bool needObjCheck = true;

        switch (pathFind)
        {
            case PlatoonPathFind.MOVE_PATH_FIND:
                platoonStatus = PlatoonStatus.UNIT_MOVE;
                direction = PlatoonMoveDirection.DIR_LEFT;
                if (isRed) direction = PlatoonMoveDirection.DIR_RIGHT;
                break;
            case PlatoonPathFind.FIND_MOVE_PATH_FIND:
                platoonStatus = PlatoonStatus.UNIT_FIND_MOVE;
                Platoon companyTarget = mainGame.GetCompanyTarget(companyNum, isRed);
                direction = DecisionDirectionToCompnayTarget(companyTarget);
                break;
            case PlatoonPathFind.ATTACK_MOVE_PATH_FIND:
                if (platoonPath.Count > 0)
                {
                    PlatoonTargetIdxX = platoonPath[0].tile.idxX;
                    PlatoonTargetIdxY = platoonPath[0].tile.idxY;
                    direction = PlatoonDirectionDecision();

                    if (DecisionMoveByDirection(direction)) return false;
                }
                else
                {
                    direction = DecisionDirectionToCompnayTarget(targetPlatoon);
                    needObjCheck = false;
                }
                platoonStatus = PlatoonStatus.UNIT_ATTACK_MOVE;
                break;
        }

        return !DecisionMoveByDirection(direction, needObjCheck);
    }

    public void SetPlatoonPath(List<PathStruct> companyPath)
    {
        platoonPath = new List<PathStruct>(companyPath);
        List<PathStruct> list = new List<PathStruct>(companyPath);

        // 현재 위치가 길찾기 루트내에 위치한지 체크, 또는 X위치가 같은지 체크하여 길찾기 루트를 수정
        int pathIdx = 0;
        int offsetX = 0;
        int offsetY = 0;

        for (int i = 0; i < platoonPath.Count; i++)
        {
            if (platoonPath[i].tile.idxX == platoonCommander.tileIdxX && platoonPath[i].tile.idxY == platoonCommander.tileIdxY)
            {
                pathIdx = i;
                break;
            }
            else if (platoonPath[i].tile.idxX == platoonCommander.tileIdxX)
            {
                offsetX = i;
            }
            else if (platoonPath[i].tile.idxY == platoonCommander.tileIdxY)
            {
                offsetY = i;
            }
        }

        newPathFind = false;

        if (pathIdx != 0)
        {
            list.RemoveRange(0, pathIdx + 1);

            platoonPath = list;
            int count = platoonPath.Count;
        }
        else if (platoonPath.Count > 0)
        {
            if (offsetX < offsetY) offsetX = offsetY; // 와이축이 일치하는 루트가 엑스축이 일치하는 루트보다 직선이동이므로 루트를 자를 인덱스를 변경합니다.

            list.RemoveRange(0, offsetX);

            List<PathStruct> tempList = SetCompanyPathFindListToTile(platoonPath[offsetX].tile);
            tempList.AddRange(list);

            platoonPath = tempList;
        }
    }

    public void SetPlatoonAttackPath(List<PathStruct> companyPath)
    {
        if (companyPath.Count <= 0 && companyCommander.platoonStatus != PlatoonStatus.UNIT_ATTACK && companyCommander.platoonStatus != PlatoonStatus.UNIT_ATTACK_READY) return; // 부대장 길찾기 루트가 나오지 않았다는 뜻

        if (targetPlatoon == null) return; // 아직 전투 범위 안에 타겟이 없다는 뜻

        newPathFind = false;

        platoonPath.Clear();

        platoonPath = SetPlatoonPathFindListToEnemyPlatoon(targetPlatoon); // 소대별 각각 패스파인딩

        if (platoonPath.Count <= 0)
        {
            SetPlatoonPath(companyPath);
        }
    }

    public PlatoonMoveDirection DecisionDirectionToCompnayTarget(Platoon companyTarget)
    {
        PlatoonTargetIdxX = companyTarget.platoonCommander.tileIdxX;
        PlatoonTargetIdxY = companyTarget.platoonCommander.tileIdxY;

        return PlatoonDirectionDecision();
    }

    public void SetCompanyPathList(PlatoonPathFind pathFind)
    {
        newPathFind = false;

        switch (pathFind)
        {
            case PlatoonPathFind.MOVE_PATH_FIND:
                PlatoonMoveDirection direction = PlatoonMoveDirection.DIR_LEFT;
                if (isRed) direction = PlatoonMoveDirection.DIR_RIGHT;
                // platoonPath = SetCompanyPathFindListToDirection(direction);
                
                if (companyCommander == this && companyNum == 0)
                {
                    JumpPointSearch(mainGame.GetTileByIdx(mainGame.tileCountX - 3, platoonCommander.tileIdxY));
                }
                break;
            case PlatoonPathFind.FIND_MOVE_PATH_FIND:
                Platoon target = mainGame.GetCompanyTarget(companyNum, isRed);
                platoonPath = SetCompanyPathFindListToEnemyPlatoon(target);
                break;
            case PlatoonPathFind.ATTACK_MOVE_PATH_FIND:
                if (targetPlatoon == null)
                {
                    Platoon companyTarget = mainGame.GetCompanyTarget(companyNum, isRed);
                    platoonPath = SetCompanyPathFindListToEnemyPlatoon(companyTarget);
                    pathFind = PlatoonPathFind.FIND_MOVE_PATH_FIND;
                }
                else
                {
                    platoonPath = SetPlatoonPathFindListToEnemyPlatoon(targetPlatoon);
                }
                break;
        }

        mainGame.SetTestFind(companyNum, isRed);

        // mainGame.SetCompanyMovePath(companyNum, isRed, platoonPath, pathFind);
    }

    public void CheckBattleArea(List<Company> enemyCompanyList)
    {
        battleAreaList.Clear();

        for (int i = 0; i < enemyCompanyList.Count; i++)
        {
            List<Platoon> enemyPlatoon = enemyCompanyList[i].platoonList;

            for (int j = 0; j < enemyPlatoon.Count; j++)
            {
                Tile tile = mainGame.GetTileByIdx(enemyPlatoon[j].platoonCommander.tileIdxX, enemyPlatoon[j].platoonCommander.tileIdxY);

                int enemyX = tile.idxX;
                int enemyY = tile.idxY;

                int friendlyXLeft = platoonCommander.tileIdxX - (battleAreaX + 4);
                int friendlyXRight = platoonCommander.tileIdxX + battleAreaX;
                int friendlyYUp = platoonCommander.tileIdxY + battleAreaY + 2;
                int friendlyYDown = platoonCommander.tileIdxY - battleAreaY - 2;

                if (friendlyXLeft <= enemyX && enemyX <= friendlyXRight && friendlyYDown <= enemyY && enemyY <= friendlyYUp)
                {
                    float distance = Vector3.Distance(new Vector3(enemyX, enemyY, 0), new Vector3(platoonCommander.tileIdxX, platoonCommander.tileIdxY, 0));

                    PathStruct path = new PathStruct();
                    path.platoon = enemyPlatoon[j];
                    path.H = distance;
                    battleAreaList.Add(path);
                }
            }
        }

        if (battleAreaList.Count > 0)
        {
            List<PathStruct> list = battleAreaList.OrderBy(i => i.H).ToList(); // 타일의 H 거리값 정렬

            Unit enemy = CheckEnemyTile();

            if (enemy != null)
            {
                targetPlatoon = enemy.platoon;
                platoonStatus = PlatoonStatus.UNIT_ATTACK_READY;
                Debug.Log("공격준비~3 : " + isRed + "팀/" + companyNum);
            }
            else
            {
                targetPlatoon = list[0].platoon;
                platoonStatus = PlatoonStatus.UNIT_ATTACK_MOVE;
            }
        }
        else
        {
            targetPlatoon = null;
            platoonStatus = PlatoonStatus.UNIT_IDLE;
        }
    }

    public Unit CheckTargetNearBy(PlatoonMoveDirection direction, Tile tile)
    {
        Unit target = null;

        Tile targetTile = DecisionTileByDirection(direction, tile);

        if (targetTile != null && targetTile.isHaveUnit)
        {
            if (targetTile.haveUnit.isRed != isRed)
            {
                target = targetTile.haveUnit.platoonCommander;
            }
        }

        return target;
    }

    Unit GetUnitByPlatoonPos(UnitPos pos)
    {
        for (int i = 0; i < unitList.Count; i++)
        {
            if (unitList[i].platoonPos == pos)
            {
                return unitList[i];
            }
        }

        return null;
    }

    public void SetPlatoonCenterPos()
    {
        Unit leftUpUnit = GetUnitByPlatoonPos(UnitPos.LEFT_UP);
        Unit rightDownUnit = GetUnitByPlatoonPos(UnitPos.RIGHT_DOWN);

        if (platoonLine <= 1)
        {
            if (leftUpUnit == null) leftUpUnit = GetUnitByPlatoonPos(UnitPos.RIGHT_UP);
            if (rightDownUnit == null) rightDownUnit = GetUnitByPlatoonPos(UnitPos.LEFT_DOWN);
        }

        if (leftUpUnit == null || rightDownUnit == null)
        {
            centerPosX = platoonCommander.tileIdxX;
            centerPosY = platoonCommander.tileIdxY;
            return;
        }

        int left = leftUpUnit.tileIdxX;
        int right = rightDownUnit.tileIdxX;
        int up = leftUpUnit.tileIdxY;
        int down = rightDownUnit.tileIdxY;

        centerPosX = left + ((right - left) / 2.0f);
        centerPosY = down + ((up - down) / 2.0f);
    }

    public void JumpPointSearch(Tile targetTile, bool useAttackPath = false)
    {
        finishJPS = false;

        openDic.Clear();
        closeDic.Clear();

        Tile currentTile = mainGame.GetTileByIdx(platoonCommander.tileIdxX, platoonCommander.tileIdxY);

        JPS_Struct start = new JPS_Struct();
        start.f = 0;
        start.g = 0;
        start.h = 0;
        start.pTile = null;
        start.direciton = PlatoonMoveDirection.DIR_DEFAULT;
        start.tile = currentTile;

        openDic.Add(currentTile, start);

        int Count = 0;

        while (true)
        {
            openDic = openDic.OrderBy(i => i.Value.f).ToDictionary(pair => pair.Key, pair => pair.Value); // 타일의 H 거리값 정렬

            JPS_Struct currentNode = openDic.FirstOrDefault().Value;

            if (closeDic.ContainsKey(currentNode.tile))
            {
                int test = 0;
            }
             else closeDic.Add(currentNode.tile, currentNode);

            openDic.Clear();

            if (currentNode.tile == targetTile)
            {
                //openList.Clear();
                break;
            }

            openDic.Remove(currentNode.tile);

            Count++;
            Debug.Log("테스트 : " + Count);

            PlatoonMoveDirection backDirection = PlatoonMoveDirection.DIR_DEFAULT;

            if (currentNode.pTile != null) backDirection = DirectionDecisionToTarget(currentNode.tile, currentNode.pTile);

            for (int i = 0; i < 8; i++)
            {
                if (i == (int)backDirection) continue;
                PlatoonMoveDirection direciton = (PlatoonMoveDirection)i;

                SearchJumpPointByDirection(direciton, currentNode.tile, targetTile, backDirection, useAttackPath);
            }
        }

         Debug.Log("Test");


    }

    public void SearchJumpPointByDirection(PlatoonMoveDirection direction, Tile current, Tile target, PlatoonMoveDirection backDirection, bool useAttackPath = false)
    {
        int forwardNum = (int)direction;

        int offsetX = 0;
        int offsetY = 0;

        int idx = 0;
        int closeCount = 0;

        Tile startTile = current;
        Tile currentTile = startTile;

        int friendlyXLeft = platoonCommander.tileIdxX - (battleAreaX + 4);
        int friendlyXRight = platoonCommander.tileIdxX + battleAreaX;
        int friendlyYUp = platoonCommander.tileIdxY + battleAreaY + 2;
        int friendlyYDown = platoonCommander.tileIdxY - battleAreaY - 2;

        /*
        DIR_RIGHT = 0, // 우방향
        DIR_RIGHT_DOWN, // 우아래 방향
        DIR_DOWN, // 아래 방향
        DIR_LEFT_DOWN, // 좌아래 방향
        DIR_LEFT, // 좌방향
        DIR_LEFT_UP, // 좌상 방향
        DIR_UP, // 윗 방향
        DIR_RIGHT_UP, // 우상 방향
        DIR_DEFAULT // 방향 없음. 보통 제자리?
        */

        SetSearchList();

        List<JPS_Struct> checkList = new List<JPS_Struct>();


        while (checkList.Count == 0)
        {
            for (int i = forwardNum - 1; i <= forwardNum + 1; i++)
            {
                idx = i;
                if (idx < 0) idx += 8;
                else if (idx > 7) idx -= 8;

                if ((int)backDirection == idx) continue;

                if (useAttackPath)
                {
                    if (offsetX < friendlyXLeft)
                    {
                        continue;
                    }
                    else if (offsetY > friendlyXRight)
                    {
                        continue;
                    }
                    else if (offsetX < friendlyYDown)
                    {
                        continue;
                    }
                    else if (offsetY > friendlyYUp)
                    {
                        continue;
                    }

                    if (!DecisionMoveBySearchListForFindAttackMovePath((PlatoonMoveDirection)idx, offsetX, offsetY))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!DecisionMoveBySearchListForMovePath((PlatoonMoveDirection)idx, offsetX, offsetY))
                    {
                        continue;
                    }
                }

                Tile tempTile = DecisionTileByDirection((PlatoonMoveDirection)idx, mainGame.GetTileByIdx(startTile.idxX + offsetX, startTile.idxY + offsetY));

                if (tempTile == null) continue;

                JPS_Struct node = new JPS_Struct();
                node.pTile = current;
                node.g = Vector3.Distance(new Vector3(tempTile.idxX, tempTile.idxY), new Vector3(startTile.idxX, startTile.idxY));
                node.h = Vector3.Distance(new Vector3(target.idxX, target.idxY), new Vector3(tempTile.idxX, tempTile.idxY));
                node.f = node.g + node.h;
                node.tile = tempTile;
                node.direciton = (PlatoonMoveDirection)idx;

                checkList.Add(node);
            }

            if (checkList.Count >= 3) // 전부 뚫려있는 경우이므로 해당 방향으로 계속 나아갑니다.
            {
                currentTile = DecisionTileByDirection(direction, mainGame.GetTileByIdx(startTile.idxX + offsetX, startTile.idxY + offsetY));

                if (openDic.ContainsKey(currentTile)) return;

                offsetX = currentTile.idxX - startTile.idxX;
                offsetY = currentTile.idxY - startTile.idxY;
            }
            else if (checkList.Count == 2) // 점프 포인트를 찾은 경우입니다. (복수이기 때문에 둘을 비교해 봅니다.)
            {
                Tile jumpTile = null;
                if (checkList[0].f <= checkList[1].f)
                {
                    jumpTile = checkList[0].tile;

                    if (jumpTile == startTile) return;
                    if (openDic.ContainsKey(jumpTile)) return;
                    if (closeDic.ContainsKey(jumpTile)) return;

                    openDic.Add(jumpTile, checkList[0]);
                }
                else
                {
                    jumpTile = checkList[1].tile;

                    if (jumpTile == startTile) return;
                    if (openDic.ContainsKey(jumpTile)) return;
                    if (closeDic.ContainsKey(jumpTile)) return;

                    openDic.Add(jumpTile, checkList[1]);
                }
            }
            else if (checkList.Count == 1) // 점프 포인트를 한개 찾은 경우입니다.
            {
                Tile jumpTile = checkList[0].tile;

                if (jumpTile == startTile) return;
                if (openDic.ContainsKey(jumpTile)) return;
                if (closeDic.ContainsKey(jumpTile)) return;

                openDic.Add(jumpTile, checkList[0]);
            }
            else // 데드 엔드 (갈곳이 없는 막힌 곳)
            {
                Debug.Log("JPS 데드 엔드!!!!!!!!!!!!!!!");
                return;
            }
        }
    }
}