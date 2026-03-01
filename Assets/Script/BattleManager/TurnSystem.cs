public enum TurnOwner { Player, Enemy }

public class TurnSystem
{
    public TurnOwner Current { get; private set; }

    public void SetFirst(TurnOwner first) => Current = first;

    public void NextTurn()
    {
        Current = (Current == TurnOwner.Player) ? TurnOwner.Enemy : TurnOwner.Player;
    }
}
