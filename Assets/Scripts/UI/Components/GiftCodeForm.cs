using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeagullStorm
{
    /// <summary>
    /// Gift code input + validate/redeem.
    /// </summary>
    public class GiftCodeForm : MonoBehaviour
    {
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private Button redeemButton;
        [SerializeField] private TMP_Text statusText;

        private void OnEnable()
        {
            if (redeemButton != null) redeemButton.onClick.AddListener(OnRedeem);
        }

        private void OnDisable()
        {
            if (redeemButton != null) redeemButton.onClick.RemoveListener(OnRedeem);
        }

        private async void OnRedeem()
        {
            if (HorizonManager.Instance?.Online != true) { SetStatus("Sign in to redeem online gifts."); return; }
            string code = codeInput != null ? codeInput.text.Trim() : "";
            if (string.IsNullOrWhiteSpace(code))
            {
                SetStatus("Enter a code.");
                return;
            }

            var save = GameManager.Instance?.Save;
            if (save != null && save.giftCodesRedeemed.Contains(code))
            {
                SetStatus("Already redeemed!");
                return;
            }

            SetStatus("Validating...");

            bool? valid = await HorizonManager.Instance.ValidateGiftCode(code);
            if (valid != true)
            {
                SetStatus("Invalid code.");
                return;
            }

            var result = await HorizonManager.Instance.RedeemGiftCode(code);
            if (result != null)
            {
                save?.giftCodesRedeemed.Add(code);

                if (!string.IsNullOrEmpty(result.giftData))
                {
                    try
                    {
                        var reward = JsonUtility.FromJson<GiftReward>(result.giftData);
                        if (reward != null && save != null)
                            save.coins += Mathf.Max(0, reward.coins);
                    }
                    catch (System.Exception ex) { HorizonManager.Instance?.RecordException(ex); }
                }

                GameManager.Instance?.NotifyProgressChanged();
                if (save != null) await HorizonManager.Instance.SaveCloudData(JsonUtility.ToJson(save));
                SetStatus("Redeemed!");
            }
            else
            {
                SetStatus("Redemption failed.");
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }

        [Serializable]
        private class GiftReward
        {
            public int coins;
        }
    }
}
