using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuickPool;
using UnityEngine.UI;
using System;
using System.Threading;
using System.Linq;
using System.Net;

public struct SearchPlatoon
{
    public Platoon platoon; // 적 중대의 소대장 유닛
    public float H; // 휴리스틱값
}

public class MainGame : MonoBehaviour
{
    public Text fpsText; // 프레임 표기 텍스트 유아이

    public Text turnText; // 턴 카운트 표기 텍스트 유아이

    public float Tick = 0.0f; // 지연

    public int tileCountX; // X축 총 타일 갯수

    public int tileCountY; // Y축 총 타일 갯수

    public Tile[,] tileArray; // 타일 2차원 배열

    public int turnCount; // 턴 카운트

    float deltaTime = 0.0f; // 프레임 표기에 사용되는 델타 타임

    public Canvas uiCanvas; // UI 캔버스

    public Camera mainCamera; // 메인 카메라

    public Camera uiCamera; // 유아이용 카메라
    
    public int platoonIdx = 0; // 소대 관련 루틴을 작동시키는 인덱스
    public int companyIdx = 0; // 중대 관련 루틴을 작동시키는 인덱스

    public List<Company> redCompanyList; // 레드 중대 리스트
    public List<Company> blueCompanyList; // 블루 중대 리스트

    public Action searchCallback;

    public Action waitCallback;

    public Button restartButton;

    public Dictionary<int, TroopSelect> troopSelectDic; // 중대 번호, 

    public Dictionary<int, TroopSelect> redSelectDic; // 레드팀 

    public Dictionary<int, TroopSelect> blueSelectDic; // 블루팀

    WaitForEndOfFrame wait;

    Vector2 camPos;

    public Toggle dmgToggle;

    public bool isDamgeReduce;

    public const int searchX = 70;

    bool isTestBlockSpawn = false;

    bool isGameStart;

    private const float TICK_TIMER_MAX = 1.0f / 60.0f;

    public int tick;
    private float tickTimer;

    void Awake()
    {
        isGameStart = false;

        tick = 0;

        isDamgeReduce = true;
        dmgToggle.onValueChanged.AddListener((value) =>
        {
            ChangeReduceDmgValue(value);
        }
        );

        camPos = mainCamera.transform.position;

        Application.targetFrameRate = 300;

        wait = new WaitForEndOfFrame();

        tileArray = new Tile[tileCountX, tileCountY];

        for (int i = 0; i < tileCountX; i++)
        {
            for (int j = 0; j < tileCountY; j++)
            {
                Tile tile = new Tile();
                tile.idxX = i;
                tile.idxY = j;
                tile.posX = i * 0.12f;
                tile.posY = j * 0.12f;
                tile.isHaveUnit = false;
                tile.haveUnit = null;
                tileArray[i, j] = tile;
            }
        }

        turnCount = 0;

        restartButton.interactable = false;
    }

    public void GameStart(Dictionary<int, TroopSelect> troopSelectDic)
    {
#if UNITY_EDITOR
        Camera.main.orthographicSize = 10;
#endif

        mainCamera.transform.position = new Vector3(camPos.x, camPos.y, mainCamera.transform.position.z);
        turnText.text = "Turn : " + turnCount;
        this.troopSelectDic = troopSelectDic;

        bool isAllEmpty = true;

        foreach (KeyValuePair<int, TroopSelect> troop in troopSelectDic)
        {
            if (!troop.Value.isEmpty)
            {
                isAllEmpty = false;
                break;
            }
        }

        if (!isTestBlockSpawn)
        {
            TestBlock(0, 0);
            TestBlock(tileCountX - 1, 0);
            TestBlock(tileCountX - 1, tileCountY - 1);
            TestBlock(0, tileCountY - 1);
            isTestBlockSpawn = true;
        }

        if (isAllEmpty) ButtonEnable();
        else Invoke("UnitSetting", 0.5f);
    }

    void LateUpdate()
    {
        ShowFps();
    }

    // FPS 표기
    void ShowFps()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        fpsText.color = Color.red;

        fpsText.text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
    }

    // 턴 카운트 표기
    void SetTurnCount()
    {
        turnCount++;
        turnText.text = "Turn : " + turnCount;
    }

    void UnitSetting()
    {
        redCompanyList = new List<Company>();

        blueCompanyList = new List<Company>();

        Dictionary<int, TroopSelect> redTroopDic = DecisionFormation();
        Dictionary<int, TroopSelect> blueTroopDic = DecisionFormation(false);

        redTroopDic = SortCenterLineCompany(redTroopDic);
        blueTroopDic = SortCenterLineCompany(blueTroopDic, false);

        int count = 0;
        foreach (KeyValuePair<int, TroopSelect> troop in redTroopDic)
        {
            CompanySpawn(troop.Value.mos, troop.Value.platoonSize, count, troop.Key, true);
            count++;
        }

        for (int i = 0; i < redCompanyList.Count; i++)
        {
            CompanyTxtSetting(redCompanyList[i]);
        }

        count = 0;
        foreach (KeyValuePair<int, TroopSelect> troop in blueTroopDic)
        {
            CompanySpawn(troop.Value.mos, troop.Value.platoonSize, count, troop.Key, false);
            count++;
        }

        for (int i = 0; i < blueCompanyList.Count; i++)
        {
            CompanyTxtSetting(blueCompanyList[i]);
        }

        Invoke("StartGame", 0.5f);
        Invoke("ButtonEnable", 0.5f);
    }

    void ButtonEnable()
    {
        restartButton.interactable = true;
    }

    void StartGame()
    {
        isGameStart = true;
    }

    // 턴 스타트
    void OnStartTurn()
    {
        //SetTurnCount(); // 턴 카운트를 하나 올리고 표기합니다

        //OnUserInput();
    }

     // 유저 입력 받는 페이즈
    void OnUserInput()
    {
        OnUseRedMagic();
    }

    // 레드팀 마법 사용 페이즈
    void OnUseRedMagic()
    {
        OnUseBlueMagic();
    }

    // 블루팀 마법 사용 페이즈
    void OnUseBlueMagic()
    {
        // OnMoveRedTeam();
        //OnSearchEnemyRedTem();
    }

    // 레드팀 색적 페이즈
    void OnSearchEnemyRedTem()
    {
        SearchEnemyInList(true, delegate { OnSearchEnemyBlueTem(); });
    }

    // 블루팀 색적 페이즈
    void OnSearchEnemyBlueTem()
    {
        SearchEnemyInList(false, null);
    }

    void OnCheckRedTeamNeedPathFind(PlatoonMoveSpeed speed)
    {
        for (int i = 0; i < redCompanyList.Count; i++)
        {
            if (!redCompanyList[i].newPathFind) continue;

            if (redCompanyList[i].speed != speed) continue;

            for (int j = 0; j < redCompanyList[i].platoonList.Count; j++)
            {
                if (redCompanyList[i].platoonList[j].NeedsToPathFind())
                {
                    redCompanyList[i].companyCommander.PathCheck();
                    break;
                }
            }
        }

        OnCheckBlueTeamNeedPathFind(speed);
    }

    void OnCheckBlueTeamNeedPathFind(PlatoonMoveSpeed speed)
    {
        for (int i = 0; i < blueCompanyList.Count; i++)
        {
            if (!blueCompanyList[i].newPathFind) continue;

            if (blueCompanyList[i].speed != speed) continue;

            for (int j = 0; j < blueCompanyList[i].platoonList.Count; j++)
            {
                if (blueCompanyList[i].platoonList[j].NeedsToPathFind())
                {
                    blueCompanyList[i].companyCommander.PathCheck();
                    break;
                }
            }
        }

        OnCheckMoveProcess(speed);
    }

    void OnCheckMoveProcess(PlatoonMoveSpeed speed)
    {
        /*
        for (int i = 0; i < redCompanyList.Count; i++)
        {
            if (redCompanyList[i].speed != speed) continue;

            CalcMoveProcess(redCompanyList[i]);
        }

        for (int i = 0; i < blueCompanyList.Count; i++)
        {
            if (blueCompanyList[i].speed != speed) continue;

            CalcMoveProcess(blueCompanyList[i]);
        }
        */

        OnMoveRedTeam(speed);
    }

    /*
    void OnRedTeamCheckNeedPathFind(PlatoonMoveSpeed speed)
    {
        if (companyIdx < redCompanyList.Count)
        {
            if (redCompanyList[companyIdx].newPathFind)
            {
                SetCompanyPathInList(redCompanyList[companyIdx].platoonList, speed, delegate { companyIdx++; OnRedTeamCheckNeedPathFind(speed); });
            }
            else
            {
                companyIdx++;
                OnRedTeamCheckNeedPathFind(speed);
            }
        }
        else
        {
            companyIdx = 0;
            OnBlueTeamCheckNeedPathFind(speed);
        }
    }

    void OnBlueTeamCheckNeedPathFind(PlatoonMoveSpeed speed)
    {
        if (companyIdx < blueCompanyList.Count)
        {
            if (blueCompanyList[companyIdx].newPathFind)
            {
                SetCompanyPathInList(blueCompanyList[companyIdx].platoonList, speed, delegate { companyIdx++; OnBlueTeamCheckNeedPathFind(speed); });
            }
            else
            {
                companyIdx++;
                OnBlueTeamCheckNeedPathFind(speed);
            }
        }
        else
        {
            companyIdx = 0;
            OnMoveRedTeam(speed);
        }
    }
    */

    // 레드팀 소대 이동 페이즈
    void OnMoveRedTeam(PlatoonMoveSpeed speed)
    {
        if (companyIdx < redCompanyList.Count)
        {
            MovePlatoonInList(redCompanyList[companyIdx].platoonList, speed, delegate { platoonIdx = 0; companyIdx++; OnMoveRedTeam(speed); });
        }
        else
        {
            companyIdx = 0;
            OnMoveBlueTeam(speed);
        }
    }

    // 블루팀 소대 이동 페이즈
    void OnMoveBlueTeam(PlatoonMoveSpeed speed)
    {
        if (companyIdx < blueCompanyList.Count)
        {
            MovePlatoonInList(blueCompanyList[companyIdx].platoonList, speed, delegate { platoonIdx = 0; companyIdx++; OnMoveBlueTeam(speed); });
        }
        else
        {
            companyIdx = 0;
        }
    }

    // 레드팀 소대 공격 페이즈
    void OnAttackRedTeam()
    {
        if (companyIdx < redCompanyList.Count)
        {
            // Debug.Log("OnAttackRedTeam()");
            AttackPlatoonInList(redCompanyList[companyIdx].platoonList, delegate { platoonIdx = 0; companyIdx++; OnAttackRedTeam(); });
        }
        else
        {
            companyIdx = 0;
            OnAttackBlueTeam();
        }
    }

    // 블루팀 소대 공격 페이즈
    void OnAttackBlueTeam()
    {
        if (companyIdx < blueCompanyList.Count)
        {
            // Debug.Log("OnAttackBlueTeam()");
            AttackPlatoonInList(blueCompanyList[companyIdx].platoonList, delegate { platoonIdx = 0; companyIdx++; OnAttackBlueTeam(); });
        }
        else
        {
            companyIdx = 0;
            OnCheckPlatoonDefeat();
        }
    }

    // 레드팀과 블루팀 소대중 전멸한 소대가 있는지 체크
    void OnCheckPlatoonDefeat()
    {
        for (int i = 0; i < redCompanyList.Count; i++)
        {
            CheckPlatoonDefeatInList(redCompanyList[i]);
        }

        for (int i = 0; i < blueCompanyList.Count; i++)
        {
            CheckPlatoonDefeatInList(blueCompanyList[i]);
        }

        OnTurnEnd();
    }

    // 턴 종료 (게임이 끝나지 않았다면 다음 턴으로 루틴 반복)
    void OnTurnEnd()
    {
        for (int i = 0; i < redCompanyList.Count; i++)
        {
            redCompanyList[i].newPathFind = true;
            redCompanyList[i].needPathFind = false;
            redCompanyList[i].pathList.Clear();

            for (int j = 0; j < redCompanyList[i].platoonList.Count; j++)
            {
                redCompanyList[i].platoonList[j].newPathFind = true;
                redCompanyList[i].platoonList[j].platoonPath.Clear();
            }
        }

        for (int i = 0; i < blueCompanyList.Count; i++)
        {
            blueCompanyList[i].newPathFind = true;
            blueCompanyList[i].needPathFind = false;
            blueCompanyList[i].pathList.Clear();

            for (int j = 0; j < blueCompanyList[i].platoonList.Count; j++)
            {
                blueCompanyList[i].platoonList[j].newPathFind = true;
                blueCompanyList[i].platoonList[j].platoonPath.Clear();
            }
        }

        SetTurnCount();
    }


    // 게임 종료 처리
    void OnGameEnd()
    { 
    }

    // 타일 인덱스로 해당 타일을 리턴
    public Tile GetTileByIdx(int idxX, int idxY)
    {
        return tileArray[idxX, idxY];
    }

    void SearchEnemyInList(bool isRed, Action action)
    {
        searchCallback = action;

        List<Company> firendCompanyList;
        List<Company> enemyCompanyList;

        if (isRed)
        {
            firendCompanyList = redCompanyList;
            enemyCompanyList = blueCompanyList;
        }
        else
        {
            firendCompanyList = blueCompanyList;
            enemyCompanyList = redCompanyList;
        }

        List<SearchPlatoon> searchList = new List<SearchPlatoon>();

        for (int i = 0; i < firendCompanyList.Count; i++)
        {
            if (firendCompanyList[i].targetPlatoon != null)
            {
                if (firendCompanyList[i].targetPlatoon.hp > 0) continue; // 해당 중대의 중대 목표가 있다면 검사를 수행할 필요가 없음
                else firendCompanyList[i].targetPlatoon = null; // 중대 목표가 사망한 상태이므로 중대 타겟 초기화
            }

            if (firendCompanyList[i].companyCommander == null) continue;

            Unit friendlyCommander = firendCompanyList[i].companyCommander.platoonCommander;

            if (firendCompanyList[i].companyCommander.hp <= 0) continue;

            for (int j = 0; j < enemyCompanyList.Count; j++)
            {
                List<Platoon> enemyPlatoonList = enemyCompanyList[j].platoonList;

                for (int k = 0; k < enemyPlatoonList.Count; k++)
                {
                    if (enemyPlatoonList[k].hp <= 0) continue;

                    Unit enemyCommander = enemyPlatoonList[k].platoonCommander;

                    if (isRed && enemyCommander.tileIdxX > friendlyCommander.tileIdxX + searchX) continue; // 레드팀일 때 색적 범위 밖이라면 팅겨냄
                    else if (!isRed && enemyCommander.tileIdxX < friendlyCommander.tileIdxX - searchX) continue; // 블루팀일 때 색적 범위 밖이라면 팅겨냄

                    // Tile currentTile = GetTileByIdx(friendlyCommander.tileIdxX, friendlyCommander.tileIdxY); // 현재 아군 위치 타일
                    // Tile targetTile = GetTileByIdx(enemyCommander.tileIdxX, enemyCommander.tileIdxY); // 적군 위치 타일

                    float distance = Vector3.Distance(new Vector3(enemyCommander.tileIdxX, enemyCommander.tileIdxY, 0), new Vector3(friendlyCommander.tileIdxX, friendlyCommander.tileIdxY, 0));

                    SearchPlatoon search = new SearchPlatoon();
                    search.H = distance;
                    search.platoon = enemyPlatoonList[k];
                    searchList.Add(search);
                }
            }

            if (searchList.Count > 0)
            {
                List<SearchPlatoon> list = searchList.OrderBy(k => k.H).ToList(); // 타일의 H 거리값 정렬
                firendCompanyList[i].targetPlatoon = list[0].platoon; // 가장 가까운 적을 중대 타겟으로
                searchList.Clear();
            }
        }

        if (searchCallback != null)
        {
            searchCallback();
        }
    }

    void SortPlatoonList(Company company)
    {
        List<Platoon> newPlatoonList = new List<Platoon>();
        List<Platoon> sortList = new List<Platoon>();

        newPlatoonList.Add(company.platoonList[0].companyCommander);

        for (int i = 0; i < company.platoonList.Count; i++)
        {
            if (company.platoonList[i].companyCommander == company.platoonList[i]) continue;

            sortList.Add(company.platoonList[i]);
        }

        if (newPlatoonList[0].isRed)
        {
            sortList = sortList.OrderByDescending(i => i.platoonCommander.tileIdxX).ToList();
        }
        else
        {
            sortList = sortList.OrderBy(i => i.platoonCommander.tileIdxX).ToList();
        }

        for (int i = 0; i < sortList.Count; i++)
        {
            newPlatoonList.Add(sortList[i]);
        }


        if (company.platoonList.Count != newPlatoonList.Count)
        {
            Debug.Log("Sort Platoon List Error!!!!!!!!!!!!!!");
        }
        else
        {
            company.platoonList = newPlatoonList;
        }
    }

    // 해당하는 리스트의 소대를 이동시킴
    void MovePlatoonInList(List<Platoon> list, PlatoonMoveSpeed speed, Action action)
    {
        if (platoonIdx < list.Count)
        {
            if (list[platoonIdx].platoonMoveSpeed == speed && list[platoonIdx].platoonStatus != PlatoonStatus.UNIT_ATTACK)
            {
                list[platoonIdx].Move(delegate { MovePlatoonInList(list, speed, action); });
            }
            else
            {
                platoonIdx++;
                MovePlatoonInList(list, speed, action);
            }
        }
        else
        {
            platoonIdx = 0; 
            action();
        }
    }
    
    // 해당하는 리스트의 소대를 공격수행
    void AttackPlatoonInList(List<Platoon> list, Action action)
    {
        if (platoonIdx < list.Count)
        {
            list[platoonIdx].Attack(delegate { AttackPlatoonInList(list, action); });
        }
        else
        {
            platoonIdx = 0; 
            action();
            //StartCoroutine(WaitTick(delegate { platoonIdx = 0; action(); }));
        }
    }

    // 해당하는 리스트의 소대가 전멸했는가? 체크. 살아있다면 남은 병사수로 새로 공격력을 계산합니다.
    void CheckPlatoonDefeatInList(Company company)
    {
        int idx = 0;

        List<Platoon> list = company.platoonList;

        while (idx < list.Count)
        {
            if (list[idx].hp <= 0)
            {
                if (list[idx] == company.companyCommander)
                {
                    Tile currentTile = GetTileByIdx(company.companyCommander.platoonCommander.tileIdxX, company.companyCommander.platoonCommander.tileIdxY);
                    company.companyCommander = AppointmentCompanyLeaderInList(company.platoonList, currentTile);

                    if (company.companyCommander != null)
                    {
                        company.targetPlatoon = company.companyCommander.targetPlatoon;
                        int companyNum = company.companyNum + 1;
                        if (company.companyCommander.isRed)
                        {
                            company.companyCommander.companyText.text = companyNum + "★";
                        }
                        else
                        {
                            company.companyCommander.companyText.text = "★" + companyNum;
                        }
                    }
                }
                DespawnPlatoonInList(list, idx);
            }
            else
            {
                list[idx].CalcAtk();
                ++idx;
            }
        }
    }

    // 중대장 소대 재결정
    Platoon AppointmentCompanyLeaderInList(List<Platoon> list, Tile currentTile)
    {
        List<SearchPlatoon> frinedlyList = new List<SearchPlatoon>();

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].hp > 0)
            {
                // Tile targetTile = GetTileByIdx(list[i].platoonCommander.tileIdxX, list[i].platoonCommander.tileIdxY); // 적군 위치 타일

                float distance = Vector3.Distance(new Vector3(list[i].platoonCommander.tileIdxX, list[i].platoonCommander.tileIdxY, 0), new Vector3(currentTile.idxX, currentTile.idxY, 0));

                SearchPlatoon search = new SearchPlatoon();
                search.H = distance;
                search.platoon = list[i];
                frinedlyList.Add(search);
            }
        }

        if (frinedlyList.Count > 0)
        {
            List<SearchPlatoon> tempList = frinedlyList.OrderBy(k => k.H).ToList(); // 타일의 H 거리값 정렬
            return tempList[0].platoon; // 가장 가까운 소대를 중대장 소대로
        }

        return null;
    }

    // 해당하는 리스트의 소대가 전멸했으므로 전멸 처리
    void DespawnPlatoonInList(List<Platoon> list, int idx)
    {
        Platoon platoon = list[idx];
        list.RemoveAt(idx);
        platoon.DespawnPlatoon();
        PoolsManager.Despawn(platoon.gameObject); // object 숨김처리
    }

    // 소대 스폰
    List<Platoon> PlatoonSpawn(PlatoonMos mos, int armyNum, int companyNum, int formantionNum, Tile startTile, bool isRed)
    {
        GameObject company = new GameObject();
        if (isRed) company.name = "Red Company (" + companyNum + ")";
        else company.name = "Blue Company (" + companyNum + ")";

        List<Platoon> list = new List<Platoon>();
        Platoon commander = null;

        int offsetY = 0;
        int offsetX = 0;

        PlatoonSize changeSize = PlatoonSize.THOUSAND; // 변경되어야할 사이즈
        PlatoonSize originalSize = PlatoonSize.THOUSAND; // 유지되어야할 사이즈

        int changeSizeCount = 0; // 변경되어야할 사이즈 소대 수
        int originalSizeCount = 5; // 유지되어야할 사이즈 소대 수

        int changeArmyNum = 0; // 바뀐 사이즈의 소대 병사 수
        int originalArmyNum = 0; // 변경되지 않은 사이즈의 소대 병사 수
        int commanderOffestArmy = 0; // 중대장 소대 추가 병사 수
        int offestArmy = 0; // 일반 소대 추가 병사 수
        int minimumArmy = 0; // 5천 이하 소대일 때 천명 이하의 병사를 갖는 소대 병사 수

        int armyValue = armyNum;
        if (armyNum < 5000)
        {
            originalSizeCount = armyNum / 1000;

            originalArmyNum = 1000;

            if (armyNum % 1000 != 0)
            {
                originalSizeCount++;
                minimumArmy = armyNum % 1000;
            }
        }
        else if (armyNum < 9000)
        {
            originalSizeCount = 5;

            originalArmyNum = 1000;

            armyValue -= (originalSizeCount - changeSizeCount) * originalArmyNum;
            offestArmy = armyValue / 5;
            commanderOffestArmy = offestArmy + (armyValue % 5);
        }
        else if (armyNum < 25000)
        {
            changeSizeCount = armyNum / 5000;

            changeSize = PlatoonSize.FIVE_THOUSAND;

            changeArmyNum = 5000;
            originalArmyNum = 1000;

            armyValue -= changeSizeCount * changeArmyNum;
            armyValue -= (originalSizeCount - changeSizeCount) * originalArmyNum;
            offestArmy = armyValue / 5;
            commanderOffestArmy = offestArmy + (armyValue % 5);
        }
        else if (armyNum < 30000)
        {
            originalSizeCount = 5;
            originalSize = PlatoonSize.FIVE_THOUSAND;

            originalArmyNum = 5000;

            armyValue -= changeSizeCount * changeArmyNum;
            armyValue -= (originalSizeCount - changeSizeCount) * originalArmyNum;
            offestArmy = armyValue / 5;
            commanderOffestArmy = offestArmy + (armyValue % 5);
        }
        else if (armyNum < 35000)
        {
            changeSizeCount = 1;

            changeSize = PlatoonSize.TEN_THOUSAND;
            originalSize = PlatoonSize.FIVE_THOUSAND;

            changeArmyNum = 10000;
            originalArmyNum = 5000;

            armyValue -= changeSizeCount * changeArmyNum;
            armyValue -= (originalSizeCount - changeSizeCount) * originalArmyNum;
            offestArmy = armyValue / 5;
            commanderOffestArmy = offestArmy + (armyValue % 5);
        }
        else if (armyNum < 40000)
        {
            changeSizeCount = 2;

            changeSize = PlatoonSize.TEN_THOUSAND;
            originalSize = PlatoonSize.FIVE_THOUSAND;

            changeArmyNum = 10000;
            originalArmyNum = 5000;

            armyValue -= changeSizeCount * changeArmyNum;
            armyValue -= (originalSizeCount - changeSizeCount) * originalArmyNum;
            offestArmy = armyValue / 5;
            commanderOffestArmy = offestArmy + (armyValue % 5);
        }
        else if (armyNum < 45000)
        {
            changeSizeCount = 3;

            changeSize = PlatoonSize.TEN_THOUSAND;
            originalSize = PlatoonSize.FIVE_THOUSAND;

            changeArmyNum = 10000;
            originalArmyNum = 5000;

            armyValue -= changeSizeCount * changeArmyNum;
            armyValue -= (originalSizeCount - changeSizeCount) * originalArmyNum;
            offestArmy = armyValue / 5;
            commanderOffestArmy = offestArmy + (armyValue % 5);
        }
        else if (armyNum < 50000)
        {
            changeSizeCount = 4;

            changeSize = PlatoonSize.TEN_THOUSAND;
            originalSize = PlatoonSize.FIVE_THOUSAND;

            changeArmyNum = 10000;
            originalArmyNum = 5000;

            armyValue -= changeSizeCount * changeArmyNum;
            armyValue -= (originalSizeCount - changeSizeCount) * originalArmyNum;
            offestArmy = armyValue / 5;
            commanderOffestArmy = offestArmy + (armyValue % 5);
        }
        else
        {
            originalSizeCount = 5;
            originalSize = PlatoonSize.TEN_THOUSAND;

            originalArmyNum = 10000;
        }

        originalSizeCount -= changeSizeCount;

        if (changeSizeCount > 0) // 사이즈 변경 소대가 존재한다면?
        {
            int startY = startTile.idxY;

            for (int i = 0; i < changeSizeCount + originalSizeCount; i++)
            {
                bool isChangeSize = false;
                bool isCompanyLeader = false;

                if (i == 1)
                {
                    if (changeSizeCount > 2)
                    {
                        // 사이즈 변경 소대
                        isChangeSize = true;
                    }
                }
                else if (i == 2)
                {
                    // 사이즈 변경 소대 그리고 중대장
                    isChangeSize = true;
                    isCompanyLeader = true;
                }
                else if (i == 3)
                {
                    if (changeSizeCount > 1)
                    {
                        // 사이즈 변경 소대
                        isChangeSize = true;
                    }
                }
                else if (i == 3)
                {
                    if (changeSizeCount > 3)
                    {
                        // 사이즈 변경 소대
                        isChangeSize = true;
                    }
                }

                PlatoonSize size = originalSize;
                int PlatoonArmyNum = originalArmyNum + offestArmy;

                if (isChangeSize)
                {
                    size = changeSize;
                    PlatoonArmyNum = changeArmyNum + offestArmy;
                }

                if (isCompanyLeader)
                {
                    PlatoonArmyNum = changeArmyNum + commanderOffestArmy;
                }

                GameObject obj = PoolsManager.Spawn("Platoon", Vector3.zero, Quaternion.identity);
                Platoon platoon = obj.GetComponent<Platoon>();
                platoon.parentObj = company;

                // 천명과 오천명의 병력 규모는 Y인덱스를 1칸 띄어줘야 와꾸가 삽니다.
                if (size == PlatoonSize.THOUSAND)
                {
                    offsetY = 1;
                }
                else if (size == PlatoonSize.FIVE_THOUSAND)
                {
                    if (isRed) offsetY = 0;
                    else offsetY = 1;
                }
                else offsetY = 0;

                if (isRed) // 레드팀이라면 천명과 오천명의 병력 규모 X인덱스를 전방 배치
                {
                    if (size == PlatoonSize.THOUSAND)
                    {
                        offsetX = 2;
                    }
                    else if (size == PlatoonSize.FIVE_THOUSAND)
                    {
                        offsetX = 1;
                    }
                    else offsetX = 0;
                }

                platoon.PlatoonInit(this, mos, size, PlatoonArmyNum, tileArray[startTile.idxX + offsetX, startTile.idxY + (i * 5) + offsetY], i, companyNum, isRed);

                if (isCompanyLeader)
                {
                    commander = platoon;
                    if (isRed) obj.name = "Red Company Leader : " + companyNum;
                    else obj.name = "Blue Company Leader : " + companyNum;
                }

                list.Add(platoon);
                obj.transform.SetParent(company.transform);
            }
        }
        else // 사이즈가 변환되는 소대 없이 모든 소대의 사이즈가 동일한 경우
        {
            int idx = 0;

            bool isLeftSort = true;

            if (0 <= formantionNum && formantionNum <= 3)
            {
                isLeftSort = false;
            }


            if (isLeftSort)
            {
                if (originalSizeCount < 3)
                {
                    idx = 2;
                }
                else if (originalSizeCount < 5)
                {
                    idx = 1;
                }
            }
            else
            {
                if (originalSizeCount == 1)
                {
                    idx = 2;
                }
                else if (originalSizeCount < 4)
                {
                    idx = 1;
                }
            }

            if (!isRed && (8 == formantionNum || formantionNum == 9))
            {
                isLeftSort = false;

                if (originalSizeCount == 1)
                {
                    idx = 3;
                }
                else if (originalSizeCount == 3)
                {
                    idx = 2;
                }
                else if (originalSizeCount == 5)
                {
                    idx = 0;
                }
            }

            for (int i = 0; i < originalSizeCount; i++)
            {
                GameObject obj = PoolsManager.Spawn("Platoon", Vector3.zero, Quaternion.identity);
                Platoon platoon = obj.GetComponent<Platoon>();
                platoon.parentObj = company;

                int PlatoonArmyNum = originalArmyNum + offestArmy;

                if (i == originalSizeCount - 1 && minimumArmy != 0) PlatoonArmyNum = minimumArmy; // 1000명 이하인 소대가 발생했다면?

                // 천명과 오천명의 병력 규모는 Y인덱스를 1칸 띄어줘야 와꾸가 삽니다.
                if (originalSize == PlatoonSize.THOUSAND)
                {
                    offsetY = 1; 
                }
                else if (originalSize == PlatoonSize.FIVE_THOUSAND)
                {
                    if (isRed) offsetY = 0;
                    else offsetY = 1;
                }
                else offsetY = 0;

                if (isRed) // 레드팀이라면 천명과 오천명의 병력 규모 X인덱스를 전방 배치
                {
                    if (originalSize == PlatoonSize.THOUSAND)
                    {
                        offsetX = 2;
                    }
                    else if (originalSize == PlatoonSize.FIVE_THOUSAND)
                    {
                        offsetX = 1;
                    }
                    else offsetX = 0;
                }

                platoon.PlatoonInit(this, mos, originalSize, PlatoonArmyNum, tileArray[startTile.idxX + offsetX, startTile.idxY + (idx * 5) + offsetY], i, companyNum, isRed);
                idx++;

                // 중대 지휘관 소대 결정
                bool isCompanyLeader = false;

                if (originalSizeCount == 1)
                {
                    isCompanyLeader = true;
                }
                else if (originalSizeCount == 2)
                {
                    if (isLeftSort)
                    {
                        if (i == 1) isCompanyLeader = true;
                    }
                    else
                    {
                        if (i == 0) isCompanyLeader = true;
                    }
                }
                else if (originalSizeCount == 3)
                {
                    if (i == 1) 
                    {
                        isCompanyLeader = true;
                    }
                }
                else if (originalSizeCount == 4)
                {
                    if (isLeftSort)
                    {
                        if (i == 2) isCompanyLeader = true;
                    }
                    else
                    {
                        if (i == 1) isCompanyLeader = true;
                    }
                }
                else
                {
                    if (i == 2)
                    {
                        isCompanyLeader = true;
                    }
                }

                if (isCompanyLeader)
                {
                    commander = platoon;
                    if (isRed) obj.name = "Red Company Leader : " + companyNum;
                    else obj.name = "Blue Company Leader : " + companyNum;
                }

                list.Add(platoon);
                obj.transform.SetParent(company.transform);
            }
        }

        CompanyCommanderSetting(list, commander);

        return list;
    }

    IEnumerator WaitTick(Action callback)
    {
        waitCallback = callback;

        yield return wait;

        if (waitCallback != null)
        {
            waitCallback();
        }
    }

    // 중대 타겟을 가져온다
    public Platoon GetCompanyTarget(int companyNum, bool isRed)
    {
        List<Company> companyList;

        if (isRed) companyList = redCompanyList;
        else companyList = blueCompanyList;

        return companyList[companyNum].targetPlatoon;
    }

    // 중대 번호에 해당하는 중대 리턴
    public Company GetCompany(int companyNum, bool isRed = true)
    {
        List<Company> companyList;

        if (isRed) companyList = redCompanyList;
        else companyList = blueCompanyList;

        return companyList[companyNum];
    }

    // 소대 리스트의 소대들에게 중대 지휘관을 세팅
    void CompanyCommanderSetting(List<Platoon> list, Platoon companyCommander)
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i].companyCommander = companyCommander;
            list[i].SetPlatoonOffset();
        }
    }

    // 중대 타겟을 세팅 (주로 소대에서 호출)
    public void SetCompanyTarget(int companyNum, Platoon target, bool isRed = true)
    {
        List<Company> companyList;

        if (isRed) companyList = redCompanyList;
        else companyList = blueCompanyList;

        companyList[companyNum].targetPlatoon = target;
    }

    // 중대 번호 표기 텍스트 세팅
    public void CompanyTxtSetting(Company company)
    {
        List<Platoon> list = company.platoonList;

        for (int i = 0; i < list.Count; i++)
        {
            Text companyTxt = SpawnCompanyTxt();
            companyTxt.color = Color.red;
            //string sideTxt = "R";

            if (!list[i].isRed) companyTxt.color = Color.blue;

            int companyNum = company.companyNum + 1;

            if (company.companyCommander == list[i])
            {
                if (company.companyCommander.isRed)
                {
                    companyTxt.text = companyNum + "★";
                }
                else
                {
                    companyTxt.text = "★" + companyNum;
                }
            }
            else
            {
                companyTxt.text = companyNum.ToString();
            }

            list[i].companyText = companyTxt;
        }
    }

    public Text SpawnCompanyTxt()
    {
        GameObject obj = PoolsManager.Spawn("CompanyText", Vector3.zero, Quaternion.identity);
        obj.transform.SetParent(uiCanvas.transform);
        Text companyTxt = obj.GetComponent<Text>();
        companyTxt.transform.localScale = new Vector3(1, 1, 1);
        companyTxt.color = Color.black;

        return companyTxt;
    }

    public void CompanySpawn(PlatoonMos mos, int size, int companyNum, int formationtNum, bool isRed = true)
    {
        List<Company> companyList = null;

        if (isRed)
        {
            companyList = redCompanyList;
        }
        else
        {
            companyList = blueCompanyList;
        }

        Company company = new Company();
        company.targetPlatoon = null;
        company.companyNum = companyNum;
        company.newPathFind = true;
        company.needPathFind = false;
        company.platoonList = PlatoonSpawn(mos, size, company.companyNum, formationtNum, SetCompanyStartTile(formationtNum, isRed), isRed);

        switch (mos)
        {
            case PlatoonMos.ARCHER:
            case PlatoonMos.FOOTMAN:
                company.speed = PlatoonMoveSpeed.TICK_10;
                break;
            case PlatoonMos.KNIGHT:
                company.speed = PlatoonMoveSpeed.TICK_15;
                break;
            case PlatoonMos.SPEARMAN:
                company.speed = PlatoonMoveSpeed.TICK_5;
                break;
        }
        if (company.platoonList.Count > 0) company.companyCommander = company.platoonList[0].companyCommander;
        company.pathList = new List<PathStruct>();
        companyList.Add(company);
    }

    public void RestartGame()
    {
        isGameStart = false;

        tick = 0;

        restartButton.interactable = false;

        waitCallback = null;
        searchCallback = null;

        for (int i = 0; i < redCompanyList.Count; i++)
        {
            List<Platoon> PlatoonList = redCompanyList[i].platoonList;
            
            for (int j = 0; j < PlatoonList.Count; j++)
            {
                PlatoonList[j].DespawnPlatoon();
                Destroy(PlatoonList[j].gameObject);
            }
            PlatoonList.RemoveRange(0, PlatoonList.Count);
            Destroy(redCompanyList[i]);
        }

        for (int i = 0; i < blueCompanyList.Count; i++)
        {
            List<Platoon> PlatoonList = blueCompanyList[i].platoonList;
            
            for (int j = 0; j < PlatoonList.Count; j++)
            {
                PlatoonList[j].DespawnPlatoon();
                Destroy(PlatoonList[j].gameObject);
            }
            PlatoonList.RemoveRange(0, PlatoonList.Count);
            Destroy(blueCompanyList[i]);
        }

        for (int i = 0; i < 5; i++)
        {
            string objName = "Red Company (" + i + ")";
            GameObject obj = GameObject.Find(objName);
            if (obj != null) Destroy(obj);
            objName = "Blue Company (" + i + ")";
            obj = GameObject.Find(objName);
            if (obj != null) Destroy(obj);
        }

        for (int i = 0; i < tileCountX; i++)
        {
            for (int j = 0; j < tileCountY; j++)
            {
                tileArray[i, j].SetObject(null);
            }
        }

        turnCount = 0;
        turnText.text = "Turn : " + turnCount;

        BattlUi ui = uiCanvas.GetComponent<BattlUi>();

        ui.BattleUiInit();
    }

    public Dictionary<int, TroopSelect> DecisionFormation(bool isRed = true)
    {
        List<TroopSelect> troopList = new List<TroopSelect>();

        Dictionary<int, TroopSelect> formationDic = new Dictionary<int, TroopSelect>();

        for (int i = 0; i < troopSelectDic.Count; i++)
        {
            if (troopSelectDic[i].isEmpty) continue;

            if (isRed && i < 5)
            {
                troopList.Add(troopSelectDic[i]);
            }
            else if (!isRed && i >= 5)
            {
                troopList.Add(troopSelectDic[i]);
            }
        }

        PlatoonMos backSideMos = PlatoonMos.KNIGHT;
        PlatoonMos backCenterMos = PlatoonMos.ARCHER;
        PlatoonMos frontSideMos = PlatoonMos.SPEARMAN;
        PlatoonMos frontCenterMos = PlatoonMos.FOOTMAN;

        if (!isRed) // 블루팀이라면 병종 배치 우선 순위가 반대입니다.
        {
            backSideMos = frontSideMos;
            frontSideMos = PlatoonMos.KNIGHT;
            backCenterMos = frontCenterMos;
            frontCenterMos = PlatoonMos.ARCHER;
        }

        if (troopList.Count <= 0) return formationDic;

        // 포메이션에 1자리씩 넣어봅시다.
        int idx = 0;
        while (idx < troopList.Count)
        {
            if (troopList[idx].mos == backSideMos)
            {
                int[] array = { 0, 6 };
                if (!SetFormDicAndRemoveTroopListByArray(formationDic, troopList, array, idx)) idx++;
            }
            else if (troopList[idx].mos == frontSideMos)
            {
                int[] array = { 1, 7 };
                if (!SetFormDicAndRemoveTroopListByArray(formationDic, troopList, array, idx)) idx++;
            }
            else if (troopList[idx].mos == backCenterMos)
            {
                int[] array = { 2, 4 };
                if (!SetFormDicAndRemoveTroopListByArray(formationDic, troopList, array, idx)) idx++;
            }
            else if (troopList[idx].mos == frontCenterMos)
            {
                int[] array = { 3, 5 };
                if (!SetFormDicAndRemoveTroopListByArray(formationDic, troopList, array, idx)) idx++;
            }
        }

        // 1자리씩 전부 차지했다면 그대로 리턴
        if (troopList.Count <= 0) return formationDic;

        // 포메이션에서 자리를 차지 하지 못한 소대는 선호 배치순의 빈자리로 메꿉니다.
        idx = 0;
        while (idx < troopList.Count)
        {
            if (troopList[idx].mos == backSideMos)
            {
                int[] array = { 2, 4, 3, 5 };
                if (!SetFormDicAndRemoveTroopListByArray(formationDic, troopList, array, idx)) idx++;
            }
            else if (troopList[idx].mos == frontSideMos)
            {
                int[] array = { 3, 5, 2, 4 };
                if (!SetFormDicAndRemoveTroopListByArray(formationDic, troopList, array, idx)) idx++;
            }
            else if (troopList[idx].mos == frontCenterMos)
            {
                int[] array = { 1, 7, 2, 4 };
                if (!SetFormDicAndRemoveTroopListByArray(formationDic, troopList, array, idx)) idx++;
            }
            else if (troopList[idx].mos == backCenterMos)
            {
                int[] array = { 0, 6, 3, 5 };
                if (!SetFormDicAndRemoveTroopListByArray(formationDic, troopList, array, idx)) idx++;
            }
        }

        // 1자리씩 전부 차지했다면 그대로 리턴
        if (troopList.Count <= 0)
        {
            return formationDic;
        }
        else // 자리 배치를 받지 못한 소대가 생겼으므로 에러입니다.
        {
            Debug.Log("Company Formation ERROR!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            return formationDic;
        } 
    }

    // 중앙열에 1개의 부대만 존재한다면 배치 위치를 가운데로 정렬합니다.
    public Dictionary<int, TroopSelect> SortCenterLineCompany(Dictionary<int, TroopSelect> troopDic, bool isRed = true)
    {
        // 중앙 열에 위치한 부대들은 1부대만 존재하는지 확인해서 중앙 정렬을 해줍니다.
        for (int i = 2; i < 4; i++)
        {
            int key = i + 2;
            int newKey = i + 6;
            if (troopDic.ContainsKey(i) && !troopDic.ContainsKey(key))
            {
                TroopSelect troop = troopDic[i];
                troopDic.Add(newKey, troop);
                troopDic.Remove(i);
            }
            else if (troopDic.ContainsKey(key) && !troopDic.ContainsKey(i))
            {
                TroopSelect troop = troopDic[key];
                troopDic.Add(newKey, troop);
                troopDic.Remove(key);
            }
        }

        // 블루팀이라면 배열 위치를 반대로 해줍시다
        if (!isRed)
        {
            for (int i = 0; i < 4; i++)
            {
                if (troopDic.ContainsKey(i))
                {
                    int key = i + 6;

                    if (i == 2 || i == 3) key = i + 2;

                    if (troopDic.ContainsKey(key))
                    {
                        TroopSelect troop = troopDic[key];
                        troopDic[key] = troopDic[i];
                        troopDic[i] = troop;
                    }
                    else
                    {
                        TroopSelect troop = troopDic[i];
                        troopDic.Add(key, troop);
                        troopDic.Remove(i);
                    }
                }
            }
        }

        return troopDic;
    }

    // 배치 번호에 따른 초기 타일 값을 받아옵니다.
    public Tile SetCompanyStartTile(int num, bool isRed)
    {
        Tile tile = tileArray[0, 0];
        int tileX = 0;
        int tileY = 0;
        int blueTileX = 0;
        if (!isRed) blueTileX = 179;

        switch (num)
        {
            case 0:
                tileX = 76;
                tileY = 91;
                break;
            case 1:
                tileX = 91;
                tileY = 91;
                break;
            case 2:
                tileX = 76;
                tileY = 126;
                break;
            case 3:
                tileX = 91;
                tileY = 126;
                break;
            case 4:
                tileX = 76;
                tileY = 156;
                break;
            case 5:
                tileX = 91;
                tileY = 156;
                break;
            case 6:
                tileX = 76;
                tileY = 191;
                break;
            case 7:
                tileX = 91;
                tileY = 191;
                break;
            case 8:
                tileX = 76;
                tileY = 141;
                break;
            case 9:
                tileX = 91;
                tileY = 141;
                break;
        }

        tile = tileArray[tileX + blueTileX, tileY];

        return tile;
    }

    public bool SetFormDicAndRemoveTroopListByArray(Dictionary<int, TroopSelect> dic, List<TroopSelect> list, int[] keyArray, int idx)
    {
        for (int i = 0; i < keyArray.Count(); i++)
        {
            if (dic.ContainsKey(keyArray[i])) continue;

            dic.Add(keyArray[i], list[idx]);
            list.RemoveAt(idx);
            return true;
        }

        return false;
    }

    public void ChangeReduceDmgValue(bool value)
    {
        isDamgeReduce = value;
    }

    // 패스파인딩 루트 리스트를 세팅
    public void SetCompanyPath(int companyNum, bool isRed, List<PathStruct> pathList)
    {
        List<Company> companyList;

        if (isRed) companyList = redCompanyList;
        else companyList = blueCompanyList;

        companyList[companyNum].pathList = pathList;
        companyList[companyNum].newPathFind = false;

        SetMoveProcess(companyList[companyNum]);
    }

    public void SetCompanyPathClear(int companyNum, bool isRed)
    {
        List<Company> companyList;

        if (isRed) companyList = redCompanyList;
        else companyList = blueCompanyList;

        companyList[companyNum].pathList.Clear();
    }

    // 패스파인딩 루트 리스트
    public List<PathStruct> GetCompanyPath(int companyNum, bool isRed)
    {
        List<Company> companyList;

        if (isRed) companyList = redCompanyList;
        else companyList = blueCompanyList;

        return companyList[companyNum].pathList;
    }

    public bool GetPossibleCompanyNewPath(int companyNum, bool isRed)
    {
        List<Company> companyList;

        if (isRed) companyList = redCompanyList;
        else companyList = blueCompanyList;

        return companyList[companyNum].newPathFind;
    }

    void SetCompanyPathInList(List<Platoon> list, PlatoonMoveSpeed speed, Action action)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].platoonMoveSpeed != speed) continue;

            if (list[i].NeedsToPathFind()) // 패스파인드를 할 필요가 있는지 체크
            {
                list[i].companyCommander.PathCheck();
                return;
            }
        }

        action();
    }

    // 패스파인딩 목표점 타일 인덱스를 세팅
    public void SetCompanyDest(int companyNum, bool isRed, int x, int y)
    {
        List<Company> companyList;

        if (isRed) companyList = redCompanyList;
        else companyList = blueCompanyList;

        companyList[companyNum].companyDestX = x;
        companyList[companyNum].companyDestY = y;
    }

    public void CalcMoveProcess(Company company)
    {
        company.platoonList = company.platoonList.OrderBy(i => i.platoonPath.Count).ToList();
    }

    // 완성된 패스파인딩 루트의 가까운 순서에 따라 이동을 결정합니다.
    public void SetMoveProcess(Company company)
    {
        /*
        Dictionary<int, Platoon> moveProcessDic = new Dictionary<int, Platoon>();
        List<Platoon> newMoveList = new List<Platoon>();
        List<PathStruct> dirList = new List<PathStruct>();
        */

        for (int i = 0; i < company.platoonList.Count; i++)
        {
            if (company.platoonList[i].companyCommander == company.platoonList[i]) continue;

            company.platoonList[i].SetPlatoonPath(company.pathList);
        }

        int count = company.platoonList.Count();
        Debug.Log("test");

        CalcMoveProcess(company);

        Debug.Log("test");
        /*
        // 소대가 패스파인딩 루트에 위치하는지?
        for (int i = 0; i < company.platoonList.Count; i++)
        {
            company.platoonList[i].SetPlatoonPath(company.pathList);

            Tile currentTile = GetTileByIdx(company.platoonList[i].platoonCommander.tileIdxX, company.platoonList[i].platoonCommander.tileIdxY);

            for (int j = 0; j < company.pathList.Count; j++)
            {
                if (company.pathList[j].tile == currentTile)
                {
                    moveProcessDic.Add(j, company.platoonList[i]);
                }
            }
        }

        var sortDic = moveProcessDic.OrderByDescending(i => i.Key); // 키밸류가 높을 수록 패스파인딩 목표점에 가까움

        // 패스파인딩 루트에 위치하지 않지만 목표점에 가까울수록 먼저 이동합니다.
        if (moveProcessDic.Count != company.platoonList.Count)
        {
            for (int i = 0; i < company.platoonList.Count; i++)
            {
                if (moveProcessDic.ContainsValue(company.platoonList[i])) continue;

                float distance = Vector3.Distance(new Vector3(company.companyDestX, company.companyDestY, 0), new Vector3(company.platoonList[i].platoonCommander.tileIdxX, company.platoonList[i].platoonCommander.tileIdxY, 0));

                PathStruct dir = new PathStruct();
                dir.H = distance;
                dir.tile = null;
                dir.platoon = company.platoonList[i];
                dirList.Add(dir);
            }

            dirList = dirList.OrderBy(i => i.H).ToList(); // 타일의 H 거리값 정렬
        }

        foreach (KeyValuePair<int, Platoon> item in sortDic)
        {
            newMoveList.Add(item.Value);
        }

        for (int i = 0; i < dirList.Count; i++)
        {
            newMoveList.Add(dirList[i].platoon);
        }

        if (company.platoonList.Count != newMoveList.Count)
        {
            Debug.Log("Move Process Error!!!!!!");
        }
        else
        {
            company.platoonList = newMoveList;
        }
        */
    }
    
        // 테스트 장애물
    void TestBlock(int X, int Y, string str = "Black")
    {
        GameObject obj = PoolsManager.Spawn(str, Vector3.zero, Quaternion.identity);
        obj.transform.SetParent(transform);
        Tile tile = tileArray[X, Y];
        obj.transform.position = new Vector3(tile.posX, tile.posY, 0);
    }

    // Tick 카운트를 올리고 부대 색적, 이동, 공격을 진행합니다.
    void Update()
    {
        if (isGameStart)
        {
            tickTimer += Time.deltaTime;

            if (tickTimer >= TICK_TIMER_MAX)
            {
                tickTimer -= TICK_TIMER_MAX;
                tick++;

                OnSearchEnemyRedTem(); // 적 타겟은 매 틱 찾습니다.

                if (tick % 4 == 0) OnCheckRedTeamNeedPathFind(PlatoonMoveSpeed.TICK_15); // 1초에 15틱 이동
                if (tick % 6 == 0) OnCheckRedTeamNeedPathFind(PlatoonMoveSpeed.TICK_10); // 1초에 10틱 이동
                if (tick % 12 == 0) OnCheckRedTeamNeedPathFind(PlatoonMoveSpeed.TICK_5); // 1초에 5틱 이동
                if (tick % 60 == 0) OnAttackRedTeam(); // 1초마다 공격합니다.
            }
        }
    }
}

/*
// 랜덤 소대 생성 루틴 테스트를 위해 남겨둡니다.
public PlatoonMos RandomPlatoonMos()
{
    int mos = UnityEngine.Random.Range(0, 4);

    return (PlatoonMos)mos;
}

public PlatoonSize RandomPlatoonSize()
{
    int size = UnityEngine.Random.Range(0, 3);
    PlatoonSize tempSize = PlatoonSize.ONE;
    switch (size)
    {
        case 0:
            tempSize = PlatoonSize.THOUSAND;
            break;
        case 1:
            tempSize = PlatoonSize.FIVE_THOUSAND;
            break;
        case 2:
            tempSize = PlatoonSize.TEN_THOUSAND;
            break;
    }

    return tempSize;
}
*/

/*
// 중대장 소대가 전멸했는지 체크해서 전멸했다면 새로 중대장 임명 (지금은 사용하지 않습니다)
void OnCheckCompanyLeaderDefeat()
{
    for (int i = 0; i < redCompanyList.Count; i++)
    {
        if (redCompanyList[i].companyCommander == null)
        {
            redCompanyList[i].companyCommander = AppointmentCompanyLeaderInList(redCompanyList[i].platoonList);

            if (redCompanyList[i].companyCommander != null)
            {
                redCompanyList[i].targetPlatoon = redCompanyList[i].companyCommander.targetPlatoon;
                redCompanyList[i].companyCommander.companyText.text = redCompanyList[i].companyNum + "★";
            }
         }
    }

    for (int i = 0; i < blueCompanyList.Count; i++)
    {
        if (blueCompanyList[i].companyCommander == null)
        {
            blueCompanyList[i].companyCommander = AppointmentCompanyLeaderInList(blueCompanyList[i].platoonList);
            if (blueCompanyList[i].companyCommander != null)
            {
                blueCompanyList[i].targetPlatoon = blueCompanyList[i].companyCommander.targetPlatoon;
                blueCompanyList[i].companyCommander.companyText.text = "★" + blueCompanyList[i].companyNum;
            }
        }
    }

    OnTurnEnd();
}
*/

// StartTurn 턴 시작

// MagicPhase 유저 인풋으로 스킬을 사용하는 페이즈
// 레드팀 마법 사용
// 블루팀 마법 사용

// UnitMoveTurn 유닛 이동 관련
// 레드팀 이동
// 블루팀 이동

// UnitAttackTurn 유닛 공격 관련
// 레드팀 공격
// 블루팀 공격

// TurnEnd 턴 종료

// GameEnd 게임 종료