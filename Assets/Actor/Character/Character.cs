
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
    /// <summary>
    /// キャラクターのメッシュに使用する、メインボディ以外のパーツ。
    /// </summary>
    public MeshRenderer[] othersBodyMeshParts;

    /// <summary>
    /// ネットワーク上でAnimatorのTriggerを同期するために使用する。
    /// </summary>
    public NetworkMecanimAnimator networkAnimator;
    /// <summary>
    /// キャラクターの移動処理に使用するCharacterController。
    /// </summary>
    [SerializeField] protected CharacterController characterController;
    /// <summary>
    /// キャラクターの職業を取得する。
    /// </summary>
    public abstract CharacterType characterType { get; }
    /// <summary>
    /// 現在の色レベルを適用するために必要なマテリアル数を取得する。
    /// メインボディ1個と、その他のボディパーツ数の合計。
    /// </summary>
    private int requiredMaterialCountPerLevel => (othersBodyMeshParts?.Length ?? 0) + 1;

    [Header("Character Stage")]
    /// <summary>キャラクターの現在の行動状態を表します。</summary>
    public PlayerStage stage;
    /// <summary>
    /// 現在の追跡対象を解除する。
    /// </summary>
    protected override void StopTracking()
    {
        base.StopTracking();
       // _inGame.playerCameraFollow.ToggleAlwaysBehindCharacter();

    }
    /// <summary>
    /// 追跡可能距離内に存在するモンスターを検索し、
    /// 最初に見つかった追跡可能なモンスターを追跡対象に設定する。
    /// </summary>
    /// <returns>追跡対象を取得できた場合はtrue。</returns>
    protected override bool TryTrackActor()
    {
        if (_inGame.allMonsters == null || _inGame.allMonsters.Count == 0) return false;

        foreach (Monster monster in _inGame.allMonsters)
        {
            // 存在しないモンスター、または追跡不可能なモンスターは除外する。
            if (monster == null || !monster.canBeTrack) continue;
            // 追跡可能距離より遠いモンスターは対象外とする。
            if (Vector3.Distance(monster.transform.position, transform.position) > trackDistance) continue;

            trackingActor = monster;
            Turn(trackingActorPosition);
            //_inGame.playerCameraFollow.TurnCameraToCharacterBack();

            return true;

        }

        return false;

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
    private static readonly int passiveSkillTriggerHash = Animator.StringToHash(passiveSkillTrigger);

    private const string ultSkillTrigger = "UltSkill";
    private static readonly int ultSkillTriggerHash = Animator.StringToHash(ultSkillTrigger);

    /// <summary>
    /// NetworkMecanimAnimatorを使用して、
    /// ネットワーク上でAnimatorのTriggerを再生する。
    /// </summary>
    /// <param name="triggerHash">再生するAnimator TriggerのHash値。</param>
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


    #region Attack
    [Header("Attack")]
    /// <summary>
    /// 通常攻撃のコンボを最初へ戻すまでの時間。
    /// </summary>
    [SerializeField] private float attackReturnToFirstAttackTime = 0.5f;
    /// <summary>
    /// 次に再生する通常攻撃アニメーションのIndex。
    /// </summary>
    private int currentAttackIndex = 0;
    /// <summary>
    /// 通常攻撃のコンボ継続時間を管理するタイマー。
    /// </summary>
    [Networked] private TickTimer normalAttackReturnTimer { get; set; }
    /// <summary>
    /// 通常攻撃のコンボ継続タイマーが動作中か確認する。
    /// </summary>
    /// <returns>タイマーが動作中の場合はtrue。</returns>
    private bool IsNormalAttackReturnTimerCounting() 
        => normalAttackReturnTimer.IsRunning && !normalAttackReturnTimer.Expired(Runner);

    /// <summary>
    /// 通常攻撃のコンボ継続タイマーを開始する。
    /// </summary>
    private void SetAttackReturnTimer() 
        => normalAttackReturnTimer = TickTimer.CreateFromSeconds(Runner, attackReturnToFirstAttackTime);

    #endregion

    /// <summary>
    /// 登録されている通常攻撃アニメーションを順番に再生する。
    /// 一定時間以内に次の攻撃を行わなかった場合は、
    /// 最初の攻撃アニメーションへ戻る。
    /// </summary>
    protected virtual void Animation_Attack()
    {
        if (!Object.HasStateAuthority) return;
        if (allNormalAttackHashes.Length == 0) return;
        // コンボ継続時間を超えている場合は、
        // 最初の通常攻撃へ戻す。
        if (!IsNormalAttackReturnTimerCounting()) currentAttackIndex = 0;
        // 前回設定された攻撃トリガーを解除する。
        ReSetAllTheAttackTrigger();
        // 現在の通常攻撃アニメーションを再生する。
        SetNetworkTrigger(allNormalAttackHashes[currentAttackIndex]);

        // 次の通常攻撃Indexへ進める。
        // 最後まで進んだ場合は0へ戻る。
        currentAttackIndex = (currentAttackIndex + 1) % allNormalAttackHashes.Length;
        // コンボ継続時間を更新する。
        SetAttackReturnTimer();
    }



    /// <summary> 走行アニメーションを開始します。 </summary>
    protected void Animation_Run()
    {
        if (!Object.HasStateAuthority) return;

        if (!actorAnimator.GetBool(runBool)) actorAnimator.SetBool(runBool, true);
    }

    /// <summary>　操作できる状態なのかを判定します。</summary>
    /// <returns>　被弾中または死亡中ではない場合は <see langword="true"/>。</returns>
    protected bool IsAllowCommand() => stage != PlayerStage.Hit && stage != PlayerStage.Death;

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
        if (Object == null || !Object.IsValid) return;
        if (!Object.HasStateAuthority) return;
        if (actorAnimator == null) return;


        stage = PlayerStage.Idle;

        // 走行アニメーションを停止する。
        actorAnimator.SetBool(runBool, false);

        // 実行中または予約されているアニメーショントリガーを解除する。
        ReSetAllTheAttackTrigger();
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
    /// <summary>
    /// キャラクターへダメージを適用する。
    /// HPが残っている場合は被ダメージアニメーションを再生する。
    /// </summary>
    /// <param name="damage">適用するダメージ量。</param>
    protected override void GotHit(int damage)
    {
        base.GotHit(damage);
        if(nowHp > 0) SetNetworkTrigger(gotHitTriggerHash);

    }
    /// <summary>
    /// キャラクターの死亡処理を行い、
    /// 死亡アニメーションを再生してInGameの管理対象から解除する。
    /// </summary>
    protected override void Death()
    {
        base.Death();
        stage = PlayerStage.Death;
        SetNetworkTrigger(deathTriggerHash);
        _inGame.UnregisterCharacter(this);
    }

    /// <summary> 回復可能な最大HPを超えない範囲でHPを回復します。 </summary>
    /// <param name="recoverAmount">回復するHP量。</param>
    public virtual void Recover(int recoverAmount)
    {
        if (!Object.HasStateAuthority) return;
        nowHp = Mathf.Min((nowHp + recoverAmount), CanRecoverMaxHp());
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

        Turn(trackingActorPosition);
        RequestChangeStage(PlayerStage.Attack);
    }
    /// <summary>
    /// 指定された地点の方向を向き、パッシブスキルを実行します。
    /// </summary>
    /// <param name="targetPosition">スキルを使用する方向を決める目標地点。</param>
    public virtual void PassiveSkill()
    {
        if (!IsAllowCommand()) return;
        Turn(trackingActorPosition);
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
        Turn(trackingActorPosition);
        // アクティブスキルの状態へ変更し、アニメーションを再生する。
        RequestChangeStage(PlayerStage.ActiveSkill);

    }
    /// <summary>
    /// FixedUpdateNetworkで使用する移動入力。
    /// </summary>
    private Vector3 moveInput;
    /// <summary>
    /// キャラクターの移動入力を設定する。
    /// </summary>
    /// <param name="direction">移動方向。</param>
    public void SetMoveInput(Vector3 direction)
    {
        if (!Object.HasStateAuthority || !IsAllowCommand()) return;

        moveInput = direction;
    }

    /// <summary>
    /// FixedUpdateNetworkで使用する回転入力。
    /// </summary>
    private Vector3 turnInput;
    /// <summary>
    /// キャラクターの回転入力を設定する。
    /// </summary>
    /// <param name="newTurnInput">キャラクターが向く方向。</param>
    public void SetTurnInput(Vector3 newTurnInput)
    {
        if (!Object.HasStateAuthority || !IsAllowCommand()) return;
        turnInput = newTurnInput;

    }
    /// <summary>
    /// 指定された方向へキャラクターを回転させる。
    /// Y軸方向の値は回転計算から除外する。
    /// </summary>
    /// <param name="faceTo">キャラクターが向く方向。</param>
    protected override void Turn(Vector3 faceTo)
    {
        // 水平方向のみを使用して回転する。
        faceTo.y = 0f;
        // 方向ベクトルがほぼ0の場合は回転しない。
        if (faceTo.sqrMagnitude < 0.0001f) return;

        base.Turn(faceTo);
    }
    /// <summary>
    /// 指定された方向へキャラクターを移動する。
    /// 移動入力がない場合は走行アニメーションを停止する。
    /// </summary>
    /// <param name="moveDirection">キャラクターの移動方向。</param>
    public override void Move(Vector3 moveDirection)
    {
        if (!Object.HasStateAuthority) return;

        if (moveDirection == Vector3.zero)
        {
            // 移動入力がない場合は走行状態を解除する。
            actorAnimator.SetBool(runBool, false);

            CanDoNextCommand();
            return;
        }

        // 次のコマンドを実行可能な場合はRun状態へ変更する。
        if (canDoNextCommand) RequestChangeStage(PlayerStage.Run);

        // Y軸方向の入力は移動に使用しない。
        moveDirection.y = 0.0f;

        // 入力値が1未満の場合は方向ベクトルを正規化する。
        if (moveDirection.sqrMagnitude < 1.0f) moveDirection.Normalize();

        characterController.Move(moveDirection * actorStatus.actorBasicStatus.actorMoveSpeed * Runner.DeltaTime);
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

    /// <summary>
    /// 指定された量の色チャージを消費する。
    /// 色喪失レベルが変化した場合は、HP回復上限と
    /// キャラクターのマテリアルを更新する。
    /// </summary>
    /// <param name="value">消費する色チャージ量。</param>
    /// <returns>色チャージを消費できた場合はtrue。</returns>
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

    /// <summary>
    /// 指定された量の色チャージを増加させる。
    /// 色喪失レベルが変化した場合は、HP回復上限と
    /// キャラクターのマテリアルを更新する。
    /// </summary>
    /// <param name="value">増加する色チャージ量。</param>
    /// <returns>色チャージを変更できた場合はtrue。</returns>
    public virtual bool TryIncreaseColor(uint value)
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
        if (colorBar != null) colorBar.ChangeValueTo(+value).Forget();
        // 色喪失レベルに応じてHPの回復可能上限を更新する。
        RecoverMaxHpBarChange();
        // 色喪失レベルが変化した場合のみ、マテリアルを変更する。
        if (needToChangeMaterial) ChangeMeshColor();
        return true;
    }

    #endregion

    /// <summary>
    /// プレイヤーデータに登録されている武器情報を取得し、
    /// キャラクターのメイン武器を初期化する。
    /// </summary>
    protected virtual void InitializeCharacterWeapon()
    {
        //WeaponStatus[] allWeaponStatus
        if (_player.allWeaponStatus.Length <= 0)
        {
            Debug.LogError("[Character] Player`s AllWeaponStatus Length <=0");
            return;
        }
        // 登録されている最初の武器をメイン武器として使用する。
        WeaponStatus mainWeaponStatus = _player.allWeaponStatus[0];
        mainWeapon.WeaponInit(this, mainWeaponStatus);


    }

    /// <summary>
    /// プレイヤーデータから現在の職業に対応する
    /// ステータスを取得し、ActorStatusへ設定する。
    /// </summary>
    protected void InitializeCharacter()
    {
        if (!Object.HasStateAuthority) return;
        if(!_player.TryGetCharacterStatus(characterType,out Status status))
        {
            Debug.LogError($"[Chectaor] Can Find {characterType.ToString()}`s Status");
            return;
        }
        actorStatus.StatusInit(status);
    }


    /// <summary>
    /// キャラクターが使用するHPバーと色チャージバーを
    /// Canvas上のUIへ切り替える。
    /// </summary>
    /// <param name="canvasHealthBar">変更後のHPバー。</param>
    /// <param name="canvasColorBar">変更後の色チャージバー。</param>
    public void ChangeCanvasBar(HealthBar canvasHealthBar, ColorBar canvasColorBar)
    {
        // キャラクター側に設定されていたHPバーを非表示にする。
        healthBar.gameObject.SetActive(false);
        healthBar = canvasHealthBar;
        colorBar = canvasColorBar;
    }
    /// <summary>
    /// キャラクターをInGameへ登録し、
    /// ステータスと武器などの初期設定を行う。
    /// </summary>
    public override void ActorInit()
    {
        InGame.Instance?.RegisterCharacter(this);
        canBeTrack = true;

        if (!Object.HasStateAuthority) return;
        InitializeCharacter();
        InitializeCharacterWeapon();
        if (networkMaxHp > 0) UpdateHealthBar(false);
    }
    /// <summary>
    /// NetworkObjectがSpawnされた際に、
    /// キャラクターとステータスを初期化する。
    /// </summary>
    public override void Spawned()
    {
        ActorInit();
        if (!Object.HasStateAuthority) return;
        AllStatusInit();
        
    }
    /// <summary>
    /// NetworkObjectがDespawnされた際に、
    /// InGameのキャラクター管理から登録を解除する。
    /// </summary>
    /// <param name="runner">使用中のNetworkRunner。</param>
    /// <param name="hasState">Despawn時にStateを保持しているか。</param>
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        InGame.Instance?.UnregisterCharacter(this);
    }


    /// <summary>
    /// State Authority側で毎Network Tick、
    /// 移動・回転・追跡対象の更新を行う。
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !_inGame.IsGamePlaying()) return;


        // 保存されている入力を使用して移動と回転を更新する。
        Move(moveInput);
        Turn(turnInput);

        // 追跡対象が存在しない場合は新しい対象を検索する。
        //if (trackingActor == null)
        //{
        //    TryTrackActor(); 
        //}
        // すでに対象が存在する場合は追跡状態を更新する。
        //else
        //{
        //    UpdateTracking();
        //}

    }

}
