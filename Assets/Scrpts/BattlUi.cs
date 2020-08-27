using JetBrains.Annotations;
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
    public GameObject troopTypeSelectPanelObj;

    public int selectButoon = 0;

    public int selectMos = 0;

    public List<Button> mosButtonList;

    public List<InputField> sizeInputList;

    public List<Slider> sizeSliderList;

    public Dictionary<int, TroopSelect> troopSelectDic;

    public List<TroopSelect> redTroopList;

    public List<TroopSelect> blueTroopList;

    public void Awake()
    {
        troopSelectDic = new Dictionary<int, TroopSelect>();

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
    }

    public void SetTroopTypeButton(int num)
    {
        troopTypeSelectPanelObj.SetActive(true);

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

        mainGame.GameStart(troopSelectDic);
    }

    public void SelectTypeComplete()
    {
        troopTypeSelectPanelObj.SetActive(false);

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
                btnTxt.text = "Cavalry";
                mos = PlatoonMos.KNIGHT;
                break;
            case 4:
                btnTxt.text = "Bow man";
                mos = PlatoonMos.ARCHER;
                break;
        }

        if (troopSelectDic.ContainsKey(selectButoon))
        {
            troopSelectDic[selectButoon] = SetTroopData(troopSelectDic[selectButoon], mos, troopSelectDic[selectButoon].platoonSize, empty);
        }
    }

    public TroopSelect SetTroopData(TroopSelect select, PlatoonMos mos, int size, bool empty)
    {
        TroopSelect temp = select;
        temp.mos = mos;
        temp.platoonSize = size;
        temp.isEmpty = empty;
        return temp;
    }
}
