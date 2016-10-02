﻿using HeroesParserData.DataQueries.ReplayData;
using HeroesParserData.Models.DbModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace HeroesParserData.ViewModels.Data
{
    public class RawMatchTeamObjectiveViewModel : RawDataContext
    {
        private ObservableCollection<ReplayMatchTeamObjective> _replayMatchTeamObjective = new ObservableCollection<ReplayMatchTeamObjective>();

        public ObservableCollection<ReplayMatchTeamObjective> ReplayMatchTeamObjective
        {
            get
            {
                return _replayMatchTeamObjective;
            }
            set
            {
                _replayMatchTeamObjective = value;
                RaisePropertyChangedEvent(nameof(ReplayMatchTeamObjective));
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RawMatchTeamObjectiveViewModel()
            : base()
        {
            AddListColumnNames();
        }

        private void AddListColumnNames()
        {
            ReplayMatchTeamObjective r = new ReplayMatchTeamObjective();

            foreach (var prop in r.GetType().GetMethods())
            {
                if (prop.IsVirtual == false && prop.ReturnType.Name == "Void")
                {
                    string columnName = prop.Name.Split('_')[1];
                    if (!columnName.Contains("Ticks"))
                        ColumnNames.Add(columnName);
                }
            }
        }

        protected override async Task ReadDataTop()
        {
            ReplayMatchTeamObjective = new ObservableCollection<ReplayMatchTeamObjective>(await Query.MatchTeamObjective.ReadTopRecordsAsync(100));
            RowsReturned = ReplayMatchTeamObjective.Count;
        }

        protected override async Task ReadDataLast()
        {
            ReplayMatchTeamObjective = new ObservableCollection<ReplayMatchTeamObjective>(await Query.MatchTeamObjective.ReadLastRecordsAsync(100));
            RowsReturned = ReplayMatchTeamObjective.Count;
        }

        protected override async Task ReadDataCustomTop()
        {
            ReplayMatchTeamObjective = new ObservableCollection<ReplayMatchTeamObjective>(await Query.MatchTeamObjective.ReadRecordsCustomTopAsync(SelectedNumber, SelectedTopColumnName, SelectedTopOrderBy));
            RowsReturned = ReplayMatchTeamObjective.Count;
        }

        protected override async Task ReadDataWhere()
        {
            ReplayMatchTeamObjective = new ObservableCollection<ReplayMatchTeamObjective>(await Query.MatchTeamObjective.ReadRecordsWhereAsync(SelectedWhereColumnName, SelectedOperand, TextBoxSelectWhere));
            RowsReturned = ReplayMatchTeamObjective.Count;
        }
    }
}
