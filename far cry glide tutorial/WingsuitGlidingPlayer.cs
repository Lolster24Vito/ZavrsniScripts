using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WingsuitGlidingPlayer : MonoBehaviour
{
    public float speedZ;
    public float speedRotation = 20f;

    public float drag;
    public Rigidbody rb;
    public float rotationPercentage;

    private Vector3 rot;
    [SerializeField] private float flapCurrentSpeed;
    

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rot = transform.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
       /* if (Input.GetKeyDown(KeyCode.Space))
        {
            flapCurrentSpeed += 30;
        }*/
        //ROTATE player
        //x
        rot.x += speedRotation * (Input.GetAxis("Vertical") * -1) * Time.deltaTime;
      //  rot.x = Mathf.Clamp(rot.x, 0, 45);
        //y
        rot.y += speedRotation * Input.GetAxis("Horizontal") * Time.deltaTime;
        transform.rotation = Quaternion.Euler(rot);
        //drag
        rotationPercentage = rot.x / 45;
        //drag( fast4 slow6????)
        //float mod_drag = (rotationPercentage * -2) + 6;
        //1 fast slow 6
        float mod_drag = (rotationPercentage * -5) + 6;

        //speed: fast(13.8) slow (12.5)
        float mod_speed = rotationPercentage * (13.8f - 12.5f) + 12.5f;
        //movement
        rb.drag = Mathf.Clamp(mod_drag,0f,mod_drag);
        Vector3 localV = transform.InverseTransformDirection(rb.velocity);
        localV.z = mod_speed;
        rb.velocity = transform.TransformDirection(localV);


    }
}
