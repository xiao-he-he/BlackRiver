using UnityEngine;
using UnityEngine.Events;

public class WindSailController : MonoBehaviour
{
    public enum SailDirection { None, Left, Down, Right, Up }

    public GameObject leftSail;
    public GameObject downSail;
    public GameObject rightSail;
    public GameObject upSail;
    public WindPushZone leftWind;
    public WindPushZone downWind;
    public WindPushZone rightWind;
    public WindPushZone upWind;
    public UnityEvent onDirectionChanged = new UnityEvent();

    public SailDirection CurrentDirection { get; private set; } = SailDirection.None;

    private void Awake()
    {
        InitializeDirection();
    }

    private void InitializeDirection()
    {
        if (CurrentDirection != SailDirection.None)
            return;
        // Read the authored visual BEFORE the first interaction, not a fixed Left.
        SailDirection initial = upSail != null && upSail.activeSelf ? SailDirection.Up :
            leftSail != null && leftSail.activeSelf ? SailDirection.Left :
            downSail != null && downSail.activeSelf ? SailDirection.Down :
            rightSail != null && rightSail.activeSelf ? SailDirection.Right : SailDirection.Left;
        ApplyDirection(initial);
    }

    public void RotateCounterClockwise()
    {
        InitializeDirection();
        // Up -> Left -> Down -> Right -> Up (positive 90 degrees in world XY).
        SetDirection(CurrentDirection == SailDirection.Up
            ? SailDirection.Left : (SailDirection)((int)CurrentDirection + 1));
    }

    public void RotateClockwise()
    {
        InitializeDirection();
        SailDirection nextDirection = (SailDirection)((int)CurrentDirection - 1 <= 0
                ? 4
                : (int)CurrentDirection - 1);
        SetDirection(nextDirection);
    }

    public void SetDirection(SailDirection direction)
    {
        ApplyDirection(direction);
        onDirectionChanged.Invoke();
    }

    private void ApplyDirection(SailDirection direction)
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
