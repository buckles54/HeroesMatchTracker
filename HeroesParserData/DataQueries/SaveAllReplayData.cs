﻿using Heroes.ReplayParser;
using HeroesParserData.DataQueries.ReplayData;
using HeroesParserData.Models.DbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using static Heroes.ReplayParser.DataParser;

namespace HeroesParserData.DataQueries
{
    public class SaveAllReplayData : IDisposable
    {
        private Heroes.ReplayParser.Replay Replay;
        private HeroesParserDataContext HeroesParserDataContext;
        private long ReplayId;
        private string FileName;
        private DateTime ParsedDateTime;

        public SaveAllReplayData(Heroes.ReplayParser.Replay replay, string fileName)
        {
            Replay = replay;
            FileName = fileName;
            HeroesParserDataContext = new HeroesParserDataContext();
        }

        public ReplayParseResult SaveAllData(out DateTime parsedDateTime)
        {
            using (HeroesParserDataContext)
            {
                using (var dbTransaction = HeroesParserDataContext.Database.BeginTransaction())
                {
                    try
                    {
                        ReplayId = SaveBasicData();
                        if (ReplayId > 0)
                        {
                            SavePlayerRelatedData();
                            SaveMatchTeamBans();
                            SaveMatchTeamLevels();
                            SaveMatchTeamExperience();
                            SaveMatchMessage();
                            SaveMatchObjectives();

                            dbTransaction.Commit();

                            parsedDateTime = ParsedDateTime;

                            return ReplayParseResult.Saved;
                        }
                        else
                        {
                            parsedDateTime = new DateTime();
                            return ReplayParseResult.Duplicate;
                        }

                    }
                    catch (Exception)
                    {
                        dbTransaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Adds replay data to database, returns the id of replay.  Will return 0 if replay already added
        /// </summary>
        /// <returns>Id of replay, 0 if already added</returns>
        private long SaveBasicData()
        {
            Models.DbModels.Replay replayData = new Models.DbModels.Replay
            {
                Frames = Replay.Frames,
                GameMode = Replay.GameMode,
                GameSpeed = Replay.GameSpeed.ToString(),
                IsGameEventsParsed = Replay.IsGameEventsParsedSuccessfully,
                MapName = Replay.Map,
                RandomValue = Replay.RandomValue,
                ReplayBuild = Replay.ReplayBuild,
                ReplayLength = Replay.ReplayLength,
                ReplayVersion = Replay.ReplayVersion,
                TeamSize = Replay.TeamSize,
                TimeStamp = Replay.Timestamp,
                FileName = FileName
            };

            ParsedDateTime = Replay.Timestamp;

            // check if replay was added to database already
            if (Query.Replay.IsExistingReplay(replayData, HeroesParserDataContext))
            {
                return 0;
            }

            return Query.Replay.CreateRecord(HeroesParserDataContext, replayData);
        }

        private void SavePlayerRelatedData()
        {
            int i = 0;

            Player[] players;

            if (Replay.ReplayBuild > 39445)
                players = Replay.ClientListByUserID;
            else
                players = Replay.Players;

            foreach (var player in players)
            {
                if (player == null)
                    break;

                ReplayAllHotsPlayer hotsPlayer = new ReplayAllHotsPlayer
                {
                    BattleTagName = Utilities.GetBattleTagName(player.Name, player.BattleTag),
                    BattleNetId = player.BattleNetId,
                    BattleNetRegionId = player.BattleNetRegionId,
                    BattleNetSubId = player.BattleNetSubId,
                    LastSeen = Replay.Timestamp,
                    Seen = 1
                };

                long playerId;

                if (Query.HotsPlayer.IsExistingHotsPlayer(HeroesParserDataContext, hotsPlayer))
                    playerId = Query.HotsPlayer.UpdateRecord(HeroesParserDataContext, hotsPlayer);
                else
                    playerId = Query.HotsPlayer.CreateRecord(HeroesParserDataContext, hotsPlayer);

                if (player.Character == null && Replay.GameMode == GameMode.Custom)
                {
                    player.Team = 4;
                    player.Character = "None";
                    SaveMatchPlayers(playerId, -1, player);
                    SavePlayersHeroes(playerId, player);
                }
                else
                {
                    SaveMatchPlayers(playerId, i, player);
                    SaveMatchPlayerScoreResults(playerId, player);
                    SaveMatchPlayerTalents(playerId, player);
                    SavePlayersHeroes(playerId, player);

                    i++;
                }
            }
        }

        private void SaveMatchPlayers(long playerId, int playerNumber, Player player)
        {
            ReplayMatchPlayer replayPlayer = new ReplayMatchPlayer
            {
                ReplayId = ReplayId,
                PlayerId = playerId,
                Character = player.Character,
                CharacterLevel = player.CharacterLevel,
                Difficulty = player.Difficulty.ToString(),
                Handicap = player.Handicap,
                IsAutoSelect = player.IsAutoSelect,
                IsSilenced = player.IsSilenced,
                IsWinner = player.IsWinner,
                MountAndMountTint = player.MountAndMountTint,
                PlayerNumber = playerNumber,
                SkinAndSkinTint = player.SkinAndSkinTint,
                Team = player.Team
            };

            Query.MatchPlayer.CreateRecord(HeroesParserDataContext, replayPlayer);
        }

        private void SaveMatchPlayerScoreResults(long playerId, Player player)
        {
            ScoreResult sr = player.ScoreResult;

            ReplayMatchPlayerScoreResult playerScore = new ReplayMatchPlayerScoreResult
            {
                ReplayId = ReplayId,
                Assists = sr.Assists,
                PlayerId = playerId,
                CreepDamage = sr.CreepDamage,
                DamageTaken = sr.DamageTaken,
                Deaths = sr.Deaths,
                ExperienceContribution = sr.ExperienceContribution,
                Healing = sr.Healing,
                HeroDamage = sr.HeroDamage,
                MercCampCaptures = sr.MercCampCaptures,
                MetaExperience = sr.MetaExperience,
                MinionDamage = sr.MinionDamage,
                SelfHealing = sr.SelfHealing,
                SiegeDamage = sr.SiegeDamage,
                SoloKills = sr.SoloKills,
                StructureDamage = sr.StructureDamage,
                SummonDamage = sr.SummonDamage,
                TakeDowns = sr.Takedowns,
                TimeCCdEnemyHeroes = sr.TimeCCdEnemyHeroes.HasValue ? sr.TimeCCdEnemyHeroes.Value.Ticks : (long?)null,
                TimeSpentDead = sr.TimeSpentDead,
                TownKills = sr.TownKills,
                WatchTowerCaptures = sr.WatchTowerCaptures
            };

            Query.MatchPlayerScoreResult.CreateRecord(HeroesParserDataContext, playerScore);
        }

        private void SaveMatchPlayerTalents(long playerId, Player player)
        {
            Talent[] talents = player.Talents;
            Talent[] talentArray = new Talent[7]; // hold all 7 talents

            // add known talents
            for (int i = 0; i < talents.Count(); i++)
            {
                talentArray[i] = new Talent();
                talentArray[i].TalentID = talents[i].TalentID;
                talentArray[i].TalentName = talents[i].TalentName;
                talentArray[i].TimeSpanSelected = talents[i].TimeSpanSelected;
            }
            // make the rest null
            for (int i = talents.Count(); i < 7; i++)
            {
                talentArray[i] = new Talent();
                talentArray[i].TalentID = null;
                talentArray[i].TalentName = null;
                talentArray[i].TimeSpanSelected = null;
            }

            ReplayMatchPlayerTalent replayTalent = new ReplayMatchPlayerTalent
            {
                ReplayId = ReplayId,
                PlayerId = playerId,
                Character = player.Character,
                TalentId1 = talentArray[0].TalentID,
                TalentName1 = talentArray[0].TalentName,
                TimeSpanSelected1 = talentArray[0].TimeSpanSelected,
                TalentId4 = talentArray[1].TalentID,
                TalentName4 = talentArray[1].TalentName,
                TimeSpanSelected4 = talentArray[1].TimeSpanSelected,
                TalentId7 = talentArray[2].TalentID,
                TalentName7 = talentArray[2].TalentName,
                TimeSpanSelected7 = talentArray[2].TimeSpanSelected,
                TalentId10 = talentArray[3].TalentID,
                TalentName10 = talentArray[3].TalentName,
                TimeSpanSelected10 = talentArray[3].TimeSpanSelected,
                TalentId13 = talentArray[4].TalentID,
                TalentName13 = talentArray[4].TalentName,
                TimeSpanSelected13 = talentArray[4].TimeSpanSelected,
                TalentId16 = talentArray[5].TalentID,
                TalentName16 = talentArray[5].TalentName,
                TimeSpanSelected16 = talentArray[5].TimeSpanSelected,
                TalentId20 = talentArray[6].TalentID,
                TalentName20 = talentArray[6].TalentName,
                TimeSpanSelected20 = talentArray[6].TimeSpanSelected,
            };

            Query.MatchPlayerTalent.CreateRecord(HeroesParserDataContext, replayTalent);
        }

        private void SavePlayersHeroes(long playerId, Player player)
        {
            var playersHeroes = player.SkinsDictionary;
            var heroesInfo = App.HeroesInfo;

            var listOfHeroes = Query.HotsPlayerHero.ReadListOfHeroRecordsForPlayerId(HeroesParserDataContext, playerId);

            if (listOfHeroes.Count < 1)
            {
                foreach (var hero in playersHeroes)
                {
                    if (heroesInfo.HeroExists(hero.Key, false))
                    {
                        ReplayAllHotsPlayerHero playersHero = new ReplayAllHotsPlayerHero
                        {
                            PlayerId = playerId,
                            ReplayId = ReplayId,
                            HeroName = hero.Key,
                            IsUsable = hero.Value,
                            LastUpdated = Replay.Timestamp
                        };

                        Query.HotsPlayerHero.CreateRecord(HeroesParserDataContext, playersHero);
                    }
                }
            }
            else
            {
                foreach (var hero in playersHeroes)
                {
                    if (heroesInfo.HeroExists(hero.Key, false))
                    {
                        ReplayAllHotsPlayerHero playersHero = new ReplayAllHotsPlayerHero
                        {
                            PlayerId = playerId,
                            ReplayId = ReplayId,
                            HeroName = hero.Key,
                            IsUsable = hero.Value,
                            LastUpdated = Replay.Timestamp
                        };

                        Query.HotsPlayerHero.UpdateRecord(HeroesParserDataContext, playersHero);
                    }
                }
            }
        }

        private void SaveMatchTeamBans()
        {
            if (Replay.GameMode == GameMode.UnrankedDraft || Replay.GameMode == GameMode.HeroLeague || Replay.GameMode == GameMode.TeamLeague || Replay.GameMode == GameMode.Custom)
            {
                if (Replay.TeamHeroBans != null)
                {
                    ReplayMatchTeamBan replayTeamBan = new ReplayMatchTeamBan
                    {
                        ReplayId = ReplayId,
                        Team0Ban0 = Replay.TeamHeroBans[0][0],
                        Team0Ban1 = Replay.TeamHeroBans[0][1],
                        Team1Ban0 = Replay.TeamHeroBans[1][0],
                        Team1Ban1 = Replay.TeamHeroBans[1][1]
                    };

                    if (replayTeamBan.Team0Ban0 != null || replayTeamBan.Team0Ban1 != null || replayTeamBan.Team1Ban0 != null || replayTeamBan.Team1Ban1 != null)
                        Query.MatchTeamBan.CreateRecord(HeroesParserDataContext, replayTeamBan);
                }
            }
        }

        private void SaveMatchTeamLevels()
        {
            Dictionary<int, TimeSpan?> team0 = Replay.TeamLevels[0];
            Dictionary<int, TimeSpan?> team1 = Replay.TeamLevels[1];

            if (team0 != null || team1 != null)
            {
                int levelDiff = team0.Count - team1.Count;
                if (levelDiff > 0)
                {
                    for (int i = team1.Count + 1; i <= team0.Count; i++)
                    {
                        team1.Add(i, null);
                    }
                }
                else if (levelDiff < 0)
                {
                    for (int i = team0.Count + 1; i <= team1.Count; i++)
                    {
                        team0.Add(i, null);
                    }
                }

                for (int level = 1; level <= team0.Count; level++)
                {
                    ReplayMatchTeamLevel replayTeamLevel = new ReplayMatchTeamLevel
                    {
                        ReplayId = ReplayId,
                        TeamTime0 = team0[level],
                        Team0Level = team0[level].HasValue ? level : (int?)null,

                        TeamTime1 = team1[level],
                        Team1Level = team1[level].HasValue ? level : (int?)null,

                    };

                    Query.MatchTeamLevel.CreateRecord(HeroesParserDataContext, replayTeamLevel);
                }
            }
        }

        private void SaveMatchTeamExperience()
        {
            var xpTeam0 = Replay.TeamPeriodicXPBreakdown[0];
            var xpTeam1 = Replay.TeamPeriodicXPBreakdown[1];

            if (xpTeam0 == null && xpTeam1 == null)
                return;

            if (xpTeam0.Count != xpTeam1.Count)
            {
                throw new Exception("Teams don't have equal periodic xp gain.");
            }

            for (int i = 0; i < xpTeam0.Count; i++)
            {
                var x = xpTeam0[i];
                var y = xpTeam1[i];

                ReplayMatchTeamExperience xp = new ReplayMatchTeamExperience
                {
                    ReplayId = ReplayId,
                    Time = x.TimeSpan,

                    Team0TeamLevel = x.TeamLevel,
                    Team0CreepXP = x.CreepXP,
                    Team0HeroXP = x.HeroXP,
                    Team0MinionXP = x.MinionXP,
                    Team0StructureXP = x.StructureXP,
                    Team0TrickleXP = x.TrickleXP,

                    Team1TeamLevel = y.TeamLevel,
                    Team1CreepXP = y.CreepXP,
                    Team1HeroXP = y.HeroXP,
                    Team1MinionXP = y.MinionXP,
                    Team1StructureXP = y.StructureXP,
                    Team1TrickleXP = y.TrickleXP,
                };

                Query.MatchTeamExperience.CreateRecord(HeroesParserDataContext, xp);
            }
        }

        private void SaveMatchMessage()
        {
            foreach (var message in Replay.Messages)
            {
                var messageEventType = message.MessageEventType;
                var player = message.MessageSender;

                if (messageEventType == ReplayMessageEvents.MessageEventType.SChatMessage)
                {
                    var chatMessage = message.ChatMessage;

                    ReplayMatchMessage chat = new ReplayMatchMessage
                    {
                        ReplayId = ReplayId,
                        CharacterName = player != null ? player.Character : string.Empty,
                        Message = chatMessage.Message,
                        MessageEventType = messageEventType.ToString(),
                        MessageTarget = chatMessage.MessageTarget.ToString(),
                        PlayerName = player != null ? player.Name : string.Empty,
                        TimeStamp = message.Timestamp
                    };

                    Query.MatchMessage.CreateRecord(HeroesParserDataContext, chat);
                }
                else if (messageEventType == ReplayMessageEvents.MessageEventType.SPingMessage)
                {
                    var pingMessage = message.PingMessage;

                    ReplayMatchMessage ping = new ReplayMatchMessage
                    {
                        ReplayId = ReplayId,
                        CharacterName = player != null ? player.Character : string.Empty,
                        Message = "used a ping",
                        MessageEventType = messageEventType.ToString(),
                        MessageTarget = pingMessage.MessageTarget.ToString(),
                        PlayerName = player != null ? player.Name : string.Empty,
                        TimeStamp = message.Timestamp
                    };

                    Query.MatchMessage.CreateRecord(HeroesParserDataContext, ping);
                }

                else if (messageEventType == ReplayMessageEvents.MessageEventType.SPlayerAnnounceMessage)
                {
                    var announceMessage = message.PlayerAnnounceMessage;

                    ReplayMatchMessage announce = new ReplayMatchMessage
                    {
                        ReplayId = ReplayId,
                        CharacterName = player != null ? player.Character : string.Empty,
                        Message = $"announce {announceMessage.AnnouncementType.ToString()}",
                        MessageEventType = messageEventType.ToString(),
                        MessageTarget = "Allies",
                        PlayerName = player != null ? player.Name : string.Empty,
                        TimeStamp = message.Timestamp
                    };

                    Query.MatchMessage.CreateRecord(HeroesParserDataContext, announce);
                }
            }
        }

        private void SaveMatchObjectives()
        {
            var objTeam0 = Replay.TeamObjectives[0];
            var objTeam1 = Replay.TeamObjectives[1];

            if (objTeam0.Count > 0 && objTeam0 != null)
            {
                foreach (var objCount in objTeam0)
                {
                    var player = objCount.Player;

                    ReplayMatchTeamObjective obj = new ReplayMatchTeamObjective
                    {
                        Team = 0,
                        PlayerId = player != null ? Query.HotsPlayer.ReadPlayerIdFromBattleNetId(HeroesParserDataContext, Utilities.GetBattleTagName(player.Name, player.BattleTag), player.BattleNetId) : (long?)null,
                        ReplayId = ReplayId,
                        TeamObjectiveType = objCount.TeamObjectiveType.ToString(),
                        TimeStamp = objCount.TimeSpan,
                        Value = objCount.Value
                    };

                    Query.MatchTeamObjective.CreateRecord(HeroesParserDataContext, obj);
                }
            }

            if (objTeam1.Count > 0 && objTeam1 != null)
            {
                foreach (var objCount in objTeam1)
                {
                    var player = objCount.Player;

                    ReplayMatchTeamObjective obj = new ReplayMatchTeamObjective
                    {
                        Team = 1,
                        PlayerId = player != null ? Query.HotsPlayer.ReadPlayerIdFromBattleNetId(HeroesParserDataContext, Utilities.GetBattleTagName(player.Name, player.BattleTag), player.BattleNetId) : (long?)null,
                        ReplayId = ReplayId,
                        TeamObjectiveType = objCount.TeamObjectiveType.ToString(),
                        TimeStamp = objCount.TimeSpan,
                        Value = objCount.Value
                    };

                    Query.MatchTeamObjective.CreateRecord(HeroesParserDataContext, obj);
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ((IDisposable)HeroesParserDataContext).Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}