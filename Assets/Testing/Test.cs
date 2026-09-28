using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.Unicode;

public class Test : MonoBehaviour
{
     void Turn(Vector3 faceTo)
    {
        Vector3 direction = faceTo - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mouseFromCenter = Mouse.current.position.ReadValue()
            - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Vector3 targetPosition = transform.position
            + new Vector3(mouseFromCenter.x, 0f, mouseFromCenter.y);

        Turn(targetPosition);
    }







}