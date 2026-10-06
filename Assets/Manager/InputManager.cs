
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーからのキーボード・マウス入力を取得し、
/// 操作中のキャラクターへ移動、回転、攻撃、
/// スキルなどの入力を渡すクラス。
/// </summary>
public class InputManager : MonoBehaviour
{

    private GameManager _gameManager => GameManager.Instance;
    public Character _character => _gameManager.gamePlayer.controlling_Character;
    /// <summary>
    /// 現在使用しているキーボードを取得する。
    /// </summary>
    private Keyboard _keyboard => Keyboard.current;
    /// <summary>
    /// 現在使用しているマウスを取得する。
    /// </summary>
    private Mouse _mouse => Mouse.current;
    /// <summary>
    /// キーボード入力からキャラクターの移動方向を取得する。
    /// キャラクター自身の向きを基準として移動方向を計算する。
    /// </summary>
    /// <returns>正規化された移動方向。</returns>
    private Vector3 ReadMoveInput()
    {
        if (_character == null) return Vector3.zero;
        // キャラクターの正面方向を取得する。
        Vector3 characterForward = _character.transform.forward;
        // キャラクターの右方向を取得する。
        Vector3 characterRight = _character.transform.right;

        // 水平方向のみを移動計算に使用する。
        characterForward.y = 0.0f;
        characterRight.y = 0.0f;

        characterForward.Normalize();
        characterRight.Normalize();

        Vector3 inputDirection = Vector3.zero;

        // Wキー入力をキャラクターの正面方向へ加算する。
        if (_keyboard.wKey.isPressed) inputDirection += characterForward;
        //if (_keyboard.sKey.isPressed) inputDirection -= characterForward;
        //if (_keyboard.aKey.isPressed) inputDirection -= characterRight;
        //if (_keyboard.dKey.isPressed) inputDirection += characterRight;

        return inputDirection.normalized;

    }

    /// <summary>
    /// 現在の移動入力をキャラクターへ設定する。
    /// </summary>
    private void CharacterMove()=> _character.SetMoveInput(ReadMoveInput());

    /// <summary>
    /// マウス位置からキャラクターの回転方向を取得する。
    /// 画面中央を基準としてマウスの相対位置を計算する。
    /// </summary>
    /// <returns>キャラクターの回転計算に使用する方向。</returns>
    private Vector3 ReadTurnInput()
    {
        if (_character == null || _mouse == null) return Vector3.zero;
        // 画面中央を原点としてマウスの相対位置を取得する。
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue() - new Vector2(Screen.width / 2f, Screen.height / 2f);
        // マウスのX・Y位置をワールド上のX・Z方向として使用する。
        Vector3 direction = new Vector3(mouseScreenPosition.x, 0, mouseScreenPosition.y);
        return direction;
    }

    /// <summary>
    /// マウス入力から回転方向を計算し、
    /// キャラクターへ回転入力を設定する。
    /// </summary>
    private void CharacterTurn()
    {
        Vector3 result = _character.transform.forward + ReadTurnInput();
        _character.SetTurnInput(result);
    }
    /// <summary>
    /// 通常攻撃の入力を確認し、
    /// 入力された場合はキャラクターの通常攻撃を実行する。
    /// </summary>
    /// <returns>通常攻撃の入力があった場合はtrue。</returns>
    private bool CharacterAttack()
    {
        if (_character == null || _mouse == null) return false;
        // 左クリックされたフレームのみ通常攻撃を実行する。
        if (!_mouse.leftButton.wasPressedThisFrame) return false;
        else
        {
            _character.Attack();
            return true;
        }

    }
    /// <summary>
    /// パッシブスキルの入力を確認し、
    /// 入力中の場合はキャラクターのパッシブスキルを実行する。
    /// </summary>
    /// <returns>パッシブスキルの入力がある場合はtrue。</returns>
    private bool CharacterPassiveSkill()
    {
        if (_character == null || _keyboard == null) return false;
        // Fキーが押されている間、パッシブスキルを実行する。
        if (!_keyboard.fKey.isPressed) return false;
        else 
        {
            _character.PassiveSkill();
            return true;
        }
        

    }
    /// <summary>
    /// アクティブスキルの入力を確認し、
    /// 入力された場合はキャラクターのアクティブスキルを実行する。
    /// </summary>
    /// <returns>アクティブスキルの入力があった場合はtrue。</returns>
    private bool CharacterActiveSkill()
    {
        if (_character == null || _keyboard == null) return false;
        // Eキーが押されたフレームのみアクティブスキルを実行する。
        if (!_keyboard.eKey.wasPressedThisFrame) return false;
        else
        {
            _character.ActiveSkill();
            return true;
        }


    }
    /// <summary>
    /// InGame中のプレイヤー入力を処理する。
    /// 回転入力を更新した後、
    /// パッシブスキル、通常攻撃、アクティブスキル、
    /// 移動の優先順で入力を処理する。
    /// </summary>
    private void InGameInput()
    {
        // InGame以外ではキャラクター入力を処理しない。
        if (_gameManager == null || _gameManager.nowGameScene != GameScene.InGame) return;
        // 入力デバイスまたは操作キャラクターが存在しない場合は処理しない。
        if (_keyboard == null || _character == null || _mouse == null) return;

        // キャラクターの回転入力を更新する。
        CharacterTurn();
        // パッシブスキル入力中は移動入力を停止する。
        if (CharacterPassiveSkill())
        {
            _character.SetMoveInput(Vector3.zero);
            return;
        }
        // 通常攻撃入力中は移動入力を停止する。
        else if (CharacterAttack())
        {
            _character.SetMoveInput(Vector3.zero);
            return;
        }
        // アクティブスキル入力中は移動入力を停止する。
        else if (CharacterActiveSkill())
        {
            _character.SetMoveInput(Vector3.zero);
            return;
        }
        // 他のアクション入力がない場合は移動入力を処理する。
        else CharacterMove();
    }



    private void Update()
    {
        /// <summary>
        /// 毎フレーム、InGame中のプレイヤー入力を更新する。
        /// </summary>
        InGameInput();
    }


}