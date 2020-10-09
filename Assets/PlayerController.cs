using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 1;
    public NetworkMan networkMan;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.GetComponent<IDScript>().address == networkMan.localID)
        {
            float xDirection = 0;
            float zDirection = 0;
            xDirection = Input.GetAxis("Horizontal") * speed;
            zDirection = Input.GetAxis("Vertical") * speed;
            transform.position = new Vector3(xDirection, 0, zDirection);
        }

        if (Input.GetKey(KeyCode.Escape)) { Application.Quit(); }
    }
}
