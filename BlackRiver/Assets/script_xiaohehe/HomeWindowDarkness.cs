using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeWindowDarkness : MonoBehaviour
{
    public Collider2D interactionTrigger;
    public KeyCode interactionKey = KeyCode.E;
    public GameObject closedCurtain;
    public GameObject grandmotherToHide;
    public Transform player;
    public Transform fireplace;
    public Camera sceneCamera;
    public Shader darknessShader;
    [Header("黑幕透光设置")]
    [Tooltip("角色周围的透光半径，数值越大，角色周围能看见的范围越大。")]
    [Min(0.01f)] public float playerLightRadius = 1.5f;

    [Tooltip("角色光圈内黑色滤镜的强度，0 为不压暗，1 为完全黑暗。")]
    [Range(0f, 1f)] public float playerDarkness = 0.35f;

    [Tooltip("烟囱火微光的影响范围。")]
    [Min(0.01f)] public float fireplaceLightRadius = 0.85f;

    [Tooltip("烟囱火在黑幕下的可见程度，0 为不可见，1 为完全透光。")]
    [Range(0f, 1f)] public float fireplaceVisibility = 0.4f;

    [Tooltip("透光区域边缘的柔和程度。")]
    [Min(0.001f)] public float edgeSoftness = 0.15f;

    public bool IsClosed { get; private set; }

    private readonly HashSet<Collider2D> playerContacts = new HashSet<Collider2D>();
    private GameObject overlay;
    private Material darknessMaterial;

    private void Update()
    {
        if (Time.timeScale > 0f && Input.GetKeyDown(interactionKey))
        {
            TryInteract();
        }
    }

    public bool TryInteract()
    {
        if (!isActiveAndEnabled || IsClosed || interactionTrigger == null || !interactionTrigger.enabled ||
            !interactionTrigger.isTrigger || closedCurtain == null || darknessShader == null ||
            player == null || fireplace == null || sceneCamera == null)
        {
            return false;
        }

        foreach (Collider2D contact in playerContacts)
        {
            if (contact != null && contact.enabled && contact.gameObject.activeInHierarchy &&
                interactionTrigger.IsTouching(contact))
            {
                closedCurtain.SetActive(true);
                CreateOverlay();
                IsClosed = true;
                if (grandmotherToHide != null)
                    grandmotherToHide.SetActive(false);
                return true;
            }
        }
        return false;
    }

    private void CreateOverlay()
    {
        overlay = new GameObject("窗帘关闭黑幕", typeof(Canvas));
        overlay.transform.SetParent(transform, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Darken the room, not the dialogue/CG/settings that must remain readable and usable.
        canvas.sortingOrder = ScenePauseMenu.DialogueSortingOrder - 2;

        GameObject panel = new GameObject("玩家与篝火透光区域", typeof(RectTransform), typeof(RawImage));
        panel.transform.SetParent(overlay.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        darknessMaterial = new Material(darknessShader);
        RawImage image = panel.GetComponent<RawImage>();
        image.material = darknessMaterial;
        image.raycastTarget = false;
        UpdateHoles();
    }

    private void LateUpdate()
    {
        if (IsClosed && overlay != null)
        {
            UpdateHoles();
        }
    }

    private void UpdateHoles()
    {
        if (darknessMaterial == null || sceneCamera == null)
        {
            return;
        }
        darknessMaterial.SetVector("_PlayerHole", GetHole(player, playerLightRadius));
        darknessMaterial.SetFloat("_PlayerDarkness", playerDarkness);
        darknessMaterial.SetVector("_FireplaceHole", GetHole(fireplace, fireplaceLightRadius));
        darknessMaterial.SetFloat("_FireplaceVisibility", fireplaceVisibility);
    }

    private Vector4 GetHole(Transform source, float radius)
    {
        if (source == null)
        {
            return new Vector4(-100000f, -100000f, 0f, 1f);
        }
        Vector3 center = sceneCamera.WorldToScreenPoint(source.position);
        if (center.z <= 0f)
        {
            return new Vector4(-100000f, -100000f, 0f, 1f);
        }
        float pixelsPerUnit = Vector3.Distance(
            sceneCamera.WorldToScreenPoint(source.position + sceneCamera.transform.up), center);
        return new Vector4(center.x, center.y, radius * pixelsPerUnit,
            Mathf.Max(0.001f, edgeSoftness * pixelsPerUnit));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrackPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TrackPlayer(other);
    }

    private void TrackPlayer(Collider2D other)
    {
        move controller = other.GetComponentInParent<move>();
        if (controller != null && interactionTrigger != null && interactionTrigger.IsTouching(other))
        {
            playerContacts.Add(other);
            if (player == null)
            {
                player = controller.transform;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        playerContacts.Remove(other);
    }

    private void OnDisable()
    {
        playerContacts.Clear();
        if (overlay != null)
        {
            overlay.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (overlay != null)
        {
            overlay.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (darknessMaterial != null)
        {
            Destroy(darknessMaterial);
        }
        if (overlay != null)
        {
            Destroy(overlay);
        }
    }
}
