//using UnityEngine;
//using UnityEngine.InputSystem;

//public class PlayerCombat : MonoBehaviour
//{
//    private Animator anim;

//    void Awake()
//    {
//        anim = GetComponent<Animator>();
//    }   
//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {

//    }

//    // Update is called once per frame
//    void Update()
//    {

//    }

//    void OnFire()
//    {
//       anim.SetTrigger("Attack");
//    }


//}
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    private Animator anim;
    private int comboStep = 0;
    private float comboTimer = 0f;
    [SerializeField] private float comboResetTime = 5f; // max delay between hits

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Handle combo timing
        if (comboStep > 0)
        {
            comboTimer += Time.deltaTime;
            if (comboTimer > comboResetTime)
            {
                ResetCombo();
            }
        }
    }

    void OnFire() // new input system passes InputValue
    {


        if (comboStep == 0) // first hit
        {
            comboStep = 1;
            anim.ResetTrigger("Attack"); // reset trigger to allow re-triggering
            anim.SetInteger("ComboStep", comboStep);
            anim.SetTrigger("Attack");
            comboTimer = 0f;
        }
        else if (comboStep == 1) // second hit
        {
            comboStep = 2;
            anim.ResetTrigger("Attack"); // reset trigger to allow re-triggering
            anim.SetInteger("ComboStep", comboStep);
            anim.SetTrigger("Attack");
            comboTimer = 0f;
        }
    }

    // Called at the end of Swing2 animation via Animation Event
    public void ResetCombo()
    {
        comboStep = 0;
        comboTimer = 0f;
        anim.ResetTrigger("Attack");
        anim.SetInteger("ComboStep", 0);
    }
}
