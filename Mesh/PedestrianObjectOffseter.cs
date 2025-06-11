using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianObjectOffseter : ObjectOffsetter
{
    CharacterController characterController;
    Pedestrian pedestrian;
   protected override void Awake()
    {
        characterController = GetComponent<CharacterController>();
        pedestrian = GetComponent<Pedestrian>();
    }
    bool originalActiveState = false;
    private void OnEnable()
    {
        Vector3 tileManagerOffset;
        if (TileManager.TryGetOffset(pedestrian.GetTile(), out tileManagerOffset))
        {
            ApplyOffset(WorldRecenterManager.Instance.GetCustomWorldOffsetWithoutFirst() - tileManagerOffset);
        }
        
    }
    protected override void ApplyOffset(Vector3 offset)
    {
        // originalActiveState = gameObject.activeSelf;
        //   gameObject.SetActive(true);
        //   StartCoroutine(CorutineSoThatTheEntityCanActuallyMove(offset));
        characterController.enabled = false;
        offset.y = 0f;
        base.ApplyOffset(offset);
        Physics.SyncTransforms();
        characterController.enabled = true;
        Physics.SyncTransforms();

    }
    IEnumerator CorutineSoThatTheEntityCanActuallyMove(Vector3 offset)
    {
        characterController.enabled = false;
        offset.y = 0f;
        yield return null;
        base.ApplyOffset(offset);
        yield return null;
        characterController.enabled = true;
        gameObject.SetActive(originalActiveState);

    }
}
