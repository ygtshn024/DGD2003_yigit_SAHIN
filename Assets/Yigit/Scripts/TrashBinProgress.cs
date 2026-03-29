using System;

/// <summary>
/// Cop kutusuna (ThrowTarget) giden fırlatılabilir sayaci. ThrownObjectDestroyOnImpact tetikler.
/// </summary>
public static class TrashBinProgress
{
    public static int HitsToBin { get; private set; }

    public static event Action OnHitTrashBin;

    public static void ResetCounters()
    {
        HitsToBin = 0;
    }

    public static void NotifyHitTrashBin()
    {
        HitsToBin++;
        OnHitTrashBin?.Invoke();
    }
}
