using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeagullStorm
{
    /// <summary>First click prepares and saves; the next explicit click confirms the same V2 original.</summary>
    public class GiftCodeForm : MonoBehaviour
    {
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private Button redeemButton;
        [SerializeField] private TMP_Text statusText;
        private void OnEnable() { if (redeemButton != null) redeemButton.onClick.AddListener(OnRedeem); }
        private void OnDisable() { if (redeemButton != null) redeemButton.onClick.RemoveListener(OnRedeem); }
        private void Update()
        {
            var h = HorizonManager.Instance;
            if (redeemButton == null) return;
            redeemButton.interactable = h != null && h.Online && !h.GiftInFlight;
            var label = redeemButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = h != null && h.ReadPendingGift(out var original) && original != null ? "Confirm / retry saved gift" : "Prepare gift";
        }
        private async void OnRedeem()
        {
            var h = HorizonManager.Instance;
            if (h?.Online != true || h.GiftInFlight) { SetStatus("Sign in to use online gifts."); return; }
            if (!h.ReadPendingGift(out var original)) { SetStatus("Original storage needs recovery. No replacement will be created."); return; }
            try
            {
                if (original == null)
                {
                    var intent = await h.PrepareGiftCode(codeInput != null ? codeInput.text.Trim() : "");
                    if (this == null) return;
                    SetStatus(intent == null ? "Prepare: " + h.LastGiftError : "Saved " + intent.code + " / expected " + intent.expectedCount + ", revision " + intent.configRevision + ". Click Confirm explicitly.");
                }
                else
                {
                    var result = await h.RedeemPendingGift();
                    if (this == null) return;
                    SetStatus(result == null ? "Original retained: " + h.LastGiftError : "Server confirmed count " + result.count + (result.alreadyApplied ? " (already applied)." : "."));
                }
            }
            catch (Exception) { if (this != null) SetStatus("Request uncertain; keep the saved original."); }
        }
        private void SetStatus(string message) { if (statusText != null) statusText.text = message; }
    }
}
