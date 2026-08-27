namespace Monopoly.Core.Models
{
    public interface IDieView
    {
        int GetDieResult();
        int GetDieType();
    }

    public interface IDie : IDieView
    {
        void Roll();
        void ScrambleDie();
    }
}
