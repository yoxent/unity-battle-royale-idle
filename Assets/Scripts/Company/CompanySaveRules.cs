using System;

namespace BattleRoyale.Company
{
    [Serializable]
    public sealed class CompanySaveAgent
    {
        public string ArchetypeId;
        public int Rarity;
        public int[] Rolls;
        public int Waived;
        public string Avatar;
    }

    [Serializable]
    public sealed class CompanySaveCard
    {
        public bool Filled;
        public string ArchetypeId;
        public int Rarity;
        public int[] Rolls;
        public string Avatar;
    }

    [Serializable]
    public sealed class CompanySaveRecord
    {
        public int Version = 1;
        public string CompanyName;
        public int InsigniaShape;
        public int Icon;
        public int FlagPattern;
        public float PrimaryR;
        public float PrimaryG;
        public float PrimaryB;
        public float SecondaryR;
        public float SecondaryG;
        public float SecondaryB;
        public float AccentR;
        public float AccentG;
        public float AccentB;
        public int Balance;
        public int Fans;
        public float DayProgress;
        public int CompanyDate;
        public int RankPoints;
        public int Stance;
        public CompanySaveAgent[] Roster;
        public CompanySaveCard[] Board;
        public float RefreshRemaining;
        public int RefreshRetries;
        public int RefreshRetryDate;
        public int RefreshFreeUsed;
        public int RefreshDay;
        public long TrustedUnix;
        public bool BoardRolled;
        public int TutorialStep;
        public string UtcStamp;
        public bool HasCheckpoint;
    }

    public static class CompanySaveRules
    {
        public const int Version = 1;

        public static bool TryWrite(CompanySaveRecord record, out string json)
        {
            json = null;
            if (!IsCompany(record))
            {
                return false;
            }

            record.Version = Version;
            json = UnityEngine.JsonUtility.ToJson(record);
            return !string.IsNullOrEmpty(json);
        }

        public static bool TryRead(string json, out CompanySaveRecord record)
        {
            record = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            CompanySaveRecord parsed;
            try
            {
                parsed = UnityEngine.JsonUtility.FromJson<CompanySaveRecord>(json);
            }
            catch (Exception)
            {
                return false;
            }

            if (!IsCompany(parsed) || parsed.Version != Version)
            {
                return false;
            }

            record = parsed;
            return true;
        }

        public static bool TrySettleOffline(
            CompanySaveRecord record,
            float elapsedSeconds,
            EconomyTuning tuning,
            out CompanySaveRecord next,
            out int salaryPaid,
            out int fanPaid)
        {
            next = null;
            salaryPaid = 0;
            fanPaid = 0;
            if (!IsCompany(record) || elapsedSeconds < 0f)
            {
                return false;
            }

            var income = new IdleIncomeState
            {
                Balance = record.Balance,
                Fans = record.Fans,
                DayProgress = record.DayProgress,
            };
            if (!IdleIncomeRules.TryAdvance(income, tuning, elapsedSeconds, true, out var paid))
            {
                return false;
            }

            next = Copy(record);
            next.Balance = paid.Balance;
            next.Fans = paid.Fans;
            next.DayProgress = paid.DayProgress;
            salaryPaid = paid.SalaryPaid;
            fanPaid = paid.FanPaid;
            return true;
        }

        public static CompanySaveRecord Copy(CompanySaveRecord record)
        {
            var json = UnityEngine.JsonUtility.ToJson(record);
            return UnityEngine.JsonUtility.FromJson<CompanySaveRecord>(json);
        }

        static bool IsCompany(CompanySaveRecord record)
        {
            return record != null &&
                !string.IsNullOrWhiteSpace(record.CompanyName) &&
                record.Roster != null &&
                record.Balance >= 0 &&
                record.Fans >= 0 &&
                record.CompanyDate >= 1;
        }
    }
}
