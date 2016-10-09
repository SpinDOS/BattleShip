namespace BattleShip.Shared
{
    public enum SquareStatus : byte
    {
        Empty = 0, //обязательно для инициализации Field
        Miss = 1,
        Full = 2,
        Hurt = 3,
        Dead = 4,
    }
}
