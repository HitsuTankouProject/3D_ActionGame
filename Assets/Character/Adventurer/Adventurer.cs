using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Adventurer : Character
{
    private enum Action { Idle, Run, Attack, Defend, Skill, Ultimate, Hit, Death }

    private const int initHp = 1000;
    private const int initDef = 100;
    private float maxDefScale = 0.3f;
    private float minDefScale = 0.1f;

    private const int initAtk = 200;

    public override int maxHp => FinallyStatus(initHp, status.HpLv);
    
    public override int atkIndex => FinallyStatus(initAtk, status.AtkLv);

    public override int GotHitHpLost(int damage)
    {
        if (damage <= 0) return 0;
        int defValue = FinallyStatus(initDef, status.DefLv);
        if (damage <= defValue) return Mathf.RoundToInt(damage * (1.0f - maxDefScale));

        float damage_per_defValue = damage / (float)defValue;
        int totalDamage = 0;
        int maxCalculate = Mathf.CeilToInt(damage_per_defValue);

        for (int calculateTurn = 1; calculateTurn <= maxCalculate; calculateTurn++)
        {
            float index;
            float defScale = maxDefScale / calculateTurn;
            if (defScale < minDefScale)
            {
                defScale = minDefScale;
                index = damage_per_defValue;
            }
            else index = Mathf.Min(damage_per_defValue, 1.0f);

            int damageValue = Mathf.RoundToInt(defValue * index * (1 - defScale));
            totalDamage += damageValue;
            damage_per_defValue -= index;
            if (damage_per_defValue <= 0) break;
        }
        return totalDamage;
    }

    public override void GotHit(int damage)
    {
        base.GotHit(damage);
    }


}