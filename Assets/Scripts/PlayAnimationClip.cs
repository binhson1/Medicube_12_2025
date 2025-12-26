using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAnimationClip : MonoBehaviour
{
    public Animator animator;
    public GameObject targetObject;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (targetObject.activeInHierarchy)
        {
            animator.Play("WinScreen");
        }
        else
        {
            animator.Play("Idle");
        }
    }
}
