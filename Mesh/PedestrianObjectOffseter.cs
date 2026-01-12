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
        base.Awake(); // Good practice to call base.Awake too
        characterController = GetComponent<CharacterController>();
        pedestrian = GetComponent<Pedestrian>();
    }
    bool originalActiveState = false;
    protected override void Start()
    {
        base.Start(); // CRITICAL: This subscribes to the WorldRecenterManager event
        if (!applyOffsetOnEnable) return;

        Vector2Int tileToUse;
        Vector3 tileManagerOffset;


        if (TileManager.TryGetOffset(pedestrian.GetTile(), out tileManagerOffset))
        {
            ApplyOffset(WorldRecenterManager.Instance.GetCustomWorldOffsetWithoutFirst() - tileManagerOffset);
        }


    }
    protected override void ApplyOffset(Vector3 offset)
    {
        // This logic is correct and handles the CharacterController properly.
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        //y is 0?
        // The base class applies the offset to the transform.
        base.ApplyOffset(offset);

        if (characterController != null)
        {
            characterController.enabled = true;
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
