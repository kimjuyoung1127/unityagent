using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UiSetRectHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiSetRect;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var go = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(id);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");

            var rectTransform = go.GetComponent<UnityEngine.RectTransform>();
            if (rectTransform == null)
                return Fail(StatusCode.NotFound,
                    $"RectTransform not found on '{go.name}'. Is this a UI element?");

            var undoName = $"unityctl: ui-set-rect: {go.name}";
            using (new UndoScope(undoName))
            {
                UnityEditor.Undo.RecordObject(rectTransform, undoName);

                var anchoredPosition = request.GetParam("anchoredPosition", null);
                if (!string.IsNullOrEmpty(anchoredPosition))
                {
                    var arr = JArray.Parse(anchoredPosition);
                    rectTransform.anchoredPosition = new UnityEngine.Vector2(
                        arr[0].Value<float>(), arr[1].Value<float>());
                }

                var sizeDelta = request.GetParam("sizeDelta", null);
                if (!string.IsNullOrEmpty(sizeDelta))
                {
                    var arr = JArray.Parse(sizeDelta);
                    rectTransform.sizeDelta = new UnityEngine.Vector2(
                        arr[0].Value<float>(), arr[1].Value<float>());
                }

                var anchorMin = request.GetParam("anchorMin", null);
                if (!string.IsNullOrEmpty(anchorMin))
                {
                    var arr = JArray.Parse(anchorMin);
                    rectTransform.anchorMin = new UnityEngine.Vector2(
                        arr[0].Value<float>(), arr[1].Value<float>());
                }

                var anchorMax = request.GetParam("anchorMax", null);
                if (!string.IsNullOrEmpty(anchorMax))
                {
                    var arr = JArray.Parse(anchorMax);
                    rectTransform.anchorMax = new UnityEngine.Vector2(
                        arr[0].Value<float>(), arr[1].Value<float>());
                }

                var pivot = request.GetParam("pivot", null);
                if (!string.IsNullOrEmpty(pivot))
                {
                    var arr = JArray.Parse(pivot);
                    rectTransform.pivot = new UnityEngine.Vector2(
                        arr[0].Value<float>(), arr[1].Value<float>());
                }

                EditorSceneManager.MarkSceneDirty(go.scene);

                return Ok($"Updated RectTransform on '{go.name}'", new JObject
                {
                    ["globalObjectId"] = id,
                    ["name"] = go.name,
                    ["anchoredPosition"] = FormatVector2(rectTransform.anchoredPosition),
                    ["sizeDelta"] = FormatVector2(rectTransform.sizeDelta),
                    ["anchorMin"] = FormatVector2(rectTransform.anchorMin),
                    ["anchorMax"] = FormatVector2(rectTransform.anchorMax),
                    ["pivot"] = FormatVector2(rectTransform.pivot),
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
        private static string FormatVector2(UnityEngine.Vector2 v)
        {
            return $"[{v.x},{v.y}]";
        }
#endif
    }
}
