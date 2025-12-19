using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System;
using System.Collections;

public static class ResizePopup
{
    public static void Initialize(GameManager gameManager)
    {
        if (gameManager.gameObject.GetComponent<PopupMonitor>() == null)
        {
            gameManager.gameObject.AddComponent<PopupMonitor>();
        }
    }

    private class SmallPopup : MonoBehaviour
    {
        private Vector2 originalPosition;
        private bool hasStoredOriginalPos = false;

        void Start()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                originalPosition = rt.anchoredPosition;
                hasStoredOriginalPos = true;
            }
        }

        void Update()
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);

            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null && hasStoredOriginalPos)
            {
                rt.anchoredPosition = new Vector2(originalPosition.x + 200f, originalPosition.y);
            }
        }
    }

    private class PopupMonitor : MonoBehaviour
    {
        private FieldInfo upgradePopupsField;
        private IList upgradePopupsList;
        private int previousCount = 0;

        void Start()
        {

            upgradePopupsField = AccessTools.Field(typeof(GameManager), "upgradePopups");
            if (upgradePopupsField == null)
            {
                return;
            }

            upgradePopupsList = upgradePopupsField.GetValue(GameManager.Instance) as IList;
            if (upgradePopupsList == null)
            {
                return;
            }

            previousCount = upgradePopupsList.Count;

            StartCoroutine(MonitorListCoroutine());
        }

        private IEnumerator MonitorListCoroutine()
        {
            while (true)
            {
                if (upgradePopupsList != null)
                {
                    int currentCount = upgradePopupsList.Count;
                    if (currentCount > previousCount)
                    {

                        var newPopup = upgradePopupsList[currentCount - 1];
                        if (newPopup != null)
                        {
                            var go = (newPopup as MonoBehaviour)?.gameObject;
                            if (go != null && go.GetComponent<SmallPopup>() == null)
                            {
                                go.AddComponent<SmallPopup>();
                            }
                        }

                        previousCount = currentCount;
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
