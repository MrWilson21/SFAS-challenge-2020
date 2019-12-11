﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform cameraStart;
    [SerializeField] private Transform cameraPlay;
    private bool isPLaying;

    [SerializeField] private float mainSpeed = 100.0f; //regular max speed
    [SerializeField] private float maxShift = 1000.0f; //Maximum speed when holding shift

    [SerializeField] private float camSens = 0.25f; //How sensitive it with mouse
    [SerializeField] private float zoomSensScroll;
    [SerializeField] private float zoomSensMouse;
    [SerializeField] private float zoomAccelerationFactor;
    [SerializeField] private float zoomDecelerationLinear;
    [SerializeField] private float zoomDecelerationStatic;
    [SerializeField] private float startZoomLevel;
   
    [SerializeField] private AnimationCurve xAngleCurve; //To determine the x angle depending on zoom level
    [SerializeField] private AnimationCurve heightCurve; //To determine the height of the camera depending on zoom level;

    private Vector3 lastMouse = Vector3.zero;

    private float zoomLevel;
    private float zoomAcceleration;
    private float zoomSpeed;

    // Start is called before the first frame update
    void Start()
    {
        resetPosition(cameraStart);
        isPLaying = false;
    }

    void Update()
    {
        if(isPLaying)
        {
            doZoom();
            doRotate();
            doTranslate();

            lastMouse = Input.mousePosition;
        }
    }

    void doZoom()
    {
        //Increment acceleration by mouse scoll amount and by mouse movement
        zoomAcceleration += Input.mouseScrollDelta.y * zoomSensScroll;
        if (Input.GetMouseButton(1))
        {
            zoomAcceleration += (Input.mousePosition - lastMouse).y * zoomSensMouse;
        }
        //Increment speed by time amount of acceleration and decrement acceleration by the same amount
        zoomSpeed += Mathf.Clamp(Time.unscaledDeltaTime * zoomAccelerationFactor, 0, Mathf.Abs(zoomAcceleration)) * Mathf.Sign(zoomAcceleration);
        zoomAcceleration -= Mathf.Clamp(Time.unscaledDeltaTime * zoomAccelerationFactor, 0, Mathf.Abs(zoomAcceleration)) * Mathf.Sign(zoomAcceleration);
        //Decrement speed gradually
        if (zoomSpeed > 0)
        {
            zoomSpeed -= zoomDecelerationLinear * (zoomSpeed + zoomDecelerationStatic) * Time.unscaledDeltaTime;
            zoomSpeed = Mathf.Clamp(zoomSpeed, 0, float.MaxValue);
        }
        else if (zoomSpeed < 0)
        {
            zoomSpeed += zoomDecelerationLinear * (-zoomSpeed + zoomDecelerationStatic) * Time.unscaledDeltaTime;
            zoomSpeed = Mathf.Clamp(zoomSpeed, float.MinValue, 0);
        }

        //Increment zoom level by speed amount
        zoomLevel += zoomSpeed * Time.unscaledDeltaTime;
        zoomLevel = Mathf.Clamp(zoomLevel, 0, 1);
    }

    void doRotate()
    {
        float xAngle = xAngleCurve.Evaluate(zoomLevel);

        float yAngle = transform.eulerAngles.y;
        if (Input.GetMouseButton(1))
        {
            lastMouse = Input.mousePosition - lastMouse;
            yAngle = lastMouse.x * camSens;
            yAngle = transform.eulerAngles.y + yAngle;
        }

        transform.eulerAngles = new Vector3(xAngle, yAngle);
    }

    void doTranslate()
    {
        Vector3 oldPosition = transform.position;
        //Move on the local x and z axis
        transform.Translate(GetBaseInput(), Space.Self);

        //Seperate movement vector and 
        Vector3 movement = transform.position - oldPosition;
        //Remove the global y component of movement
        movement = new Vector3(movement.x, 0, movement.z);
        //Normalise movement and multiply by speed
        movement.Normalize();
        if (Input.GetKey(KeyCode.LeftShift))
        {
            movement *= maxShift * Time.unscaledDeltaTime;
        }
        else
        {
            movement *= mainSpeed * Time.unscaledDeltaTime;
        }       

        //Set height of old position and then set transform back to old position
        oldPosition.y = heightCurve.Evaluate(zoomLevel);
        transform.position = oldPosition;
        //Translate by new movement vector on the z/x plain only
        transform.Translate(movement, Space.World);
    }

    private Vector3 GetBaseInput()
    { //returns the basic values, if it's 0 then it's not active.
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey(KeyCode.W))
        {
            p_Velocity += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            p_Velocity += new Vector3(1, 0, 0);
        }
        return p_Velocity;
    }

    public void resetPosition(Transform position)
    {
        transform.position = position.position;
        transform.rotation = position.rotation;
    }

    public void startGame()
    {
        isPLaying = true;
        zoomLevel = startZoomLevel;
        resetPosition(cameraPlay);
    }

    public void endGame()
    {
        isPLaying = false;
        resetPosition(cameraStart);
    }
}