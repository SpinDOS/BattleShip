namespace BattleShip.Shared
{
    public enum SquareStatus : byte
    {
        Empty = 0, //обязательно для инициализации Field
        Miss,
        Full,
        Hurt,
        Dead,
    }
}
