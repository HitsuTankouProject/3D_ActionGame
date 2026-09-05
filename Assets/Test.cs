using UnityEngine;


public class Test : MonoBehaviour,IDamage
{
 
    public virtual void GotDamage(int damage)
    {
        Debug.Log(damage);
    }
}   
