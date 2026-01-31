using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VanguardArena.Tests.Utils
{
    /// <summary>
    /// Utilities for testing UI components in PlayMode.
    /// </summary>
    public static class UITestHelper
    {
        /// <summary>
        /// Find UI element by name in hierarchy.
        /// </summary>
        public static GameObject FindUIElement(string name)
        {
            var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            foreach (var obj in allObjects)
            {
                if (obj.name == name)
                {
                    return obj;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Get text from TextMeshProUGUI component (or Text component as fallback).
        /// </summary>
        public static string GetTextMeshProText(GameObject parent, string childName)
        {
            var child = parent.transform.Find(childName);
            if (child == null) return null;
            
            // Try TextMeshProUGUI first (requires TMPro package)
            var tmpComponent = child.GetComponent("TextMeshProUGUI");
            if (tmpComponent != null)
            {
                // Use reflection to get text property
                var textProperty = tmpComponent.GetType().GetProperty("text");
                if (textProperty != null)
                {
                    return textProperty.GetValue(tmpComponent) as string;
                }
            }
            
            // Fallback to legacy Text component
            var textComponent = child.GetComponent<Text>();
            return textComponent != null ? textComponent.text : null;
        }

        /// <summary>
        /// Get slider value.
        /// </summary>
        public static float GetSliderValue(GameObject parent, string sliderName)
        {
            var child = parent.transform.Find(sliderName);
            if (child == null) return -1f;
            
            var slider = child.GetComponent<Slider>();
            return slider != null ? slider.value : -1f;
        }

        /// <summary>
        /// Check if UI element is visible.
        /// </summary>
        public static bool IsUIElementVisible(GameObject uiElement)
        {
            if (uiElement == null) return false;
            
            var canvasGroup = uiElement.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                return canvasGroup.alpha > 0 && canvasGroup.interactable;
            }
            
            return uiElement.activeInHierarchy;
        }

        /// <summary>
        /// Wait for UI element to appear.
        /// </summary>
        public static IEnumerator WaitForUIElement(string elementName, float timeout = 5f)
        {
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                var element = FindUIElement(elementName);
                if (element != null && IsUIElementVisible(element))
                {
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Debug.LogWarning($"[TEST] UI element '{elementName}' did not appear within {timeout}s");
        }
    }
}
