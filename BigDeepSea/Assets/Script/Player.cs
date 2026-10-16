using System.Collections;
 
using UnityEngine;
using UnityEngine.SceneManagement;


public class Player : MonoBehaviour
{
    private Rigidbody2D rb;

    public float speed;

    private float inputX;

    private float inputY;

    private Vector2 movementInput;
 
    private Animator[] animators;
    public bool isMoving;

    private bool inputDisable;
    private bool useTool;

    //动画使用工具
    private float mouseX;
    private float mouseY;

    [Header("移动设置")]
    public float moveSpeed = 5f;
    public int boxLayerMask=1<<7;




    private Vector2 boxMovement;
     
    private float pushDistance = 0.5f;
    //[SerializeField]
    //private GameObject obj=null;

    private void OnEnable()
    {

        // 注册场景加载完成事件
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventHandler.BeforeSceneUnLoadEvent += OnBeforeSceneUnloadEvent;
        EventHandler.AfterSceneLoadedEvent += OnAfterSceneLoadedEvent;
        EventHandler.MoveToPositionEvent += OnMoveToPositionEvent;
    }

    private void OnDisable()
    {

        // 取消注册，防止内存泄漏
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventHandler.BeforeSceneUnLoadEvent -= OnBeforeSceneUnloadEvent;
        EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvent;
        EventHandler.MoveToPositionEvent -= OnMoveToPositionEvent;
    }

   

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 每次场景加载完成后，重新检查并更新手枪控制状态
        CheckSceneAndSetWeaponState();
    }

    // 标记玩家当前是否可以控制手枪
    private bool canControlPistol = false;

   

    void CheckSceneAndSetWeaponState()
    {
        // 1. 获取当前活动的场景对象
        Scene currentScene = SceneManager.GetActiveScene();

        // 2. 获取场景名称
        string sceneName = currentScene.name;

        // 3. 根据场景名称判断逻辑
        // 规则：
        // - name1: 不控制
        // - name3: 不控制
        // - name2: 控制

        if (sceneName == "Scene0")
        {
            canControlPistol = false;
            
        }
        else if (sceneName == "SceneTwo" || sceneName == "SceneThree")
        {
            canControlPistol = true;
            
        }
        else
        {
            // 可选：处理其他未定义场景的情况，默认禁用或启用
            canControlPistol = false;
             
        }
    }


    private void OnAfterSceneLoadedEvent()
    {
        inputDisable = false;//加载新场景后可以移动
    }

    private void OnBeforeSceneUnloadEvent()
    {
        inputDisable = true;//卸载场景前禁止移动
    }

    private void OnMoveToPositionEvent(Vector3 targetPosition)
    {
        transform.position = targetPosition;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // InventoryManager
     
        animators = GetComponentsInChildren<Animator>();
       
    }
    private void Start()
    {

        CheckSceneAndSetWeaponState();
        //Debug.Log(LayerMask.LayerToName(7));
        //Debug.Log(LayerMask.LayerToName(10));

      
    }

    private void Update()
    {

        CheckSceneAndSetWeaponState();

        if (inputDisable==false)
            PlayerInput();
        else
        {
            isMoving = false;
            this.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }


        SwitchAnimation();

        //箱子的移动方位判定
        if (isMoving)
        {
            boxMovement.x = Input.GetAxisRaw("Horizontal");//-1- 1
            boxMovement.y = Input.GetAxisRaw("Vertical");//-1,1

            if (boxMovement != Vector2.zero)
            {
                if (Mathf.Abs(boxMovement.x) > Mathf.Abs(transform.position.x)) //&&**没有风**)//没有逆风的情况下
                {
                    //正数时，返回 1, 0 retun 0 ,负数时，返回 -1
                    boxMovement = new Vector2( Mathf.Sign(boxMovement.x),0);

                }
                else if(Mathf.Abs(boxMovement.y) > Mathf.Abs(transform.position.y)) //&&**没有风**)
                {
                    boxMovement = new Vector2(0, Mathf.Sign(boxMovement.y));
                }
                else
                {
                    //考虑有风力的情况下
                   // boxMovement = Vector2.zero;

                }


                // Debug.Log("移动方位" + boxMovement);


             if(canControlPistol)
                TryMoveBox();

            }
        }
        else
        {
            boxMovement = Vector2.zero;
            
        }

       

    }
    private void FixedUpdate()
    {
        if (!inputDisable)
            Movement();


        Transform transform =this.transform;//"(-1.36, -0.31, 0.00)"
        if (Input.GetKeyDown(KeyCode.U))
        {
            // Check for a interactable.
            LayerMask mask = LayerMask.GetMask("interactable");

            // Check if a interactable is hit.
            var hitx = Physics2D.Raycast(transform.position, Vector2.right, 4.0f, mask);//"(-1.50, -0.31, 0.00)","(1.00, 0.00)"
            var hity = Physics2D.Raycast(transform.position, Vector2.down, 4.0f, mask);
            var hit_x = Physics2D.Raycast(transform.position, Vector2.left, 4.0f, mask);
            var hit_y = Physics2D.Raycast(transform.position, Vector2.up, 4.0f, mask);
            Debug.Log(hitx.collider+"   "+ hity.collider);
            Debug.Log(hit_x.collider + "   " + hit_y.collider);
        }

         

    }

    private void PlayerInput()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        // print("AD");
        inputY = Input.GetAxisRaw("Vertical");
         // print("WS");
        if (inputX != 0 && inputY != 0)
        {
            inputX = inputX * 0.6f;

            inputY = inputY * 0.6f;

        }
        //走路状态速度按住shift 一直按住速度减半
        if (Input.GetKey(KeyCode.LeftShift))
        {
            inputX = inputX * 0.5f;
            inputY = inputY * 0.5f;
        }
        movementInput = new Vector2(inputX, inputY);
        //不移动（0,0）
        isMoving = movementInput != Vector2.zero;//  

        this.gameObject.transform.rotation = Quaternion.identity;
    }

    private void Movement()
    {
        rb.MovePosition(rb.position + movementInput * speed * Time.deltaTime);
        //this.gameObject.transform.rotation = Quaternion.identity;
    }
    /// <summary>
    /// 人物动画切换
    /// </summary>
    private void SwitchAnimation()
    {
        foreach (var anim in animators)//增加1个手部施法部位的动画循环
        {
            anim.SetBool("isMoving", isMoving);
           // anim.SetFloat("mouseX", mouseX);
            //anim.SetFloat("mouseY", mouseY);

            if (isMoving)
            {
                anim.SetFloat("InputX", inputX);
                anim.SetFloat("InputY", inputY);
            }
        }
    }
    
    void TryMoveBox()
    {
        Vector2 currentPos = transform.position;//"(0.06, 0.00, -10.00)"
        int boxLayerMask = LayerMask.GetMask("interactable");

        var hitx = Physics2D.Raycast(currentPos, Vector2.right, pushDistance, boxLayerMask);//"(-1.50, -0.31, 0.00)","(1.00, 0.00)"
        var hity = Physics2D.Raycast(currentPos, Vector2.up, pushDistance, boxLayerMask);
        var hit_x = Physics2D.Raycast(currentPos, Vector2.left, pushDistance, boxLayerMask);
        var hit_y = Physics2D.Raycast(currentPos, Vector2.down, pushDistance, boxLayerMask);


        var obj1 = FindObjectsOfType<Box>()[1].gameObject;//[0]"供奉箱子 (UnityEngine.GameObject)",[1]"普通箱子 (UnityEngine.GameObject)"
        var obj0 = FindObjectsOfType<Box>()[0].gameObject;
        //供奉箱已经进入目标地点
       
        if (obj0.GetComponent<Box>().IsOnTarget()) 
        {
            GameManager.instance.DisablePanel();
            return; 
        }

            if ((hitx.collider != null && hitx.collider != gameObject.GetComponent<BoxCollider2D>())||
            (hity.collider != null && hity.collider != gameObject.GetComponent<BoxCollider2D>())||
            hit_x.collider != null && hit_x.collider != gameObject.GetComponent<BoxCollider2D>()||
            hit_y.collider != null && hit_y.collider != gameObject.GetComponent<BoxCollider2D>())//排除自身的碰撞体，碰到箱子
        {
             

            if (obj1 != null && obj1.GetComponent<Box>().CanPush(boxMovement))   
            {
                 
                //obj1.GetComponent<Rigidbody2D>().velocity = boxMovement * Time.deltaTime * 0.1f;//"(0.00, -0.02)"--->"(0.00, -1.00)"
                obj1.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                StartCoroutine(MoveWithBox(boxMovement, obj1.GetComponent<Box>()));
            }
            if (obj0 != null && obj0.GetComponent<Box>().CanPush(boxMovement))
            {
                 
                //obj1.GetComponent<Rigidbody2D>().velocity = boxMovement * Time.deltaTime * 0.1f;//"(0.00, -0.02)"--->"(0.00, -1.00)"
                obj0.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                StartCoroutine(MoveWithBox(boxMovement, obj0.GetComponent<Box>()));
            }


        }
        if (this.CompareTag("Target1"))
        {
            //StartCoroutine(MoveWithBox(boxMovement, obj1.GetComponent<Box>()));
            obj1.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        }
        if (this.CompareTag("Target0"))
        {
            //StartCoroutine(MoveWithBox(boxMovement, obj0.GetComponent<Box>()));
            obj0.GetComponent<Rigidbody2D>().linearVelocity =Vector2.zero; ;
        }
    }


    private IEnumerator MoveWithBox(Vector2 direction, Box box)//"(1.00, 0.00)"
    {
        isMoving = true;
        Vector2 playerStart = rb.position;//"(0.26, 0.56)"、、"(-0.48, 0.37)","(0.49, -0.68)"
        Vector2 playerEnd = playerStart + direction;//"(1.26, 0.56)"、、"(-0.48, -0.63)","(0.49, 0.32)"
        Vector2 boxStart = box.transform.position;//"(-0.56, -0.48, 0.00)"、、"(-0.51, -0.48)""(0.54, -0.46)"
        Vector2 boxEnd = boxStart + direction;//"(1.00, 0.00)"、、"(-0.51, -1.48)""(0.54, 0.54)"

        float elapsed = 0f;
         
        while (elapsed < 1f / moveSpeed)//0.2//0.015//0.035//0.036//0.36
        {
            elapsed += Time.deltaTime;
            float t = elapsed * moveSpeed;//0.074723//0.174//1.84//0.079
            rb.MovePosition(Vector2.Lerp(playerStart, playerEnd, t));
            box.PushTo(Vector2.Lerp(boxStart, boxEnd, t));
            yield return null;
        }
        rb.MovePosition(playerEnd);
        box.PushTo(boxEnd);
        Debug.Log("boxStart "+ boxStart + "boxEnd" + boxEnd);
        box.OnPushComplete();

        isMoving = false;
    }

    private IEnumerator Move(Vector2 direction,Box box)
    {
        isMoving = true;
        Vector2 startPos = rb.position;
        Vector2 endPos = startPos + direction;
        float elapsed = 0f;

        while (elapsed < 1f / moveSpeed)
        {
            elapsed += Time.deltaTime;
            rb.MovePosition(Vector2.Lerp(startPos, endPos, elapsed * moveSpeed));
            yield return null;
        }

        rb.MovePosition(endPos);
        isMoving = false;
    }


    private void OnDrawGizmos()
    {
        Vector2 currentPos = transform.position;//"(0.06, 0.00, -10.00)"
       var currentPosX=new Vector2 (currentPos.x,0);
       var  currentPosY= new Vector2(0, currentPos.y);
        //int boxLayerMask = LayerMask.GetMask("interactable");
        RaycastHit2D hitX = Physics2D.Raycast(currentPosX, Vector2.right, pushDistance, boxLayerMask);
        RaycastHit2D hitY = Physics2D.Raycast(currentPosY, Vector2.up, pushDistance, boxLayerMask);
        var hit_x = Physics2D.Raycast(transform.position, Vector2.left, pushDistance, boxLayerMask);
        var hit_y = Physics2D.Raycast(transform.position, Vector2.down, pushDistance, boxLayerMask);
        // 2. 设置 Gizmos 颜色为红色
        Gizmos.color = Color.red;

        // 3.计算方向向量
        // 从玩家指向箱子的向量
        Vector3 directionVectorX = currentPosX - hitX.point;
        Vector3 directionVectorY = currentPosY - hitY.point;
        Vector3 directionVector_X = currentPosX - hit_x.point;
        Vector3 directionVector_Y = currentPosY - hit_y.point;

        // Debug.Log("currentPosX: " + currentPosX + "hitX.point: " + hitX.point);
        // Debug.Log("currentPosX: " + currentPosY + "hitX.point: " + hitY.point);

        // 关键步骤：归一化方向向量
        // 如果直接使用未归一化的向量，DrawRay 的长度会受到距离影响
        // normalized 确保向量长度为 1，代表纯方向
        Vector3 normalizedDirectionX = directionVectorX.normalized;
        Vector3 normalizedDirectionY = directionVectorY.normalized;
        Vector3 normalizedDirection_X = directionVectorX.normalized;
        Vector3 normalizedDirection_Y = directionVectorY.normalized;
        // 4.绘制射线
        // 参数1: 起点 (玩家位置)
        // 参数2: 方向 * 长度 (归一化方向 * 1)

        Gizmos.DrawRay(currentPos, -normalizedDirectionX * 1/2f);
        Gizmos.DrawRay(currentPos, -normalizedDirectionY * 1/2f);
        Gizmos.DrawRay(currentPos, normalizedDirection_X * 1 / 2f);
        Gizmos.DrawRay(currentPos, normalizedDirection_Y * 1 / 2f);


    }

}
