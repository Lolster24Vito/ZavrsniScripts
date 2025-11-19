using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianObjectOffseter : ObjectOffsetter
{
    CharacterController characterController;
    Pedestrian pedestrian;
    [SerializeField] bool applyOffsetOnEnable = false;
   protected override void Awake()
    {
        characterController = GetComponent<CharacterController>();
        pedestrian = GetComponent<Pedestrian>();
    }
    bool originalActiveState = false;
    private void OnEnable()
    {
        Vector3 tileManagerOffset;
        if (applyOffsetOnEnable)
        {

        if (TileManager.TryGetOffset(pedestrian.GetTile(), out tileManagerOffset))
        {
            ApplyOffset(WorldRecenterManager.Instance.GetCustomWorldOffsetWithoutFirst() - tileManagerOffset);
        }
        }


    }
    protected override void ApplyOffset(Vector3 offset)
    {
        // originalActiveState = gameObject.activeSelf;
        //   gameObject.SetActive(true);
        //   StartCoroutine(CorutineSoThatTheEntityCanActuallyMove(offset));
        if (characterController != null)
        {

        characterController.enabled = false;
        offset.y = 0f;
        base.ApplyOffset(offset);
        Physics.SyncTransforms();
        if(characterController!=null)
        characterController.enabled = true;
        Physics.SyncTransforms();
        }
        else
        {
            offset.y = 0f;
            base.ApplyOffset(offset);

        }

    }
    IEnumerator CorutineSoThatTheEntityCanActuallyMove(Vector3 offset)
    {
        characterController.enabled = false;
        offset.y = 0f;
        yield return null;
        base.ApplyOffset(offset);
        yield return null;
        if (characterController != null)
            characterController.enabled = true;
        gameObject.SetActive(originalActiveState);

    }
}
