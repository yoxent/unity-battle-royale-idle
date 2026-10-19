using System;
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
                company.ShowDashboard);
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
            company.SaveIdentity(identity);
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
            company.SaveIdentity(identity);
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
            company.SaveIdentity(identity);
        }

        void OnEmblemEdited()
        {
            if (identity == null)
            {
                return;
            }

            Show(identity);
            company.SaveIdentity(identity);
        }
    }
}
