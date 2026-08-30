using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core.Models
{
    public class Player(string name, int id)
    {
        public int Id { get; } = id >= 0 ? id : throw new ArgumentOutOfRangeException(nameof(id));
        public string Name { get; } = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new ArgumentException("Player name cannot be empty.", nameof(name));
        public int Money { get; private set; }
        public int Position { get; private set; }
        internal int NumberOfGetOutOFJailCards { get; private set; }
        public bool IsBankrupt { get; private set; }

        internal void Credit(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Money = checked(Money + amount);
        }

        internal bool TryDebit(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Money < amount) return false;
            Money -= amount;
            return true;
        }

        internal int TakeAllMoney()
        {
            int money = Money;
            Money = 0;
            return money;
        }

        internal void MoveTo(int position)
        {
            if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
            Position = position;
        }

        internal void AddJailCards(int count = 1)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            NumberOfGetOutOFJailCards = checked(NumberOfGetOutOFJailCards + count);
        }

        internal bool TryUseJailCard()
        {
            if (NumberOfGetOutOFJailCards == 0) return false;
            NumberOfGetOutOFJailCards--;
            return true;
        }

        internal int TakeAllJailCards()
        {
            int cards = NumberOfGetOutOFJailCards;
            NumberOfGetOutOFJailCards = 0;
            return cards;
        }

        internal void MarkBankrupt()
        {
            if (Money != 0 || NumberOfGetOutOFJailCards != 0)
                throw new InvalidOperationException("A player must surrender money and jail cards before bankruptcy.");
            IsBankrupt = true;
        }

        internal void RestoreState(int money, int position, int jailCards, bool isBankrupt)
        {
            if (money < 0) throw new ArgumentOutOfRangeException(nameof(money));
            if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
            if (jailCards < 0) throw new ArgumentOutOfRangeException(nameof(jailCards));
            if (isBankrupt && (money != 0 || jailCards != 0))
                throw new ArgumentException("A bankrupt player cannot retain money or jail cards.", nameof(isBankrupt));

            Money = money;
            Position = position;
            NumberOfGetOutOFJailCards = jailCards;
            IsBankrupt = isBankrupt;
        }
    }
}
