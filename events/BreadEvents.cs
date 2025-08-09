using System;


public class BreadEvents
{
    public event Action<int> onBreadGained;
    public void BreadGained(int breadAmount)
    {
        if (onBreadGained != null)
        {
            onBreadGained(breadAmount);
        }
    }

    public event Action<int> onBreadChange;
    public void BreadChange(int breadAmount)
    {
        if (onBreadChange != null)
        {
            onBreadChange(breadAmount);
        }
    }
    public event Action onBreadCollected;
    public void BreadCollected()
    {
        if (onBreadCollected != null)
        {
            onBreadCollected();
        }
    }
}
