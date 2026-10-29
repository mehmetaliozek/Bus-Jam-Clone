using UnityEngine;

public class PassengerAnimationStateEvents : StateMachineBehaviour
{
    [SerializeField] private string _stateName;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PassengerAnimatorController controller = animator.GetComponentInParent<PassengerAnimatorController>();

        if (controller != null) controller.NotifyStateEntered(_stateName);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PassengerAnimatorController controller = animator.GetComponentInParent<PassengerAnimatorController>();

        if (controller != null) controller.NotifyStateExited(_stateName);
    }
}