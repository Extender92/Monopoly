using Monopoly.Core.Models.FortuneCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Events
{
    internal class DrawCommunityChestCardArgs
    {
        public ICommunityChestCard CommunityChestCard { get; set; }

        public DrawCommunityChestCardArgs(ICommunityChestCard communityChestCard)
        {
            CommunityChestCard = communityChestCard;
        }
    }
}
