﻿using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct TroopSelect
{
    public PlatoonMos mos;
    public int platoonSize;
    public bool isEmpty;
}

public class BattlUi : MonoBehaviour
{
    public MainGame mainGame;

    public GameObject troopSelectPanelObj;

    public GameObject typeSelectPanelObj;

    public int selectButoon = 0;

    public int selectMos = 0;

    public List<Button> mosButtonList;

    public List<InputField> sizeInputList;

    public List<Slider> sizeSliderList;

    public Dictionary<int, TroopSelect> troopSelectDic;

    public List<TroopSelect> redTroopList;

    public List<TroopSelect> blueTroopList;

    public Text redTotalNum;
    public Text blueTotalNum;

    float panelPosY = 156;

    public void LoadPreset()
    {
        for (int i = 0; i < 10; i++)
        {
            PlatoonMos mos = PlatoonMos.FOOTMAN;
            int size = 50000;
            bool isEmpty = false;

            string ketStr = "mos_" + i;
            if (PlayerPrefs.HasKey(ketStr))
            {
                int mosNum = PlayerPrefs.GetInt(ketStr);

                Button button = mosButtonList[i];
                Text btnTxt = button.GetComponentInChildren<Text>();

                switch (mosNum)
                {
                    case 0:
                        btnTxt.text = "Infantry";
                        mos = PlatoonMos.FOOTMAN;
                        break;
                    case 1:
                        btnTxt.text = "Spear man";
                        mos = PlatoonMos.SPEARMAN;
                        break;
                    case 2:
                        btnTxt.text = "Bow man";
                        mos = PlatoonMos.ARCHER;
                        break;
                    case 3:
                        btnTxt.text = "Cavalry";
                        mos = PlatoonMos.KNIGHT;
                        break;
                }
            }
            else
            {
                if (i < 2)
                {
                    Button button = mosButtonList[i];
                    Text btnTxt = button.GetComponentInChildren<Text>();
                    btnTxt.text = "Infantry";
                    mos = PlatoonMos.FOOTMAN;
                    PlayerPrefs.SetInt(ketStr, 0);
                }
                else if (i < 5)
                {
                    Button button = mosButtonList[i];
                    Text btnTxt = button.GetComponentInChildren<Text>();
                    btnTxt.text = "Cavalry";
                    mos = PlatoonMos.KNIGHT;
                    PlayerPrefs.SetInt(ketStr, 3);
                }
                else if (i == 5)
                {
                    Button button = mosButtonList[i];
                    Text btnTxt = button.GetComponentInChildren<Text>();
                    btnTxt.text = "Infantry";
                    mos = PlatoonMos.FOOTMAN;
                    PlayerPrefs.SetInt(ketStr, 0);
                }
                else if (i == 6)
                {
                    Button button = mosButtonList[i];
                    Text btnTxt = button.GetComponentInChildren<Text>();
                    btnTxt.text = "Bow man";
                    mos = PlatoonMos.ARCHER;
                    PlayerPrefs.SetInt(ketStr, 2);
                }
                else if (i == 7)
                {
                    Button button = mosButtonList[i];
                    Text btnTxt = button.GetComponentInChildren<Text>();
                    btnTxt.text = "Cavalry";
                    mos = PlatoonMos.KNIGHT;
                    PlayerPrefs.SetInt(ketStr, 3);
                }
                else
                {
                    Button button = mosButtonList[i];
                    Text btnTxt = button.GetComponentInChildren<Text>();
                    btnTxt.text = "Spear man";
                    mos = PlatoonMos.SPEARMAN;
                    PlayerPrefs.SetInt(ketStr, 1);
                }
            }

            ketStr = "size_" + i;
            if (PlayerPrefs.HasKey(ketStr))
            {
                size = PlayerPrefs.GetInt(ketStr);
            }
            else
            {
                PlayerPrefs.SetInt(ketStr, 50000);
            }

            ketStr = "empty_" + i;
            if (PlayerPrefs.HasKey(ketStr))
            {
                isEmpty = bool.Parse(PlayerPrefs.GetString(ketStr));

                if (isEmpty)
                {
                    Button button = mosButtonList[i];
                    Text btnTxt = button.GetComponentInChildren<Text>();
                    btnTxt.text = "EMPTY";
                }
            }
            else
            {
                PlayerPrefs.SetString(ketStr, "false");
            }

            TroopSelect troop = new TroopSelect();
            troop.platoonSize = size;
            troop.mos = mos;
            troop.isEmpty = isEmpty;
            troopSelectDic.Add(i, troop);

            // Ui 세팅
            InputField textEdit = sizeInputList[i];
            textEdit.text = size.ToString();

            Slider slider = sizeSliderList[i];
            slider.value = size;
        }

        UpdateTotalNum();
    }

    public void Awake()
    {
        troopSelectDic = new Dictionary<int, TroopSelect>();

        LoadPreset();
    }

    public void BattleUiInit()
    {
        // 초기화할 필요 없는듯?
        /*
        troopSelectDic.Clear();

        for (int i = 0; i < 10; i++)
        {
            TroopSelect troop = new TroopSelect();
            troop.platoonSize = 1000;
            troop.mos = PlatoonMos.FOOTMAN;
            troop.isEmpty = true;
            troopSelectDic.Add(i, troop);
            InputField textEdit = sizeInputList[i];
            textEdit.text = "1000";
        }
        */

        troopSelectPanelObj.SetActive(true);
    }

    public void SetTroopSize(int num)
    {
        InputField textEdit = sizeInputList[num];

        string sizeTxt = textEdit.text;

        int sizeNum;
        if (Int32.TryParse(sizeTxt, out sizeNum))
        {
            if (sizeNum < 1000)
            {
                sizeNum = 1000;
                textEdit.text = sizeNum.ToString();
            }
            else if (sizeNum > 50000)
            {
                sizeNum = 50000;
                textEdit.text = sizeNum.ToString();
            }

            Slider slider = sizeSliderList[num];
            slider.value = sizeNum;

            if (troopSelectDic.ContainsKey(num))
            {
                troopSelectDic[num] = SetTroopData(troopSelectDic[num], troopSelectDic[num].mos, sizeNum, troopSelectDic[num].isEmpty);
            }
        }
        else
        {
            sizeNum = 1000;
            textEdit.text = sizeNum.ToString();
            Slider slider = sizeSliderList[num];
            slider.value = sizeNum;
            if (troopSelectDic.ContainsKey(num))
            {
                troopSelectDic[num] = SetTroopData(troopSelectDic[num], troopSelectDic[num].mos, sizeNum, troopSelectDic[num].isEmpty);
            }
        }

        UpdateTotalNum();
    }

    public void SetTroppSizeSlide(int num)
    {
        Slider slider = sizeSliderList[num];

        int size = Convert.ToInt32(slider.value);

        InputField textEdit = sizeInputList[num];

        textEdit.text = size.ToString();

        if (troopSelectDic.ContainsKey(num))
        {
            troopSelectDic[num] = SetTroopData(troopSelectDic[num], troopSelectDic[num].mos, size, troopSelectDic[num].isEmpty);
        }

        UpdateTotalNum();
    }

    public void SetTroopTypeButton(int num)
    {
        float posX = -310;
        float posY = 0;

        if (num < 5)
        {
            posY = panelPosY - (num * 105);
        }
        else
        {
            posX = 330;
            posY = panelPosY - ((num - 5) * 105);
        }

        RectTransform transform = typeSelectPanelObj.transform as RectTransform;

        transform.anchoredPosition = new Vector3(posX, posY, transform.transform.position.z);

        typeSelectPanelObj.SetActive(true);

        selectMos = 0;
             
        selectButoon = num;
    }

    public void SetTroopType(int num)
    {
        selectMos = num;

        SelectTypeComplete();
    }

    public PlatoonSize getPlatoonSize(int num)
    {
        PlatoonSize size = PlatoonSize.ONE;
        switch (num)
        {
            case 1:
                size = PlatoonSize.THOUSAND;
                break;
            case 2:
                size = PlatoonSize.FIVE_THOUSAND;
                break;
            case 3:
                size = PlatoonSize.TEN_THOUSAND;
                break;
        }
        return size;
    }

    public void SelectComplete()
    {
        troopSelectPanelObj.SetActive(false);

        for (int i = 0; i < 10; i++)
        {
            string ketStr = "mos_" + i;
            PlayerPrefs.SetInt(ketStr, (int)troopSelectDic[i].mos);
            ketStr = "size_" + i;
            PlayerPrefs.SetInt(ketStr.ToString(), troopSelectDic[i].platoonSize);
            ketStr = "empty_" + i;
            PlayerPrefs.SetString(ketStr.ToString(), troopSelectDic[i].isEmpty.ToString());
        }

        mainGame.GameStart(troopSelectDic);
    }

    public void SelectTypeComplete()
    {
        typeSelectPanelObj.SetActive(false);


        Button button = mosButtonList[selectButoon];

        Text btnTxt = button.GetComponentInChildren<Text>();

        PlatoonMos mos = PlatoonMos.FOOTMAN;

        bool empty = false;

        switch (selectMos)
        {
            case 0:
                btnTxt.text = "EMPTY";
                empty = true;
                break;
            case 1:
                btnTxt.text = "Infantry";
                mos = PlatoonMos.FOOTMAN;
                break;
            case 2:
                btnTxt.text = "Spear man";
                mos = PlatoonMos.SPEARMAN;
                break;
            case 3:
                btnTxt.text = "Bow man";
                mos = PlatoonMos.ARCHER;
                break;
            case 4:
                btnTxt.text = "Cavalry";
                mos = PlatoonMos.KNIGHT;
                break;
        }

        if (troopSelectDic.ContainsKey(selectButoon))
        {
            troopSelectDic[selectButoon] = SetTroopData(troopSelectDic[selectButoon], mos, troopSelectDic[selectButoon].platoonSize, empty);
        }

        UpdateTotalNum();
    }

    public TroopSelect SetTroopData(TroopSelect select, PlatoonMos mos, int size, bool empty)
    {
        TroopSelect temp = select;
        temp.mos = mos;
        temp.platoonSize = size;
        temp.isEmpty = empty;
        return temp;
    }

    public void UpdateTotalNum()
    {
        int redTotalSize = 0;
        int blueTotalSize = 0;

        for (int i = 0; i < 10; i++)
        {
            if (troopSelectDic.ContainsKey(i))
            {
                if (troopSelectDic[i].isEmpty) continue;

                if (i < 5) redTotalSize += troopSelectDic[i].platoonSize;
                else blueTotalSize += troopSelectDic[i].platoonSize;
            }
        }

        redTotalNum.text = "총 병사수 : " + redTotalSize;
        blueTotalNum.text = "총 병사수 : " + blueTotalSize;
    }
}
