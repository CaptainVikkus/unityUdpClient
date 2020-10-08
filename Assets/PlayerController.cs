using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 3;
    public NetworkMan networkMan;

    // Start is called before the first frame update
    void Start()
    {
        networkMan.localPlayer = gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        float xDirection = 0;
        float zDirection = 0;
        if (Input.GetAxis("Horizontal") > 0.1f)
        {
            xDirection = speed;
        }
        if (Input.GetAxis("Vertical") > 0.1f)
        {
            zDirection = speed;
        }

        transform.Translate(new Vector3(xDirection, 0, zDirection));
    }
}
