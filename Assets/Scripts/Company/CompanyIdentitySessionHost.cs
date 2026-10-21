using System;
using LitMotion;
using UnityEngine;

namespace BattleRoyale.Company
{
    public sealed class CompanyIdentitySessionHost : MonoBehaviour
    {
        [SerializeField] CompanyIdentity.CompanyIdentityEditorViewBindings editorBindings;
        [SerializeField] CompanyIdentity.CompanyIdentitySummaryViewBindings summaryBindings;
        [SerializeField] CompanyIdentity.CompanyIdentityPreviewViewBindings editorPreviewBindings;
        [SerializeField] CompanyIdentity.CompanyIdentityPreviewViewBindings summaryPreviewBindings;
        [SerializeField] CompanyIdentity.CompanyIdentityPreviewViewBindings dashboardPreviewBindings;
        [SerializeField] CompanySessionController company;

        CompanyIdentity.CompanyIdentityEditorView editor;
        CompanyIdentity.CompanyIdentitySummaryView summary;
        CompanyIdentity.CompanyIdentityPreviewView editorPreview;
        CompanyIdentity.CompanyIdentityPreviewView summaryPreview;
        CompanyIdentity.CompanyIdentityPreviewView dashboardPreview;
        CompanyIdentity.CompanyIdentityData identity;
        [SerializeField, Min(0f)] float identitySaveDebounceSeconds = 0.15f;
        MotionHandle identitySaveMotion;
        bool identitySavePending;
        bool ready;

        public void Show(CompanyIdentity.CompanyIdentityData next)
        {
            if (!Ensure())
            {
                return;
            }

            identity = next;
            if (summary != null)
            {
                summary.Refresh(identity);
            }

            editorPreview.Render(identity);
            if (summaryPreview != null)
            {
                summaryPreview.Render(identity);
            }

            if (dashboardPreview != null)
            {
                dashboardPreview.Render(identity);
            }

            editor.Refresh(identity, editorPreviewBindings.ShapeSprites);
        }

        bool Ensure()
        {
            if (ready)
            {
                return true;
            }

            if (editorBindings == null || editorPreviewBindings == null || company == null)
            {
                Debug.LogError("Company identity needs its editor and preview.", this);
                return false;
            }

            editorPreview = new CompanyIdentity.CompanyIdentityPreviewView();
            editorPreview.Initialize(editorPreviewBindings);
            if (summaryPreviewBindings != null)
            {
                summaryPreview = new CompanyIdentity.CompanyIdentityPreviewView();
                summaryPreview.Initialize(summaryPreviewBindings);
            }

            if (dashboardPreviewBindings != null)
            {
                dashboardPreview = new CompanyIdentity.CompanyIdentityPreviewView();
                dashboardPreview.Initialize(dashboardPreviewBindings);
            }

            editor = new CompanyIdentity.CompanyIdentityEditorView();
            editor.Initialize(
                editorBindings,
                OnOption,
                OnColor,
                OnBodyStep,
                OnEmblemEdited,
                OnDoneRequested);
            if (summaryBindings != null)
            {
                summary = new CompanyIdentity.CompanyIdentitySummaryView();
                summary.Initialize(summaryBindings, summaryPreview, company.ShowEditor, null);
                if (summaryBindings.ResetButton != null)
                {
                    summaryBindings.ResetButton.gameObject.SetActive(false);
                }
            }

            ready = true;
            return true;
        }

        void OnDisable()
        {
            FlushPendingIdentitySave();
        }

        public void OpenEditor()
        {
            if (identity == null)
            {
                return;
            }

            Show(identity);
        }

        void OnOption(CompanyIdentity.CompanyIdentityTab tab, int option)
        {
            if (identity == null)
            {
                return;
            }

            switch (tab)
            {
                case CompanyIdentity.CompanyIdentityTab.Insignia:
                    if (option >= 0 && option < CompanyIdentity.CompanyIdentityRules.InsigniaShapes.Length)
                    {
                        identity.insigniaShape = CompanyIdentity.CompanyIdentityRules.InsigniaShapes[option];
                    }

                    break;
                case CompanyIdentity.CompanyIdentityTab.Icon:
                    if (option >= 0 && option < CompanyIdentity.CompanyIdentityRules.Icons.Length)
                    {
                        identity.icon = CompanyIdentity.CompanyIdentityRules.Icons[option];
                    }

                    break;
                case CompanyIdentity.CompanyIdentityTab.Flag:
                    if (option >= 0 && option < CompanyIdentity.CompanyIdentityRules.FlagPatterns.Length)
                    {
                        identity.flagPattern = CompanyIdentity.CompanyIdentityRules.FlagPatterns[option];
                    }

                    break;
            }

            Show(identity);
            SaveIdentityImmediately();
        }

        void OnBodyStep(int direction)
        {
            if (identity == null ||
                editorPreviewBindings == null ||
                editorPreviewBindings.BodyPairCount <= 0)
            {
                return;
            }

            identity.bodyIndex = CompanyIdentity.CompanyIdentityRules.StepBodyIndex(
                identity.bodyIndex,
                direction,
                editorPreviewBindings.BodyPairCount);
            Show(identity);
            SaveIdentityImmediately();
        }

        void OnColor(CompanyIdentity.CompanyIdentityColorSlot slot, int paletteIndex)
        {
            if (identity == null)
            {
                return;
            }

            var palette = CompanyIdentity.CompanyIdentityRules.Palette;
            if (paletteIndex < 0 || paletteIndex >= palette.Length)
            {
                return;
            }

            identity.SetColor(slot, palette[paletteIndex]);
            Show(identity);
            SaveIdentityImmediately();
        }

        void OnEmblemEdited()
        {
            if (identity == null)
            {
                return;
            }

            Show(identity);
            QueueIdentitySave();
        }

        void OnDoneRequested()
        {
            FlushPendingIdentitySave();
            if (company != null)
            {
                company.ShowDashboard();
            }
        }

        void SaveIdentityImmediately()
        {
            FlushPendingIdentitySave();
            if (identity != null && company != null)
            {
                company.SaveIdentity(identity);
            }
        }

        void QueueIdentitySave()
        {
            if (identity == null || company == null)
            {
                return;
            }

            identitySavePending = true;
            if (identitySaveMotion.IsActive())
            {
                identitySaveMotion.Cancel();
            }

            identitySaveMotion = default;
            if (identitySaveDebounceSeconds <= 0f)
            {
                CommitPendingIdentitySave();
                return;
            }

            identitySaveMotion = LMotion.Create(0f, 1f, identitySaveDebounceSeconds)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(CommitPendingIdentitySave)
                .RunWithoutBinding();
        }

        void CommitPendingIdentitySave()
        {
            identitySaveMotion = default;
            if (!identitySavePending)
            {
                return;
            }

            identitySavePending = false;
            if (identity != null && company != null)
            {
                company.SaveIdentity(identity);
            }
        }

        void FlushPendingIdentitySave()
        {
            if (identitySaveMotion.IsActive())
            {
                identitySaveMotion.Cancel();
            }

            identitySaveMotion = default;
            if (!identitySavePending)
            {
                return;
            }

            identitySavePending = false;
            if (identity != null && company != null)
            {
                company.SaveIdentity(identity);
            }
        }
    }
}
