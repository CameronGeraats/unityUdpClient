using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    public float speed = 2.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 translation = new Vector3();
        translation.x = Input.GetAxis("Horizontal") * speed;
        translation.z = Input.GetAxis("Vertical") * speed;
        transform.Translate(translation * Time.deltaTime);
    }
}
