namespace Monopoly.Core.Models
{
    public interface IDie
    {
        int GetDieResult();
        int GetDieType();
        void Roll();
        void ScrambleDie();
    }
}
