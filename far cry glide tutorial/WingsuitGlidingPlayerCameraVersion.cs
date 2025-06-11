using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WingsuitGlidingPlayerCameraVersion : MonoBehaviour
{
    public float speedRotation = 20f;

    public float currentDrag;
    public float currentSpeed;
    public Rigidbody rb;

    [SerializeField] private float dragFastAngleValue = 0f;
    [SerializeField] private float dragSlowAngleValue = 9f;

    [SerializeField] private float speedFastAngle = 13.8f;
    [SerializeField] private float speedSlowAngle = 12.5f;
    [SerializeField] private float flapSpeed = 10f;
    [SerializeField] private float flapCurrentSpeed;
    [SerializeField] private float flapSlowDownStrength = 5f;

    private Vector3 rot;

    [SerializeField] private Vector3 debugVelocityViewerVariable;
    [Header("Debug dot  experimenting")]

    [SerializeField] private float debugDragTestPlus90;
    [SerializeField] private float debugDragTestMinus90;


    private bool firstTimeDebug = true;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rot = transform.eulerAngles;
    }

    // Update is called once per frame
    public void testingValuesOnDegrees(float degreeInput)
    {
        float degree = Mathf.Repeat(degreeInput, 360f);
        rot.x = degree;
        movePlayer();

        //math testing


    }

    void Update()
    {
        //if (firstTimeDebug)
       // {
            //firstTimeDebug = false;
            //225
            //testingValuesOnDegrees(-90f);
          //  testingValuesOnDegrees(-20);
           // testingValuesOnDegrees(15f);


            //     testingValuesOnDegrees(-90f);

            // testingValuesOnDegrees(135f);

            // testingValuesOnDegrees(45f);


            //  testingValuesOnDegrees(-45f);
        //}
        //else
        //{
            rotatePlayer();
            movePlayer();
        //}
        if (Input.GetKeyDown(KeyCode.Space))
        {
            flapCurrentSpeed += flapSpeed;
        }
        if (flapCurrentSpeed >= 0f)
        {
            flapCurrentSpeed -= flapSlowDownStrength*Time.deltaTime;
        }


    }
    private void movePlayer()
    {
        currentDrag= getDrag();
        rb.drag = currentDrag;

        //movement
           currentSpeed = getSpeed();

        Vector3 localV = transform.InverseTransformDirection(rb.velocity);
        localV.z = currentSpeed;
        debugVelocityViewerVariable = transform.TransformDirection(localV);
        rb.velocity = transform.TransformDirection(localV);
    }
    private float getSpeed()
    {
        Quaternion angle90Degrees = Quaternion.Euler(90, rot.y, rot.z);
        Quaternion angleMinus90Degrees = Quaternion.Euler(-90, rot.y, rot.z);

        debugDragTestPlus90 = Quaternion.Dot(Quaternion.Euler(rot), angle90Degrees);
        debugDragTestMinus90 = Quaternion.Dot(Quaternion.Euler(rot), angleMinus90Degrees);

        //Dot product
        //1 if same
        //0 of 90 degrees diference
        //-1 if 180 degrees of difference
        //going up
        float speed = 0f;
        float distancePlusTo1 = Mathf.Abs(1 - Mathf.Abs(debugDragTestPlus90));
        float distanceMinusTo1 = Mathf.Abs(1 - Mathf.Abs(debugDragTestMinus90));
        if (distancePlusTo1 < distanceMinusTo1)
        {
            speed = Mathf.Lerp(speedFastAngle, speedSlowAngle, distancePlusTo1);
        }
        //going down
        else
        {
            speed = Mathf.Lerp(speedSlowAngle, speedFastAngle, distanceMinusTo1);
        }
        return speed+ flapCurrentSpeed;
    }
    private float getDrag()
    {

        
        Quaternion var90 = Quaternion.Euler(90, rot.y, rot.z);
        Quaternion varMinus90 = Quaternion.Euler(-90, rot.y, rot.z); //rot z y removed here

        debugDragTestPlus90 = Quaternion.Dot(Quaternion.Euler(rot), var90);
        debugDragTestMinus90 = Quaternion.Dot(Quaternion.Euler(rot), varMinus90);
        float drag = 0f;
        //1 if same
        //0 of 90 degrees diference
        //-1 if 180 degrees of difference
        //going up

        float distancePlusTo1 = Mathf.Abs(1 - Mathf.Abs(debugDragTestPlus90));
        float distanceMinusTo1 = Mathf.Abs(1 - Mathf.Abs(debugDragTestMinus90));
        if (distancePlusTo1 < distanceMinusTo1)
        {
            drag = Mathf.Lerp(dragFastAngleValue, dragSlowAngleValue, distancePlusTo1);
        }
        //going down
        else
        {
            drag = Mathf.Lerp(dragSlowAngleValue , dragFastAngleValue, distanceMinusTo1);
        }
        return drag;
    }


    //ROTATE player
    //x
    private void rotatePlayer()
    {
        //-1 is here for to invert rotation controlls
        rot.x = Mathf.Repeat(rot.x + speedRotation * (Input.GetAxis("Vertical") * -1) * Time.deltaTime, 360f);
        rot.y = Mathf.Repeat(rot.y + speedRotation * Input.GetAxis("Horizontal") * Time.deltaTime, 360f);

        transform.rotation = Quaternion.Euler(rot);

    }
    /*
* Nova nova dot
-90:
-0.9999999 testMinus90 
0 testPlus90
89.83
testMinus 0.001501903
testPlus 0.9999988
0
testMinus 0.7086074
testPlus 0.7056029
-45.269
testMinus --0.9247741
testPlus -0.3805166
45.565
testMinus 0.3781216
testPLus 0.9257559

-135.039(225)
testMinus -0.9237481
testPlus 0.3830005

+135(-225,-225.453)
testMinus -0.3790267
testPlus 0.9253857*/
}
