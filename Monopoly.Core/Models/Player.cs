using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core.Models
{
    public class Player
    {
        private readonly Dictionary<ResourceId, int> _resources = [];
        private readonly ReadOnlyDictionary<ResourceId, int> _resourcesView;

        public Player(string name, int id)
        {
            Id = id >= 0 ? id : throw new ArgumentOutOfRangeException(nameof(id));
            Name = !string.IsNullOrWhiteSpace(name)
                ? name
                : throw new ArgumentException("Player name cannot be empty.", nameof(name));
            _resourcesView = new ReadOnlyDictionary<ResourceId, int>(_resources);
        }

        public int Id { get; }
        public string Name { get; }
        public int Money { get; private set; }
        public int Position { get; private set; }
        public SpaceId CurrentSpaceId { get; private set; }
        public IReadOnlyDictionary<ResourceId, int> Resources => _resourcesView;
        internal int NumberOfGetOutOFJailCards { get; private set; }
        public bool IsBankrupt { get; private set; }

        internal void InitializeProfileState(IEnumerable<ResourceAmount> resources, SpaceId spaceId, int position)
        {
            ArgumentNullException.ThrowIfNull(resources);
            if (!spaceId.IsValid) throw new ArgumentException("The current space ID is invalid.", nameof(spaceId));
            if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));

            ResourceAmount[] supplied = resources.ToArray();
            if (supplied.Any(resource => !resource.IsValid) ||
                supplied.Select(resource => resource.ResourceId).Distinct().Count() != supplied.Length)
            {
                throw new ArgumentException("Profile resources must contain unique valid resource amounts.", nameof(resources));
            }

            _resources.Clear();
            foreach (ResourceAmount resource in supplied.OrderBy(resource => resource.ResourceId))
                _resources.Add(resource.ResourceId, resource.Value);
            MoveTo(position, spaceId);
        }

        internal void CreditResource(ResourceId resourceId, int amount)
        {
            if (!resourceId.IsValid) throw new ArgumentException("The resource ID is invalid.", nameof(resourceId));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!_resources.TryGetValue(resourceId, out int current))
                throw new KeyNotFoundException($"Resource '{resourceId}' is not defined for this player.");
            _resources[resourceId] = checked(current + amount);
        }

        internal bool TryDebitResource(ResourceId resourceId, int amount)
        {
            if (!resourceId.IsValid) throw new ArgumentException("The resource ID is invalid.", nameof(resourceId));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!_resources.TryGetValue(resourceId, out int current))
                throw new KeyNotFoundException($"Resource '{resourceId}' is not defined for this player.");
            if (current < amount) return false;
            _resources[resourceId] = current - amount;
            return true;
        }

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

        internal void MoveTo(int position, SpaceId spaceId)
        {
            if (!spaceId.IsValid) throw new ArgumentException("The current space ID is invalid.", nameof(spaceId));
            MoveTo(position);
            CurrentSpaceId = spaceId;
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
