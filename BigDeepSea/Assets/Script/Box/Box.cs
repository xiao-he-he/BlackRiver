using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Box : MonoBehaviour
{
    public LayerMask boxLayer;
    public LayerMask wallLayer;
    public Sprite normalSprite;
    public Sprite onTargetSprite;

    private SpriteRenderer sr;
    private  Rigidbody2D rigidbody2D;
    private bool isOnTarget = false;

    public static List<Box> allBoxes = new List<Box>();
    public static event System.Action OnAllBoxesOnTarget;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        allBoxes.Add(this);
    }

    //void OnDestroy()
    //{
    //    allBoxes.Remove(this);
    //}
    private float checkDistance = 0.5f;
    public bool CanPush(Vector2 direction)//"(0.00, -1.00)"
    {
        this.rigidbody2D.linearVelocity = Vector2.zero;
        Vector2 currentPos = transform.position;//"(-0.51, -0.55, 0.00)"
        RaycastHit2D wallHit = Physics2D.Raycast(currentPos, direction, 1f, wallLayer);//512
        RaycastHit2D boxHit = Physics2D.Raycast(currentPos, direction, 1f, boxLayer);//1024//检测到自己身上的碰撞体"供奉箱子 (UnityEngine.BoxCollider2D)"

        // Check for a interactable.
        LayerMask mask = LayerMask.GetMask("Player");

        // Check if a interactable is hit.
        var hitx = Physics2D.Raycast(transform.position, Vector2.right, checkDistance, mask);//"(-1.50, -0.31, 0.00)","(1.00, 0.00)"
        var hity = Physics2D.Raycast(transform.position, Vector2.up, checkDistance, mask);
        var hit_x = Physics2D.Raycast(transform.position, Vector2.left, checkDistance, mask);
        var hit_y = Physics2D.Raycast(transform.position, Vector2.down, checkDistance, mask);
        Debug.Log("box hitx" + hitx.collider);
        Debug.Log("box hity" + hity.collider);
        Debug.Log("box hit_x" + hit_x.collider);//??普通箱子自己的碰撞体忘记打钩触发体了
        Debug.Log("box hit_y " + hit_y.collider);
        return wallHit.collider == null && (boxHit.collider == null || boxHit.collider == transform.GetComponent<BoxCollider2D>()) &&
            (hitx.collider != null||hit_x.collider != null|| hit_y.collider != null|| hity.collider != null);

        //return wallHit.collider == null && 
        //      (boxHit.collider == null||boxHit.collider!=transform.GetComponent<BoxCollider2D>())&&
        //      (hitx.collider!=null|| hitx.collider !=transform.GetComponent<BoxCollider2D>())||
        //      (hity.collider != null|| hity.collider != transform.GetComponent<BoxCollider2D>())||
        //      ( hit_x.collider != null|| hit_x.collider != transform.GetComponent<BoxCollider2D>())||
        //      (hit_y.collider != null||hit_y.collider != transform.GetComponent<BoxCollider2D>());
    }

    public void PushTo(Vector2 position)
    {
        rigidbody2D.MovePosition(position);
    }

    public void OnPushComplete()
    {
        this.rigidbody2D.linearVelocity = Vector2.zero;
        CheckTarget();
        CheckWinCondition();
    }

    void CheckTarget()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);//"普通箱子 (UnityEngine.Transform)""(-0.51, -1.48, 0.00)"
        bool wasOnTarget = isOnTarget;
        isOnTarget = false;

        foreach (Collider2D hit in hits)//石砖 普通箱子 Bounds "Traget2 供奉箱 
        {
            //是target 0 触碰到 target2 而不是 target1 触碰到target 2
            if (hit.CompareTag("Target2") )
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    // 先检查是否为 null，再检查名称
                    if (hits[i] != null && hits[i].name == "供奉箱子")
                    {
                        isOnTarget = true;
                        wasOnTarget=isOnTarget;
                        Debug.Log("游戏胜利");
                        break; // 找到第一个后立即退出循环，提高效率
                    }
                }
            
            }

        }
        

        if (isOnTarget != wasOnTarget)//Ture！=false
        {
            sr.sprite = isOnTarget ? onTargetSprite : normalSprite;//供奉箱:普通箱子
            Debug.Log("老大，你放错了箱子了，游戏失败");
        }
    }

    void CheckWinCondition()
    {
        foreach (Box box in allBoxes)//"供奉箱子 (Box)""普通箱子 (Box)
        {
            if (box.isOnTarget)
            {
                OnAllBoxesOnTarget?.Invoke();
                return;
            }
        }

        
    }

    public bool IsOnTarget()
    {
        return isOnTarget;//false
    }

    private void OnDrawGizmos()
    {
        Vector2 currentPos = transform.position;//"(0.06, 0.00, -10.00)"
        var currentPosX = new Vector2(currentPos.x, 0);
        var currentPosY = new Vector2(0, currentPos.y);
        LayerMask mask = LayerMask.GetMask("Player");
        var hitx = Physics2D.Raycast(transform.position, Vector2.right, checkDistance, mask);//"(-1.50, -0.31, 0.00)","(1.00, 0.00)"
        var hity = Physics2D.Raycast(transform.position, Vector2.up, checkDistance, mask);
        var hit_x = Physics2D.Raycast(transform.position, Vector2.left, checkDistance, mask);
        var hit_y = Physics2D.Raycast(transform.position, Vector2.down, checkDistance, mask);
        // 2. 设置 Gizmos 颜色为红色
        Gizmos.color = Color.red;

        // 3.计算方向向量
        // 从玩家指向箱子的向量
        Vector3 directionVectorX = currentPosX - hitx.point;
        Vector3 directionVectorY = currentPosY - hity.point;
        Vector3 directionVector_X = currentPosX - hit_x.point;
        Vector3 directionVector_Y = currentPosY - hit_y.point;

        // Debug.Log("currentPosX: " + currentPosX + "hitX.point: " + hitX.point);
        // Debug.Log("currentPosX: " + currentPosY + "hitX.point: " + hitY.point);

        // 关键步骤：归一化方向向量
        // 如果直接使用未归一化的向量，DrawRay 的长度会受到距离影响
        // normalized 确保向量长度为 1，代表纯方向
        Vector3 normalizedDirectionX = directionVectorX.normalized;
        Vector3 normalizedDirectionY = directionVectorY.normalized;
        Vector3 normalizedDirection_X = directionVector_X.normalized;
        Vector3 normalizedDirection_Y = directionVector_Y.normalized;
        // 4.绘制射线
        // 参数1: 起点 (玩家位置)
        // 参数2: 方向 * 长度 (归一化方向 * 1)

        Gizmos.DrawRay(currentPos, -normalizedDirectionX * 1/2f);
        Gizmos.DrawRay(currentPos, -normalizedDirectionY * 1/2f);
        Gizmos.DrawRay(currentPos, normalizedDirection_X * 1/2f);
        Gizmos.DrawRay(currentPos, normalizedDirection_Y * 1/2f);

    }
}
