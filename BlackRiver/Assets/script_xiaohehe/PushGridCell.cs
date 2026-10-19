using System.Collections.Generic;
using UnityEngine;

public class PushGridCell : MonoBehaviour
{
    public float gridSize = 1f;
    public bool canBoxMoveHere = true;
    public PushBox occupiedBox;
    public Vector2 centerOffset;

    public Vector2 Position => transform.TransformPoint(centerOffset);

    public static readonly HashSet<PushGridCell> Cells = new HashSet<PushGridCell>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCells()
    {
        Cells.Clear();
    }

    private void OnEnable()
    {
        Cells.Add(this);
    }

    private void OnDisable()
    {
        Cells.Remove(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PushBox box = other.GetComponentInParent<PushBox>();
        if (box != null)
        {
            box.EnterCell(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PushBox box = other.GetComponentInParent<PushBox>();
        if (box != null)
        {
            box.ExitCell(this);
        }
    }

    public static PushGridCell GetCell(Vector2 position, float size)
    {
        if (size <= 0f)
        {
            return null;
        }

        PushGridCell nearestCell = null;
        float nearestDistance = float.PositiveInfinity;
        float tolerance = size * 0.45f;

        foreach (PushGridCell cell in Cells)
        {
            if (cell == null || !cell.isActiveAndEnabled ||
                !Mathf.Approximately(cell.gridSize, size))
            {
                continue;
            }

            Vector2 offset = cell.Position - position;
            if (Mathf.Abs(offset.x) <= tolerance &&
                Mathf.Abs(offset.y) <= tolerance &&
                offset.sqrMagnitude < nearestDistance)
            {
                nearestDistance = offset.sqrMagnitude;
                nearestCell = cell;
            }
        }

        return nearestCell;
    }
}
