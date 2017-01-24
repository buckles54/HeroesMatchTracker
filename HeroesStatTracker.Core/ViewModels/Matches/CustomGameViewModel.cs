﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Heroes.ReplayParser;

namespace HeroesStatTracker.Core.ViewModels.Matches
{
    public class CustomGameViewModel : MatchesBase
    {
        public CustomGameViewModel()
            : base(GameMode.Custom)
        {
        }
    }
}
