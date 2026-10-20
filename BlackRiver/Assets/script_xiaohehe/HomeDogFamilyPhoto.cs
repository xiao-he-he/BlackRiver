using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class HomeDogFamilyPhoto : MonoBehaviour
{
    public HomeWindowDarkness window;
    public CircleCollider2D interactionTrigger;
    public move player;
    public Sprite familyPhoto;
    public next sceneTransition;
    public HomeStorySequence story;
    [Min(0f)] public float photoDuration = 3f;

    public bool IsShowingPhoto { get; private set; }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) &&
            (Time.timeScale > 0f || (story != null && story.HasDogBubble)))
            TryInteract();
    }

    public bool TryInteract()
    {
        if (story != null)
        {
            if ((Time.timeScale <= 0f && !story.HasDogBubble) ||
                !player || !interactionTrigger || !interactionTrigger.enabled)
                return false;
            var collider = player.GetComponent<Collider2D>();
            if (!collider || !collider.enabled || interactionTrigger.Distance(collider).distance > 0f)
                return false;
            return story.OnDogInteract();
        }
        if (IsShowingPhoto || !window || !window.IsClosed || !player ||
            !interactionTrigger || !interactionTrigger.enabled || !familyPhoto ||
            !sceneTransition)
            return false;

        var playerCollider = player.GetComponent<Collider2D>();
        if (!playerCollider || !playerCollider.enabled ||
            interactionTrigger.Distance(playerCollider).distance > 0f)
            return false;

        IsShowingPhoto = true;
        StartCoroutine(ShowPhoto());
        return true;
    }

    private IEnumerator ShowPhoto()
    {
        player.enabled = false;
        var body = player.GetComponent<Rigidbody2D>();
        if (body)
            body.linearVelocity = Vector2.zero;

        var screen = new GameObject("全家福CG", typeof(Canvas), typeof(GraphicRaycaster));
        screen.transform.SetParent(transform, false);
        var canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 1;

        var background = new GameObject("背景", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(screen.transform, false);
        Stretch((RectTransform)background.transform);
        background.GetComponent<Image>().color = Color.black;

        var image = new GameObject("全家福", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(screen.transform, false);
        Stretch((RectTransform)image.transform);
        var graphic = image.GetComponent<Image>();
        graphic.sprite = familyPhoto;
        graphic.preserveAspect = true;

        yield return new WaitForSecondsRealtime(photoDuration);
        sceneTransition.Unlock();
        sceneTransition.TryTransition();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
