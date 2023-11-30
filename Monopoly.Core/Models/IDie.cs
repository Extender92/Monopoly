namespace Monopoly.Core.Models
{
    internal interface IDie
    {
        int GetDieResult();
        int GetDieType();
        void Roll();
    }
}