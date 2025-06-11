using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFollowTransform : MonoBehaviour
{
    [SerializeField] private Vector3 followOffset = new Vector3(-59.96757f, -180.63092f, -252.30391f);

    [SerializeField]
    private Transform[] _transformsFollowingMe;
    private Transform _transform;

    void Start()
    {
        _transform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < _transformsFollowingMe.Length; i++)
        {

            _transformsFollowingMe[i].position = _transform.position + followOffset;

        }


    }
}
