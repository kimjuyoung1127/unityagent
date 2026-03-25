#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;

namespace Unityctl.Plugin.Editor.Utilities
{
    internal static class UiReadUtility
    {
        public static bool IsUiGameObject(UnityEngine.GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            if (!TryGetRootCanvas(gameObject, out _))
            {
                return false;
            }

            return gameObject.GetComponent<UnityEngine.RectTransform>() != null
                || gameObject.GetComponent<UnityEngine.Canvas>() != null;
        }

        public static bool TryGetRootCanvas(UnityEngine.GameObject gameObject, out UnityEngine.Canvas rootCanvas)
        {
            rootCanvas = null;
            if (gameObject == null)
            {
                return false;
            }

            var current = gameObject.transform;
            while (current != null)
            {
                var candidate = current.GetComponent<UnityEngine.Canvas>();
                if (candidate != null)
                {
                    rootCanvas = candidate.rootCanvas ?? candidate;
                    return rootCanvas != null;
                }

                current = current.parent;
            }

            return false;
        }

        public static string GetUiType(UnityEngine.GameObject gameObject)
        {
            if (gameObject.GetComponent<UnityEngine.Canvas>() != null)
                return "Canvas";
            if (gameObject.GetComponent<UnityEngine.UI.Button>() != null)
                return "Button";
            if (gameObject.GetComponent<UnityEngine.UI.InputField>() != null)
                return "InputField";
            if (gameObject.GetComponent<UnityEngine.UI.Toggle>() != null)
                return "Toggle";
            if (gameObject.GetComponent<UnityEngine.UI.Slider>() != null)
                return "Slider";
            if (gameObject.GetComponent<UnityEngine.UI.Dropdown>() != null)
                return "Dropdown";
            if (gameObject.GetComponent<UnityEngine.UI.ScrollRect>() != null)
                return "ScrollView";
            if (gameObject.GetComponent<UnityEngine.UI.Text>() != null)
                return "Text";

            var image = gameObject.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
                return IsPanelLike(gameObject) ? "Panel" : "Image";

            return "RectTransform";
        }

        public static bool IsPanelLike(UnityEngine.GameObject gameObject)
        {
            if (gameObject.GetComponent<UnityEngine.UI.Image>() == null)
                return false;

            return gameObject.GetComponent<UnityEngine.UI.Selectable>() == null
                && gameObject.GetComponent<UnityEngine.UI.Text>() == null
                && gameObject.GetComponent<UnityEngine.UI.InputField>() == null
                && gameObject.GetComponent<UnityEngine.UI.ScrollRect>() == null
                && gameObject.GetComponent<UnityEngine.Canvas>() == null;
        }

        public static bool MatchesUiType(UnityEngine.GameObject gameObject, string requestedType)
        {
            var actualType = GetUiType(gameObject);
            if (string.Equals(actualType, requestedType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(requestedType, "Panel", StringComparison.OrdinalIgnoreCase)
                && IsPanelLike(gameObject);
        }

        public static string GetPrimaryText(UnityEngine.GameObject gameObject)
        {
            var selfText = gameObject.GetComponent<UnityEngine.UI.Text>();
            if (selfText != null)
            {
                return selfText.text;
            }

            var inputField = gameObject.GetComponent<UnityEngine.UI.InputField>();
            if (inputField != null)
            {
                return inputField.text;
            }

            var dropdown = gameObject.GetComponent<UnityEngine.UI.Dropdown>();
            if (dropdown != null && dropdown.captionText != null)
            {
                return dropdown.captionText.text;
            }

            var childTexts = gameObject.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            for (var i = 0; i < childTexts.Length; i++)
            {
                var childText = childTexts[i];
                if (childText != null && childText.gameObject != gameObject)
                {
                    return childText.text;
                }
            }

            return null;
        }

        public static bool? GetInteractable(UnityEngine.GameObject gameObject)
        {
            var selectable = gameObject.GetComponent<UnityEngine.UI.Selectable>();
            return selectable != null ? selectable.interactable : null;
        }

        public static JObject CreateUiFindSummary(UnityEngine.GameObject gameObject, string hierarchyPath)
        {
            var rootCanvas = TryGetRootCanvas(gameObject, out var canvas) ? canvas : null;
            var interactable = GetInteractable(gameObject);
            var primaryText = GetPrimaryText(gameObject);
            var summary = new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(gameObject),
                ["name"] = gameObject.name,
                ["hierarchyPath"] = hierarchyPath,
                ["scenePath"] = gameObject.scene.path,
                ["sceneName"] = gameObject.scene.name,
                ["sceneAssetPath"] = gameObject.scene.path,
                ["uiType"] = GetUiType(gameObject),
                ["activeSelf"] = gameObject.activeSelf,
                ["componentTypes"] = SceneExplorationUtility.GetComponentTypeNames(gameObject),
                ["parentGlobalObjectId"] = gameObject.transform.parent != null
                    ? GlobalObjectIdResolver.GetId(gameObject.transform.parent.gameObject)
                    : JValue.CreateNull(),
                ["canvasGlobalObjectId"] = rootCanvas != null
                    ? GlobalObjectIdResolver.GetId(rootCanvas.gameObject)
                    : JValue.CreateNull(),
                ["canvasName"] = rootCanvas != null ? rootCanvas.gameObject.name : JValue.CreateNull(),
                ["interactable"] = interactable.HasValue ? interactable.Value : JValue.CreateNull()
            };

            if (!string.IsNullOrEmpty(primaryText))
            {
                summary["text"] = primaryText;
            }

            return summary;
        }

        public static JObject CreateUiDetails(UnityEngine.GameObject gameObject, string globalObjectId)
        {
            var hierarchyPath = SceneExplorationUtility.GetHierarchyPath(gameObject);
            var data = CreateUiFindSummary(gameObject, hierarchyPath);
            data["globalObjectId"] = globalObjectId;

            var rectTransform = gameObject.GetComponent<UnityEngine.RectTransform>();
            if (rectTransform != null)
            {
                data["rectTransform"] = CreateRectTransformSummary(rectTransform);
            }

            if (TryGetRootCanvas(gameObject, out var rootCanvas))
            {
                data["canvas"] = CreateCanvasSummary(rootCanvas);
            }

            var selectable = gameObject.GetComponent<UnityEngine.UI.Selectable>();
            if (selectable != null)
            {
                data["selectable"] = CreateSelectableSummary(selectable);
            }

            var text = gameObject.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
            {
                data["textComponent"] = CreateTextSummary(text);
            }

            var image = gameObject.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                data["image"] = CreateImageSummary(image);
            }

            var button = gameObject.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                data["button"] = new JObject
                {
                    ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(button),
                    ["interactable"] = button.interactable,
                    ["enabled"] = button.enabled,
                    ["transition"] = button.transition.ToString()
                };
            }

            var toggle = gameObject.GetComponent<UnityEngine.UI.Toggle>();
            if (toggle != null)
            {
                data["toggle"] = new JObject
                {
                    ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(toggle),
                    ["isOn"] = toggle.isOn,
                    ["interactable"] = toggle.interactable,
                    ["enabled"] = toggle.enabled
                };
            }

            var slider = gameObject.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null)
            {
                data["slider"] = new JObject
                {
                    ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(slider),
                    ["value"] = slider.value,
                    ["minValue"] = slider.minValue,
                    ["maxValue"] = slider.maxValue,
                    ["wholeNumbers"] = slider.wholeNumbers,
                    ["interactable"] = slider.interactable,
                    ["enabled"] = slider.enabled
                };
            }

            var dropdown = gameObject.GetComponent<UnityEngine.UI.Dropdown>();
            if (dropdown != null)
            {
                data["dropdown"] = new JObject
                {
                    ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(dropdown),
                    ["value"] = dropdown.value,
                    ["optionsCount"] = dropdown.options != null ? dropdown.options.Count : 0,
                    ["captionText"] = dropdown.captionText != null ? dropdown.captionText.text : JValue.CreateNull(),
                    ["interactable"] = dropdown.interactable,
                    ["enabled"] = dropdown.enabled
                };
            }

            var inputField = gameObject.GetComponent<UnityEngine.UI.InputField>();
            if (inputField != null)
            {
                data["inputField"] = new JObject
                {
                    ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(inputField),
                    ["text"] = inputField.text,
                    ["readOnly"] = inputField.readOnly,
                    ["contentType"] = inputField.contentType.ToString(),
                    ["lineType"] = inputField.lineType.ToString(),
                    ["interactable"] = inputField.interactable,
                    ["enabled"] = inputField.enabled
                };
            }

            var scrollRect = gameObject.GetComponent<UnityEngine.UI.ScrollRect>();
            if (scrollRect != null)
            {
                data["scrollRect"] = new JObject
                {
                    ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(scrollRect),
                    ["horizontal"] = scrollRect.horizontal,
                    ["vertical"] = scrollRect.vertical,
                    ["movementType"] = scrollRect.movementType.ToString(),
                    ["enabled"] = scrollRect.enabled
                };
            }

            return data;
        }

        public static JObject CreateRectTransformSummary(UnityEngine.RectTransform rectTransform)
        {
            return new JObject
            {
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(rectTransform),
                ["anchoredPosition"] = CreateVector2(rectTransform.anchoredPosition),
                ["sizeDelta"] = CreateVector2(rectTransform.sizeDelta),
                ["anchorMin"] = CreateVector2(rectTransform.anchorMin),
                ["anchorMax"] = CreateVector2(rectTransform.anchorMax),
                ["pivot"] = CreateVector2(rectTransform.pivot)
            };
        }

        public static JObject CreateCanvasSummary(UnityEngine.Canvas canvas)
        {
            return new JObject
            {
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(canvas),
                ["gameObjectGlobalObjectId"] = GlobalObjectIdResolver.GetId(canvas.gameObject),
                ["name"] = canvas.gameObject.name,
                ["renderMode"] = canvas.renderMode.ToString(),
                ["isRootCanvas"] = canvas.isRootCanvas
            };
        }

        public static JObject CreateSelectableSummary(UnityEngine.UI.Selectable selectable)
        {
            return new JObject
            {
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(selectable),
                ["typeName"] = selectable.GetType().Name,
                ["interactable"] = selectable.interactable,
                ["enabled"] = selectable.enabled
            };
        }

        public static JObject CreateTextSummary(UnityEngine.UI.Text text)
        {
            return new JObject
            {
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(text),
                ["text"] = text.text,
                ["raycastTarget"] = text.raycastTarget,
                ["color"] = CreateColor(text.color)
            };
        }

        public static JObject CreateImageSummary(UnityEngine.UI.Image image)
        {
            return new JObject
            {
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(image),
                ["type"] = image.type.ToString(),
                ["raycastTarget"] = image.raycastTarget,
                ["color"] = CreateColor(image.color),
                ["sprite"] = image.sprite != null ? image.sprite.name : JValue.CreateNull()
            };
        }

        private static JArray CreateVector2(UnityEngine.Vector2 value)
        {
            return new JArray(value.x, value.y);
        }

        private static JArray CreateColor(UnityEngine.Color color)
        {
            return new JArray(color.r, color.g, color.b, color.a);
        }
    }
}
#endif
