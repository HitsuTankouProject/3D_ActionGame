using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TextCore.Text;

/// <summary>
/// プレイヤーからのキーボード・マウス入力を取得し、
/// 操作中のキャラクターへ移動、回転、攻撃、
/// スキルなどの入力を渡すクラス。
/// </summary>
public static class InputManager
{
    /// <summary>
    /// 現在使用しているキーボードを取得する。
    /// </summary>
    private static Keyboard _keyboard => Keyboard.current;
    /// <summary>
    /// 現在使用しているマウスを取得する。
    /// </summary>
    private static Mouse _mouse => Mouse.current;

    public static Vector2 ReadMouseMoveValue() => _mouse.delta.ReadValue();
    public static bool canMoveMousePad { get; private set; } = false;

    public static void OnMoveMousePad()
    {
        canMoveMousePad = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    private static void OffMoveMousePad()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        canMoveMousePad = false;
    }
    public static void IsOnMoveMousePadKetDown()
    {
        if (_keyboard == null) return;
        if (_keyboard.altKey.isPressed) OnMoveMousePad();
        else OffMoveMousePad();
    }

    public static Vector3 ReadMoveValue()
    {
        Vector3 inputDirection = Vector3.zero;
        if (canMoveMousePad) return inputDirection.normalized;


        if (_keyboard.wKey.isPressed) inputDirection += Vector3.forward;
        if (_keyboard.sKey.isPressed) inputDirection += Vector3.back;
        if (_keyboard.aKey.isPressed) inputDirection += Vector3.left;
        if (_keyboard.dKey.isPressed) inputDirection += Vector3.right;

        Quaternion cameraYaw = Quaternion.Euler(0.0f, Camera.main.transform.eulerAngles.y, 0.0f);

        return cameraYaw * inputDirection.normalized;

    }

    public static bool IsNormalAttackKeyDown()
    {
        if (_mouse == null) return false;
        return _mouse.leftButton.wasPressedThisFrame;
    }
    public static bool IsPassiveSkillKeyDown()
    {
        if (_keyboard == null) return false;
        return _keyboard.fKey.isPressed;
    }
    public static bool IsActiveSkillKeyDown()
    {
        if (_keyboard == null) return false;
        return _keyboard.eKey.wasPressedThisFrame;
    }
    public static bool IsUltSkillKeyDown()
    {
        if (_keyboard == null) return false;
        return _keyboard.qKey.wasPressedThisFrame;
    }



}