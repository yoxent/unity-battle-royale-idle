using System.Collections.Generic;
using BattleRoyale.GameModes;
using TMPro;
using UnityEngine;

namespace BattleRoyale.GameModes.BattleRoyale
{
    public sealed class BattleRoyaleStatTracker : StatSheetView
    {
        [SerializeField] BattleRoyaleGameMode match;
        [SerializeField] GameObject board;
        [SerializeField] TMP_Text aliveCount;
        [SerializeField] RectTransform rows;
        [SerializeField] BattleRoyaleStatRow rowPrefab;

        readonly List<BattleRoyaleStatRow> pool = new();

        bool configured;

        public override bool Matches(SimulationMatch candidate)
        {
            return candidate is BattleRoyaleGameMode;
        }

        public override SimulationMatch BoundMatch => match;

        public override void Bind(SimulationMatch candidate)
        {
            if (candidate is not BattleRoyaleGameMode battleRoyale)
            {
                return;
            }

            match = battleRoyale;
            if (isActiveAndEnabled)
            {
                Begin();
            }
        }

        void OnEnable()
        {
            Begin();
        }

        void Begin()
        {
            if (match == null)
            {
                configured = false;
                return;
            }

            configured = match != null &&
                         board != null &&
                         aliveCount != null &&
                         rows != null &&
                         rowPrefab != null;
            if (!configured)
            {
                Debug.LogError(
                    "Battle Royale stat tracker is missing a reference.",
                    this);
            }
        }

        void Update()
        {
            if (!Application.isPlaying || !configured)
            {
                return;
            }

            if (!match.IsMatchStarted)
            {
                board.SetActive(false);
                return;
            }

            board.SetActive(true);
            aliveCount.text = match.AliveCount + " alive";
            var order = match.RosterOrder;
            var count = order == null ? 0 : order.Count;
            for (var index = 0; index < count; index++)
            {
                var entry = order.EntryAt(index);
                var row = Slot(index);
                row.gameObject.SetActive(true);
                row.transform.SetSiblingIndex(index);
                row.Show(
                    entry.DisplayName,
                    match.TeamColorAt(entry.TeamIndex),
                    entry.Alive);
            }

            for (var index = count; index < pool.Count; index++)
            {
                if (pool[index] != null)
                {
                    pool[index].gameObject.SetActive(false);
                }
            }
        }

        BattleRoyaleStatRow Slot(int index)
        {
            while (pool.Count <= index)
            {
                pool.Add(Instantiate(rowPrefab, rows));
            }

            return pool[index];
        }
    }
}
