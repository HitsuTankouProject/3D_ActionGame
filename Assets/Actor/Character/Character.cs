
using Cysharp.Threading.Tasks;
using Fusion;
using System;
using System.Collections.Generic;

using System.Threading;
using UnityEngine;


/// <summary>プレイヤーデータの種類を表します。</summary>
[System.Serializable] public enum PlayerDataType { Character, Bag }
/// <summary>キャラクターの職業を表します。</summary>
[System.Serializable] public enum CharacterType { Adventurer = 1, Magician, Thief, Warrior }

/// <summary>キャラクターが現在行っているアクションを表します。</summary>
public enum PlayerStage { Idle, Run, Attack, PassiveSkill, ActiveSkill, UltSkill, Hit, Death }

/// <summary>
/// プレイヤーキャラクターに共通する
/// 1. ステータス、
/// 2. アニメーション、
/// 3. 移動、
/// 4. 戦闘処理を管理します。
/// </summary>
public abstract class Character : Actor, IColorDamageable
{
    [Header("Character Basic")]
    public MeshRenderer[] othersBodyMeshParts;
    public NetworkMecanimAnimator networkAnimator;
    protected abstract CharacterType characterType { get; }

    private int requiredMaterialCountPerLevel => (othersBodyMeshParts?.Length ?? 0) + 1;

    [Header("Character Stage")]
    /// <summary>キャラクターの現在の行動状態を表します。</summary>
    public PlayerStage stage;
    protected override void StopTracking()
    {
        base.StopTracking();
       // _inGame.playerCameraFollow.ToggleAlwaysBehindCharacter();

    }

    protected override bool TryTrackActor()
    {
        if (_inGame.allMonsters == null || _inGame.allMonsters.Count == 0) return false;

        foreach (Monster monster in _inGame.allMonsters)
        {
            if (monster == null || !monster.canBeTrack) continue;

            if (Vector3.Distance(monster.transform.position, transform.position) > trackDistance) continue;

            trackingActor = monster;
            Turn(trackingMonsterPosition, true);
            //_inGame.playerCameraFollow.TurnCameraToCharacterBack();

            return true;

        }

        return false;

    }
    protected override void UpdateTracking(float distanceError = 1.0f)
    {
        if (trackingActor == null) return;
        if (!trackingActor.canBeTrack)
        {
            StopTracking();
            return;
        }

        float distance = Vector3.Distance(transform.position, trackingActor.transform.position);

        if (distance > (trackDistance * distanceError)) StopTracking();

    }

    // 現在実行中のアクションをキャンセルするために使用するToken。
    private CancellationTokenSource actionToken;
    /// <summary>次のコマンドを待機する時間を取得します。</summary>
    /// 連続入力のエラーを防ぐため
    private float _commandTime => GameManager.commandTime;


    /// <summary>
    /// 次のコマンドを実行できる状態が一定時間続いた場合、自動的に待機状態へ戻します。
    /// </summary>
    /// <param name="token">監視処理を中止するためのキャンセルトークン。</param>
    private async UniTask AutoTurnToIdle(CancellationToken token)
    {
        try
        {
            //　次のコマンドを実行できる状態になるまで待機する。
            //　canDoNextCommandは動画スタートのときfalseになって、
            //　動画の指定フレームでtrueにもどる
            while (!canDoNextCommand) await UniTask.Yield(cancellationToken: token);

            float timer = 0.0f;
            // コマンドを実行可能な間、経過時間を計測する。
            while (canDoNextCommand)
            {
                timer += Time.deltaTime;

                // 指定時間コマンドが入力されなかった場合、待機状態へ戻る。
                if (timer >= _commandTime)
                {
                    ReturnIdle();
                    return;
                }
                await UniTask.Yield(cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            // 新しい監視処理が開始された場合のキャンセルは正常終了として扱う。
        }
    }

    /// <summary>
    /// 現在の待機処理を中止し、次のコマンド受付後に自動で待機状態へ戻る監視を開始します。
    /// </summary>
    public void WaitTheNextAction()
    {
        // 以前のToken監視処理を停止して破棄する。
        actionToken?.Cancel();
        actionToken?.Dispose();

        // 新しい監視処理で使用するTokenを生成する。
        actionToken = new CancellationTokenSource();

        AutoTurnToIdle(actionToken.Token).Forget();
    }

    #region Animation
    // Animator に設定されているパラメーター名。
    private const string passiveSkillTrigger = "PassiveSkill";
    private const string ultSkillTrigger = "UltSkill";

    private static readonly int cancelTriggerHash = Animator.StringToHash(cancelTrigger);
    private static readonly int attack01TriggerHash = Animator.StringToHash(attack01Trigger);
    private static readonly int attack02TriggerHash = Animator.StringToHash(attack02Trigger);
    private static readonly int passiveSkillTriggerHash = Animator.StringToHash(passiveSkillTrigger);
    private static readonly int activeSkillTriggerHash = Animator.StringToHash(activeSkillTrigger);
    private static readonly int ultSkillTriggerHash = Animator.StringToHash(ultSkillTrigger);
    private static readonly int gotHitTriggerHash = Animator.StringToHash(gotHitTrigger);
    private static readonly int deathTriggerHash = Animator.StringToHash(deathTrigger);



    // 2種類の通常攻撃を交互に使用するためのフラグ。
    private bool useFirstAttack = true;

    /// <summary>
    /// 通过 NetworkMecanimAnimator 播放 Trigger。
    /// </summary>
    private void SetNetworkTrigger(int triggerHash)
    {
        if (!Object.HasStateAuthority) return;

        if (networkAnimator == null)
        {
            Debug.LogError("NetworkMecanimAnimator is not assigned.", this);
            return;
        }

        networkAnimator.SetTrigger(triggerHash);
    }


    /// <summary>
    /// 2種類の通常攻撃アニメーションを交互に再生します。
    /// 攻撃のパタン
    /// </summary>
    protected void Animation_Attack()
    {
        if (!Object.HasStateAuthority) return;

        int attackTriggerHash = useFirstAttack ? attack01TriggerHash : attack02TriggerHash;

        // 前回設定された攻撃トリガーを解除する。
        actorAnimator.ResetTrigger(attack01TriggerHash);
        actorAnimator.ResetTrigger(attack02TriggerHash);

        SetNetworkTrigger(attackTriggerHash);

        // 次回は別の攻撃アニメーションを使用する。
        useFirstAttack = !useFirstAttack;
    }



    /// <summary> 走行アニメーションを開始します。 </summary>
    protected void Animation_Run()
    {
        if (!Object.HasStateAuthority) return;

        if (!actorAnimator.GetBool(runBool)) actorAnimator.SetBool(runBool, true);
    }

    /// <summary>　操作できる状態なのかを判定します。</summary>
    /// <returns>　被弾中または死亡中ではない場合は <see langword="true"/>。</returns>
    private bool IsAllowCommand() => stage != PlayerStage.Hit && stage != PlayerStage.Death;

    /// <summary>
    /// 次のコマンドの受付を許可します。
    /// Animation Eventからの呼び出しを想定しています。
    /// </summary>
    public override void CanDoNextCommand()
    {
        if (!Object.HasStateAuthority) return;
        base.CanDoNextCommand();
    }
    /// <summary>　現在のアニメーションをキャンセルします。　</summary>
    public void CancelCommand()
    {
        if (!Object.HasStateAuthority) return;
        SetNetworkTrigger(cancelTriggerHash);
    }


    /// <summary>
    /// 指定したプレイヤー状態に対応するアニメーションを再生します。
    /// </summary>
    /// <param name="playerStage">再生するアニメーションに対応するプレイヤー状態。</param>
    private void PlayAnimation(PlayerStage playerStage)
    {
        switch (playerStage)
        {
            case PlayerStage.Run: Animation_Run(); break;
            case PlayerStage.Attack: Animation_Attack(); break;
            case PlayerStage.PassiveSkill: SetNetworkTrigger(passiveSkillTriggerHash); break;
            case PlayerStage.ActiveSkill: SetNetworkTrigger(activeSkillTriggerHash); break;
            case PlayerStage.UltSkill: SetNetworkTrigger(ultSkillTriggerHash); break;
            default: break;
        }
    }

    /// <summary>
    /// コマンドを実行可能な場合、指定された状態へ変更して
    /// 対応するアニメーションを再生します。
    /// </summary>
    /// <param name="playerStage">変更先のプレイヤー状態。</param>
    private void RequestChangeStage(PlayerStage playerStage)
    {
        // 被弾中、死亡中、または前のコマンドの実行中は変更しない。
        if (!Object.HasStateAuthority || !IsAllowCommand() || !canDoNextCommand) return;

        // 新しいコマンドが完了するまで、次のコマンドを禁止する。
        canDoNextCommand = false;
        // 異なる状態へ移行する場合、以前のアニメーション設定を解除する。
        if (playerStage != stage) ReturnIdle();
        stage = playerStage;

        PlayAnimation(playerStage);
        WaitTheNextAction();
    }
    /// <summary>
    /// キャラクターを待機状態へ変更し、
    /// 現在設定されているアニメーションパラメーターを解除します。
    /// </summary>
    public virtual void ReturnIdle()
    {
        if (!Object.HasStateAuthority) return;

        stage = PlayerStage.Idle;

        // 走行アニメーションを停止する。
        actorAnimator.SetBool(runBool, false);

        // 実行中または予約されているアニメーショントリガーを解除する。
        actorAnimator.ResetTrigger(attack01TriggerHash);
        actorAnimator.ResetTrigger(attack02TriggerHash);
        actorAnimator.ResetTrigger(passiveSkillTriggerHash);
        actorAnimator.ResetTrigger(activeSkillTriggerHash);
        actorAnimator.ResetTrigger(ultSkillTriggerHash);

    }

    #endregion


    #region HP

    /// <summary>
    /// 色喪失レベルごとの回復可能な最大HP倍率。
    /// 色喪失レベルが高くなるほど、回復可能な最大HPが減少します。
    /// </summary>
    // 色喪失レベル：  0      1      2      3
    // 最大HP倍率：    1.00   0.75   0.50   0.25
    private readonly float[] recoverMaxHpScale = { 1.0f, 0.75f, 0.5f, 0.25f };

    /// <summary>
    /// 色喪失レベルが定義された範囲外かを判定します。
    /// </summary>
    /// <returns>
    /// 範囲外の場合はエラーログ出ます <see langword="true"/>。
    /// </returns>
    private bool IsLostColorLevelOverRange()
    {
        bool result = colorSystem.lostColorLevel < 0 || colorSystem.lostColorLevel > recoverMaxHpScale.Length - 1;
        if (result) Debug.LogError($"Lost Color Level no in Range [ 0 to {recoverMaxHpScale.Length - 1}  ]");
        return result;
    }
    /// <summary> 現在の色喪失レベルから、回復可能な最大HPを計算します。 </summary>
    /// <returns>現在回復できるHPの上限。</returns>
    private int CanRecoverMaxHp()
    {
        if (IsLostColorLevelOverRange()) return maxHp;
        return Mathf.RoundToInt(maxHp * recoverMaxHpScale[colorSystem.lostColorLevel]);
    }

    /// <summary> 色喪失レベルに応じて、HPバーの回復可能範囲を更新します。 </summary>
    protected void RecoverMaxHpBarChange()
    {
        if (IsLostColorLevelOverRange()) return;
        healthBar.ChangeBackBar(recoverMaxHpScale[colorSystem.lostColorLevel]).Forget();

    }
    protected override void GotHit(int damage)
    {
        base.GotHit(damage);
        if(nowHp > 0) SetNetworkTrigger(gotHitTriggerHash);

    }
    protected override void Death()
    {
        base.Death();
        stage = PlayerStage.Death;
        SetNetworkTrigger(deathTriggerHash);
    }

    /// <summary> 回復可能な最大HPを超えない範囲でHPを回復します。 </summary>
    /// <param name="recoverAmount">回復するHP量。</param>
    public virtual void Recover(int index)
    {
        if (!Object.HasStateAuthority) return;
        nowHp = Mathf.Min((nowHp + index), CanRecoverMaxHp());
    }

    /// <summary>
    /// 色喪失レベルを1段階回復し、
    /// 更新後の回復可能上限までHPを回復します。
    /// マップ上に設置された回復設備から呼び出すことを想定しています。
    /// </summary>
    public void PurifyRecover()
    {
        int nextLostLevel = Mathf.Max(colorSystem.lostColorLevel - 1, 0);
        colorSystem.ColorCharge(0, nextLostLevel);
        Recover(maxHp);
    }



    #endregion

    #region ActionProcess
    /// <summary>
    /// 指定された地点の方向を向き、通常攻撃を実行します。
    /// </summary>
    /// <param name="targetPosition">攻撃する方向を決める目標地点。</param>
    public virtual void Attack()
    {
        if (!IsAllowCommand()) return;

        Turn(trackingMonsterPosition, true);
        RequestChangeStage(PlayerStage.Attack);
    }
    /// <summary>
    /// 指定された地点の方向を向き、パッシブスキルを実行します。
    /// </summary>
    /// <param name="targetPosition">スキルを使用する方向を決める目標地点。</param>
    public virtual void PassiveSkill()
    {
        if (!IsAllowCommand()) return;
        Turn(trackingMonsterPosition, true);
        RequestChangeStage(PlayerStage.PassiveSkill);
    }

    /// <summary>
    /// アクティブスキルの発動に必要な色チャージ量を取得します。
    /// 派生クラスで職業ごとの消費量を定義します。
    /// </summary>
    public abstract uint activeSkillCost { get; }

    /// <summary>
    /// 色チャージを消費し、追跡中のモンスターへ向かって
    /// アクティブスキルを実行します。
    /// 追跡対象が存在しない場合は、キャラクターの正面へ発動します。
    /// </summary>
    public virtual void ActiveSkill()
    {
        // 被弾中、死亡中、または別のコマンドを実行中の場合は発動しない。
        // この場合、色チャージも消費しない。
        if (!IsAllowCommand() || !canDoNextCommand) return;

        // 必要な色チャージを消費できない場合は発動しない。
        if (!TryReduceColor(activeSkillCost)) return;

        // 追跡中のモンスター、またはキャラクターの正面を向く。
        Turn(trackingMonsterPosition, true);
        // アクティブスキルの状態へ変更し、アニメーションを再生する。
        RequestChangeStage(PlayerStage.ActiveSkill);

    }
    private Vector3 moveInput;
    public void SetMoveInput(Vector3 direction)
    {
        if (!Object.HasStateAuthority || !IsAllowCommand()) return;

        moveInput = direction;
    }


    private Vector3 turnInput;
    public void SetTurnInput(Vector3 newTurnInput)
    {
        if (!Object.HasStateAuthority || !IsAllowCommand()) return;
        turnInput = newTurnInput;

    }

    public override void Move(Vector3 moveDirection)
    {
        if(moveDirection == Vector3.zero)
        {
            actorAnimator.SetBool(runBool, false);
            CanDoNextCommand();
            return;
        }

        if (canDoNextCommand) RequestChangeStage(PlayerStage.Run);
        //Turn(moveDirection, true);
        base.Move(moveDirection);

    }

    #endregion

    #region Color   

    [Header("Color Charge")]
    /// <summary> キャラクターの色チャージ状態を管理するシステム。 </summary>
    public ColorSystem colorSystem;
    /// <summary> このキャラクターが使用する色システムを取得します。 </summary>
    public ColorSystem actorColorSystem => colorSystem;
    /// <summary> 自分が操作しているキャラクターの色チャージを表示するUIバー。 </summary>
    public ColorBar colorBar;

    /// <summary>
    /// 現在の色レベルに対応するマテリアルを、
    /// キャラクター本体と各ボディパーツへ適用します。
    /// </summary>
    protected virtual void ChangeMeshColor()
    {
        ColorMaterial colorLevel = colorSystem.m_colorLevel;
        int currentMaterialCount = colorLevel.materialSlotCount;

        // 必要な数のマテリアルが設定されていない場合は変更しない。
        if (currentMaterialCount != requiredMaterialCountPerLevel)
        {
            Debug.LogError($"マテリアルを{requiredMaterialCountPerLevel}個設定してください。" +
                $"現在の設定数：{colorSystem.m_colorLevel.materialSlotCount}個", this);
            return;
        }

        if (colorLevel.bodyMaterial == null)
        {
            Debug.LogError("メインボディ用のマテリアルが設定されていません。", this);
            return;
        }

        // 先頭のマテリアルは、必ずメインのボディメッシュに使用する。
        actorBodyMesh.material = colorSystem.m_colorLevel.bodyMaterial;
        // 2個目以降のマテリアルを、その他のボディパーツへ適用する。
        for (int partIndex = 0; partIndex < othersBodyMeshParts.Length; partIndex++)
            othersBodyMeshParts[partIndex].sharedMaterial = colorLevel.otherMaterials[partIndex];

    }


    public virtual bool TryReduceColor(uint value)
    {
        if (colorSystem == null) return false;
        // 減少後の色チャージ量と色喪失レベルを計算する。
        bool canUse = colorSystem.CanColorCharge(ColorChargeType.Decrease, value, true, out int nextCharge, out int nextLostLevel);
        // 必要な色チャージを消費できない場合は変更しない。
        if (!canUse) return false;
        bool needToChangeMaterial = colorSystem.lostColorLevel != nextLostLevel;
        // 計算済みの色チャージ量と色喪失レベルを反映する。
        colorSystem.ColorCharge(nextCharge, nextLostLevel);
        // 自分が操作しているキャラクターの場合のみ、
        // Canvas上の色チャージバーを更新する。
        if (colorBar != null) colorBar.ChangeValueTo(-value).Forget();
        // 色喪失レベルに応じてHPの回復可能上限を更新する。
        RecoverMaxHpBarChange();
        // 色喪失レベルが変化した場合のみ、マテリアルを変更する。
        if (needToChangeMaterial) ChangeMeshColor();
        return true;
    }


    #endregion

    protected void InitializeCharacter(Status status)
    {
        if (!Object.HasStateAuthority) return;

        actorStatus.StatusInit(status, allLevelScalePairs);

        networkMaxHp = maxHp;
        nowHp = networkMaxHp;

        ReturnToMinDefScale();
    }

    public void ChangeCanvasBar(HealthBar canvasHealthBar, ColorBar canvasColorBar)
    {
        healthBar.gameObject.SetActive(false);
        healthBar = canvasHealthBar;
        colorBar = canvasColorBar;
    }

    public override void ActorInit()
    {
        InGame.Instance?.RegisterCharacter(this);
        canBeTrack = true;

        if (!Object.HasStateAuthority) return;
        InitializeCharacter(_gameManager.gamePlayer.TargetCharacterStatus(characterType));
        if (networkMaxHp > 0) UpdateHealthBar(false);
    }
    public override void Spawned()
    {
        ActorInit();
        if (!Object.HasStateAuthority) return;
        AllStatusInit();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        InGame.Instance?.UnregisterCharacter(this);
    }



    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || _gameManager.nowGameScene != GameScene.InGame) return;

        Move(moveInput);
        Turn(turnInput, false);
        if (trackingActor == null)
        {
            TryTrackActor(); 
        }
        else
        {
            UpdateTracking(1.1f);
        }

    }

}
