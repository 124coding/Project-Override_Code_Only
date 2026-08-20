using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class FieldPlayerInteraction : MonoBehaviour
{
    private HashSet<InteractObject> targetsInRange = new HashSet<InteractObject>();

    private void OnEnable()
    {
        // 이제 여기서 입력을 듣습니다.
        InputManager.Instance.inputActions.Field.Interact.performed += TryInteract;
    }

    private void OnDisable()
    {
        InputManager.Instance.inputActions.Field.Interact.performed -= TryInteract;
    }

    private void TryInteract(InputAction.CallbackContext context)
    {
        // 범위 내에 있는 상호작용 대상들 중 가장 가까운 놈을 찾아서 실행
        InteractObject bestTarget = GetClosestTarget();
        bestTarget?.ExecuteInteraction();
    }

    private InteractObject GetClosestTarget()
    {
        InteractObject closest = null;
        float minDistance = float.MaxValue;
        foreach (var target in targetsInRange)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < minDistance) { minDistance = dist; closest = target; }
        }
        return closest;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var target = collision.GetComponentInParent<InteractObject>();
        if (target != null) targetsInRange.Add(target);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var target = collision.GetComponentInParent<InteractObject>();
        if (target != null) targetsInRange.Remove(target);
    }
}