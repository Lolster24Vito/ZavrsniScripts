using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectOffsetter : MonoBehaviour
{

    private void Start()
    {
        WorldRecenterManager.OnWorldRecentered += ApplyOffset;
    }

    private void OnDestroy()
    {
        WorldRecenterManager.OnWorldRecentered -= ApplyOffset;
    }
    protected virtual void Awake()
    {
        transform.position -= WorldRecenterManager.Instance.GetRecenterOffset();
    }
    protected virtual void ApplyOffset(Vector3 offset)
    {
        // Offset this object's position

        Vector3 origPos = transform.position;
        Vector3 newPos =origPos-offset;
        transform.position = new Vector3(newPos.x,newPos.y,newPos.z); //had to add new Vector3 because of character controller not moving 
        Debug.Log($"{gameObject.name} offset by {offset}, origPos:{origPos}, newPos:{newPos}");
    }
}
