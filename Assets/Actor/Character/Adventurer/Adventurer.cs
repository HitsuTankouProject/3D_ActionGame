using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

/// <summary>
/// Adventurer職業のキャラクターを管理するクラス。
/// 通常攻撃に加えて、左手の盾を使用した防御処理を行う。
/// </summary>
public class Adventurer : Character
{
    /// <summary>
    /// キャラクターの職業を取得する。
    /// </summary>
    public override CharacterType characterType => CharacterType.Adventurer;
    /// <summary>
    /// モンスターを追跡可能な最大距離。
    /// </summary>
    protected override float trackDistance => 5.0f;
    /// <summary>
    /// 防御状態を表すAnimatorパラメーター名。
    /// </summary>
    private const string isDefend = "isDefending";
    /// <summary>
    /// アクティブスキルの発動に必要な色チャージ量。
    /// </summary>
    public override uint activeSkillCost => 30;
    /// <summary>
    /// キャラクターを待機状態へ戻し、
    /// 防御状態と盾の防御処理を終了する。
    /// </summary>
    public override void ReturnIdle()
    {
        base.ReturnIdle();
        // 防御アニメーションを解除する。
        actorAnimator.SetBool(isDefend, false);
        // 盾による防御状態を終了する。
        leftHand.EndDefend();
    }

    [Header("Adventurer Special")]
    /// <summary>
    /// Adventurerが左手に装備する盾。
    /// </summary>
    public Shield leftHand;
    /// <summary>
    /// 左手の武器を配置するTransform。
    /// </summary>
    public Transform leftHandTransform;
    /// <summary>
    /// Adventurerが使用する通常攻撃アニメーションのTrigger Hash一覧。
    /// </summary>
    protected override int[] allNormalAttackHashes { get; } =
    {
        Animator.StringToHash("Attack_01"), Animator.StringToHash("Attack_02"),
    };
    /// <summary>
    /// プレイヤーデータに登録されている武器情報を使用して、
    /// メイン武器と左手の盾を初期化する。
    /// </summary>
    protected override void InitializeCharacterWeapon()
    {
        base.InitializeCharacterWeapon();
        // 盾に使用する2つ目の武器情報が存在するか確認する。
        if (_player.allWeaponStatus.Length < 2)
        {
            Debug.LogError("[Adventurer] Player`s AllWeaponStatus Length < 2");
            return;
        }
        // 2つ目の武器を左手の盾として初期化する。
        WeaponStatus supWeaponStatus = _player.allWeaponStatus[1];
        leftHand.WeaponInit(this, supWeaponStatus);

    }
    /// <summary>
    /// 盾を使用したパッシブスキルを実行する。
    /// すでに防御状態の場合は次の行動受付処理を開始し、
    /// 防御状態でない場合は盾による防御を開始する。
    /// </summary>
    public override void PassiveSkill()
    {
        // コマンドを実行できない状態ではパッシブスキルを使用しない。
        if (!IsAllowCommand()) return;

        // すでにパッシブスキル状態の場合は、
        // 次の行動を受け付けるための待機処理を開始する。
        if (stage == PlayerStage.PassiveSkill) 
        {
            WaitTheNextAction();
            return;
        }
        // Character側のパッシブスキル処理を実行する。
        base.PassiveSkill();
        // 防御アニメーションを有効にする。
        actorAnimator.SetBool(isDefend, true);
        // 盾の防御判定を開始する。
        leftHand.DoDefend();

    }


}