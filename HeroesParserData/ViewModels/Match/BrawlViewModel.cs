﻿using GalaSoft.MvvmLight.Messaging;
using Heroes.ReplayParser;
using HeroesParserData.DataQueries;
using HeroesParserData.Messages;
using System.Collections.ObjectModel;

namespace HeroesParserData.ViewModels.Match
{
    public class BrawlViewModel : MatchOverviewContext
    {
        public BrawlViewModel()
            :base()
        {

        }

        protected override void ExecuteLoadMatchListCommmand()
        {
            MatchListCollection = new ObservableCollection<Models.DbModels.Replay>(Query.Replay.ReadGameModeRecords(GameMode.Brawl, this));
            RowsReturned = MatchListCollection.Count;
        }

        protected override void ExecuteShowMatchSummaryCommand()
        {
            if (SelectedReplay == null)
                return;

            Messenger.Default.Send(new MatchSummaryMessage { ReplayId = SelectedReplay.ReplayId, MatchSummary = MatchSummary.Brawl });
        }
    }
}