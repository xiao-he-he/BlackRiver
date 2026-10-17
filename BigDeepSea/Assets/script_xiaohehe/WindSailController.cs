using UnityEngine;
using UnityEngine.Events;

public class WindSailController : MonoBehaviour
{
    public enum SailDirection { Left, Down, Right, Up }

    public GameObject leftSail;
    public GameObject downSail;
    public GameObject rightSail;
    public GameObject upSail;
    public WindPushZone leftWind;
    public WindPushZone downWind;
    public WindPushZone rightWind;
    public WindPushZone upWind;
    public UnityEvent onDirectionChanged = new UnityEvent();

    public SailDirection CurrentDirection { get; private set; } = SailDirection.Left;

    private void Start()
    {
        SetDirection(SailDirection.Left);
    }

    public void RotateClockwise()
    {
        SetDirection((SailDirection)(((int)CurrentDirection + 1) % 4));
    }

    public void SetDirection(SailDirection direction)
    {
        CurrentDirection = direction;
        SetSail(leftSail, direction == SailDirection.Left);
        SetSail(downSail, direction == SailDirection.Down);
        SetSail(rightSail, direction == SailDirection.Right);
        SetSail(upSail, direction == SailDirection.Up);
        SetWind(leftWind, Vector2.left, direction == SailDirection.Left);
        SetWind(downWind, Vector2.down, direction == SailDirection.Down);
        SetWind(rightWind, Vector2.right, direction == SailDirection.Right);
        SetWind(upWind, Vector2.up, direction == SailDirection.Up);
        Physics2D.SyncTransforms();
        onDirectionChanged.Invoke();
    }

    private void SetSail(GameObject sail, bool active)
    {
        if (sail != null)
        {
            sail.SetActive(active);
        }
    }

    private void SetWind(WindPushZone wind, Vector2 direction, bool active)
    {
        if (wind != null)
        {
            wind.direction = direction;
            wind.gameObject.SetActive(active);
        }
    }
}
