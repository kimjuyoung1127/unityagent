using System;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UiElementCreateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiElementCreate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var type = request.GetParam("type", null);
            if (string.IsNullOrEmpty(type))
                return InvalidParameters("Parameter 'type' is required.");

            var name = request.GetParam("name", null);
            var parentId = request.GetParam("parent", null);

            UnityEngine.Transform parentTransform = null;
            if (!string.IsNullOrEmpty(parentId))
            {
                var parentGo = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(parentId);
                if (parentGo == null)
                    return Fail(StatusCode.NotFound, $"Parent not found: {parentId}");
                parentTransform = parentGo.transform;
            }

            var undoName = $"unityctl: ui-element-create: {type}";
            using (new UndoScope(undoName))
            {
                UnityEngine.GameObject go;

                switch (type.ToLowerInvariant())
                {
                    case "button":
                        go = CreateButton(name ?? "Button");
                        break;
                    case "text":
                        go = CreateText(name ?? "Text");
                        break;
                    case "image":
                        go = CreateImage(name ?? "Image");
                        break;
                    case "panel":
                        go = CreatePanel(name ?? "Panel");
                        break;
                    case "inputfield":
                        go = CreateInputField(name ?? "InputField");
                        break;
                    case "toggle":
                        go = CreateToggle(name ?? "Toggle");
                        break;
                    case "slider":
                        go = CreateSlider(name ?? "Slider");
                        break;
                    case "dropdown":
                        go = CreateDropdown(name ?? "Dropdown");
                        break;
                    case "scrollview":
                        go = CreateScrollView(name ?? "ScrollView");
                        break;
                    default:
                        return InvalidParameters(
                            $"Unknown UI type: '{type}'. Valid types: " +
                            "Button, Text, Image, Panel, InputField, Toggle, Slider, Dropdown, ScrollView");
                }

                UnityEditor.Undo.RegisterCreatedObjectUndo(go, undoName);

                if (parentTransform != null)
                {
                    UnityEditor.Undo.SetTransformParent(go.transform, parentTransform, undoName);
                }

                EditorSceneManager.MarkSceneDirty(go.scene);

                var globalId = GlobalObjectIdResolver.GetId(go);
                return Ok($"Created UI {type} '{go.name}'", new JObject
                {
                    ["globalObjectId"] = globalId,
                    ["name"] = go.name,
                    ["type"] = type,
                    ["scenePath"] = go.scene.path,
                    ["sceneDirty"] = true,
                    ["undoGroupName"] = undoName
                });
            }
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static UnityEngine.GameObject CreateButton(string name)
        {
            var go = new UnityEngine.GameObject(name);
            go.AddComponent<UnityEngine.RectTransform>();
            go.AddComponent<UnityEngine.UI.Image>();
            go.AddComponent<UnityEngine.UI.Button>();

            var textGo = new UnityEngine.GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var rectTransform = textGo.AddComponent<UnityEngine.RectTransform>();
            rectTransform.anchorMin = UnityEngine.Vector2.zero;
            rectTransform.anchorMax = UnityEngine.Vector2.one;
            rectTransform.sizeDelta = UnityEngine.Vector2.zero;
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.text = "Button";
            text.alignment = UnityEngine.TextAnchor.MiddleCenter;
            text.color = UnityEngine.Color.black;

            return go;
        }

        private static UnityEngine.GameObject CreateText(string name)
        {
            var go = new UnityEngine.GameObject(name);
            go.AddComponent<UnityEngine.RectTransform>();
            var text = go.AddComponent<UnityEngine.UI.Text>();
            text.text = "New Text";
            text.color = UnityEngine.Color.black;
            return go;
        }

        private static UnityEngine.GameObject CreateImage(string name)
        {
            var go = new UnityEngine.GameObject(name);
            go.AddComponent<UnityEngine.RectTransform>();
            go.AddComponent<UnityEngine.UI.Image>();
            return go;
        }

        private static UnityEngine.GameObject CreatePanel(string name)
        {
            var go = new UnityEngine.GameObject(name);
            var rt = go.AddComponent<UnityEngine.RectTransform>();
            rt.anchorMin = UnityEngine.Vector2.zero;
            rt.anchorMax = UnityEngine.Vector2.one;
            rt.sizeDelta = UnityEngine.Vector2.zero;
            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = new UnityEngine.Color(1f, 1f, 1f, 0.4f);
            return go;
        }

        private static UnityEngine.GameObject CreateInputField(string name)
        {
            var go = new UnityEngine.GameObject(name);
            go.AddComponent<UnityEngine.RectTransform>();
            go.AddComponent<UnityEngine.UI.Image>();
            var inputField = go.AddComponent<UnityEngine.UI.InputField>();

            var textGo = new UnityEngine.GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<UnityEngine.RectTransform>();
            textRt.anchorMin = UnityEngine.Vector2.zero;
            textRt.anchorMax = UnityEngine.Vector2.one;
            textRt.sizeDelta = UnityEngine.Vector2.zero;
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.color = UnityEngine.Color.black;
            text.supportRichText = false;

            inputField.textComponent = text;

            return go;
        }

        private static UnityEngine.GameObject CreateToggle(string name)
        {
            var go = new UnityEngine.GameObject(name);
            go.AddComponent<UnityEngine.RectTransform>();
            var toggle = go.AddComponent<UnityEngine.UI.Toggle>();

            var bgGo = new UnityEngine.GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            bgGo.AddComponent<UnityEngine.RectTransform>();
            bgGo.AddComponent<UnityEngine.UI.Image>();

            var checkGo = new UnityEngine.GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform, false);
            checkGo.AddComponent<UnityEngine.RectTransform>();
            var checkImage = checkGo.AddComponent<UnityEngine.UI.Image>();

            toggle.graphic = checkImage;

            return go;
        }

        private static UnityEngine.GameObject CreateSlider(string name)
        {
            var go = new UnityEngine.GameObject(name);
            go.AddComponent<UnityEngine.RectTransform>();
            var slider = go.AddComponent<UnityEngine.UI.Slider>();

            var bgGo = new UnityEngine.GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            bgGo.AddComponent<UnityEngine.RectTransform>();
            bgGo.AddComponent<UnityEngine.UI.Image>();

            var fillAreaGo = new UnityEngine.GameObject("Fill Area");
            fillAreaGo.transform.SetParent(go.transform, false);
            fillAreaGo.AddComponent<UnityEngine.RectTransform>();

            var fillGo = new UnityEngine.GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            fillGo.AddComponent<UnityEngine.RectTransform>();
            var fillImage = fillGo.AddComponent<UnityEngine.UI.Image>();
            slider.fillRect = fillGo.GetComponent<UnityEngine.RectTransform>();

            var handleAreaGo = new UnityEngine.GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(go.transform, false);
            handleAreaGo.AddComponent<UnityEngine.RectTransform>();

            var handleGo = new UnityEngine.GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            handleGo.AddComponent<UnityEngine.RectTransform>();
            handleGo.AddComponent<UnityEngine.UI.Image>();
            slider.handleRect = handleGo.GetComponent<UnityEngine.RectTransform>();

            return go;
        }

        private static UnityEngine.GameObject CreateDropdown(string name)
        {
            var go = new UnityEngine.GameObject(name);
            go.AddComponent<UnityEngine.RectTransform>();
            go.AddComponent<UnityEngine.UI.Image>();
            var dropdown = go.AddComponent<UnityEngine.UI.Dropdown>();

            var labelGo = new UnityEngine.GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<UnityEngine.RectTransform>();
            labelRt.anchorMin = UnityEngine.Vector2.zero;
            labelRt.anchorMax = UnityEngine.Vector2.one;
            labelRt.sizeDelta = UnityEngine.Vector2.zero;
            var labelText = labelGo.AddComponent<UnityEngine.UI.Text>();
            labelText.text = "Option A";
            labelText.color = UnityEngine.Color.black;
            labelText.alignment = UnityEngine.TextAnchor.MiddleLeft;

            dropdown.captionText = labelText;

            return go;
        }

        private static UnityEngine.GameObject CreateScrollView(string name)
        {
            var go = new UnityEngine.GameObject(name);
            var rt = go.AddComponent<UnityEngine.RectTransform>();
            go.AddComponent<UnityEngine.UI.Image>();
            var scrollRect = go.AddComponent<UnityEngine.UI.ScrollRect>();

            var viewportGo = new UnityEngine.GameObject("Viewport");
            viewportGo.transform.SetParent(go.transform, false);
            var viewportRt = viewportGo.AddComponent<UnityEngine.RectTransform>();
            viewportRt.anchorMin = UnityEngine.Vector2.zero;
            viewportRt.anchorMax = UnityEngine.Vector2.one;
            viewportRt.sizeDelta = UnityEngine.Vector2.zero;
            viewportGo.AddComponent<UnityEngine.UI.Image>();
            viewportGo.AddComponent<UnityEngine.UI.Mask>();

            var contentGo = new UnityEngine.GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.AddComponent<UnityEngine.RectTransform>();
            contentRt.anchorMin = new UnityEngine.Vector2(0, 1);
            contentRt.anchorMax = new UnityEngine.Vector2(1, 1);
            contentRt.pivot = new UnityEngine.Vector2(0.5f, 1);

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            return go;
        }
#endif
    }
}
