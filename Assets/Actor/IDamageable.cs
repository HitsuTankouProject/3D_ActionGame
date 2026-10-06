using UnityEngine;

/// <summary>
/// ダメージを受けることができるオブジェクトが実装するインターフェース。
/// </summary>
public interface IDamageable
{
        /// <summary>
    /// 指定されたダメージを受ける。
    /// </summary>
    /// <param name="damage">受けるダメージ量。</param>
    void TakeDamage(int damage);
}

