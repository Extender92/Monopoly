using Monopoly.Core.Models.FortuneCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Events
{
    internal class DrawCommunityChestCardArgs : EventArgs
    {
        public ICommunityChestCard CommunityChestCard { get; }

        public DrawCommunityChestCardArgs(ICommunityChestCard communityChestCard)
        {
            CommunityChestCard = communityChestCard;
        }
    }
}
