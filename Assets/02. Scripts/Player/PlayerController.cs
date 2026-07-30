using UnityEngine;
using static PlayerController;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Vector3 startPoint;

    [System.Serializable]
    public class Form
    {
        public Color formColor;
        public float speed;
        public float jumpPower;
        public bool isUnlock = false;
        public bool isNotDamaged = false;
    }

    int index = 0;
    [SerializeField] Form[] forms;
    Form nowForm;

    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundCheckRadius = 0.3f;
    [SerializeField] float groundCheckOffset = 0.1f;

    Rigidbody rb;
    Renderer cr;
    Vector3 moveInput;
    [SerializeField] float coyoteTime = 0.1f;
    float coyoteTimer;
    [SerializeField] float jumpBuffer = 0.1f;
    float jumpBufferTimer;
    bool isGrounded;


    void Awake()
    {
        nowForm = forms[index];
        rb = GetComponent<Rigidbody>();
        cr = GetComponent<Renderer>();
        cr.material.color = nowForm.formColor;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) TabForm();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0f, v).normalized;
        
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferTimer = jumpBuffer;
        else
            jumpBufferTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        CheckGround();

        Vector3 velocity = moveInput * nowForm.speed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;

        coyoteTimer = isGrounded ? coyoteTime : coyoteTimer - Time.fixedDeltaTime;

        if (0 < jumpBufferTimer && 0 < coyoteTimer)
        {
            Vector3 v = rb.linearVelocity;
            v.y = nowForm.jumpPower;
            rb.linearVelocity = v;

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }

    void CheckGround()
    {
        Vector3 checkPos = transform.position + Vector3.down * groundCheckOffset;
        isGrounded = Physics.CheckSphere(checkPos, groundCheckRadius, groundMask);
    }

    void TabForm()
    {
        do
        {
            index = (index + 1) % forms.Length;
            nowForm = forms[index];
        }
        while (!nowForm.isUnlock);

        cr.material.color = nowForm.formColor;
    }

    public void Damage()
    {
        if(!nowForm.isNotDamaged)
            transform.position = startPoint;
    }

    public void Unlock(int index)
    {
        forms[index].isUnlock = true;
    }
}
