using System;
using System.Collections;
using UnityEngine;

public class DoorOpen : MonoBehaviour
{
    public bool isOpen { set; get; } = false;
    private GameObject[] doors;
    private void Start()
    {
        doors = GameObject.FindGameObjectsWithTag("Door");
    }
    
    public void Swap()
    {
        if (isOpen) { openDoors(); }
        else { closeDoors(); }
    }
    void openDoors()
    {
        foreach (GameObject door in doors)
        {
            HingeJoint hinge = door.GetComponent<HingeJoint>();
            JointSpring spring = hinge.spring;
            spring.targetPosition = -90;
            hinge.spring = spring;
        }
    }

    public void openDoorsTemp()
    {
        if (!isOpen)
        {
            StartCoroutine(OpenDoors10());
        }
    }

    private IEnumerator OpenDoors10()
    {
        openDoors();
        yield return new WaitForSeconds(10);
        if(!isOpen) closeDoors();
    }

    void closeDoors()
    {
        foreach (GameObject door in doors)
        {
            HingeJoint hinge = door.GetComponent<HingeJoint>();
            JointSpring spring = hinge.spring;
            spring.targetPosition = 0;
            hinge.spring = spring;
        }
    }
}
