namespace Bag
{
    public interface IItemEffect
    {
        // 钩子：当回合开始时触发
        void OnTurnStart();
    }
}