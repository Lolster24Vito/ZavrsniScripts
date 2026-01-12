using UnityEngine;

public class RagdollPlayerActivator : MonoBehaviour
{
    [SerializeField] GlideStateMachineBodyPoses glideStateMachineBodyPoses;
    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = glideStateMachineBodyPoses.GetFlapVelocity() + 20f;
        Vector3 impactDir = (collision.transform.position - transform.position).normalized;
        Vector3 contactPoint = collision.GetContact(0).point;

        if (collision.gameObject.layer == LayerMask.NameToLayer("NPC"))
        {
            RagdollSwapper.Instance.SwapToRagdoll(
                     collision.gameObject,
                     impactForce,
                     contactPoint,
                     impactDir
                 );

        }
            if(collision.gameObject.layer == LayerMask.NameToLayer("NPC_Ragdoll")) {
            Ragdoll collisionRagdoll = collision.gameObject.GetComponentInParent<Ragdoll>();
            if (collisionRagdoll != null)
            {
                collisionRagdoll.TriggerRagdoll(
                                    impactForce,
                                    contactPoint,
                                    impactDir
                                );
            }
        }
        //old code
        /*
        Debug.Log(" ON COLLISION WITH RAGDOLL");
        //forceDirection and flapVelocity get through 
        Ragdoll collisionRagdoll = collision.gameObject.GetComponentInParent<Ragdoll>();
        if (collisionRagdoll != null)
        {
            collisionRagdoll.TriggerRagdoll(glideStateMachineBodyPoses.GetFlapVelocity() + 20f, collision.GetContact(0).point, glideStateMachineBodyPoses.GetAimingDirection());
        }
        */
    

    }
    private void OnCollisionStay(Collision collision)
    {

        if (collision.gameObject.layer == LayerMask.NameToLayer("NPC") ||
           collision.gameObject.layer == LayerMask.NameToLayer("NPC_Ragdoll"))
        {

            //forceDirection and flapVelocity get through 
            Ragdoll collisionRagdoll = collision.gameObject.GetComponentInParent<Ragdoll>();
            if (collisionRagdoll != null)
            {
                collisionRagdoll.TriggerRagdoll(glideStateMachineBodyPoses.GetFlapVelocity() + 20f, collision.GetContact(0).point, glideStateMachineBodyPoses.GetAimingDirection());
            }

        }
    }
}
