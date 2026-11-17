using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AppFlow.Editor
{
    /// <summary>Post-processing (Volume) style feature list for the Features option of an App State.</summary>
    [CustomPropertyDrawer(typeof(AppStateFeatures))]
    public sealed class AppStateFeaturesDrawer : PropertyDrawer
    {
        // Translucent so both editor skins read correctly.
        static Color Separator => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.1f) : new Color(0f, 0f, 0f, 0.2f);
        static Color HeaderBg => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.05f) : new Color(0f, 0f, 0f, 0.06f);

        static string DisplayName(Type t) => ObjectNames.NicifyVariableName(t.Name).Replace(" Feature", "");

        static VisualElement Section(VisualElement header)
        {
            header.style.backgroundColor = HeaderBg;
            header.style.borderTopWidth = 1;
            header.style.borderTopColor = Separator;
            header.style.marginLeft = 0;
            header.style.marginRight = 0;
            header.style.marginTop = 0;
            header.style.marginBottom = 0;
            header.style.paddingLeft = 2;
            header.style.minHeight = 22;
            header.style.alignItems = Align.Center;
            return header;
        }

        static Button MenuButton(Action<Rect> open)
        {
            Button button = null;
            button = new Button(() => open(button.worldBound))
            {
                // Same icon as the component header menu in the Inspector.
                iconImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("_Menu").image),
                tooltip = "Feature options",
                style =
                {
                    marginLeft = StyleKeyword.Auto, marginRight = 2, width = 18, height = 18,
                    paddingLeft = 1, paddingRight = 1, paddingTop = 1, paddingBottom = 1,
                    backgroundColor = Color.clear, borderTopWidth = 0, borderBottomWidth = 0, borderLeftWidth = 0, borderRightWidth = 0,
                },
            };
            return button;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var items = property.FindPropertyRelative("Items");
            if (items == null)
            {
                // The option value came back null: seed it so the list has something to bind to.
                property.boxedValue = new AppStateFeatures();
                property.serializedObject.ApplyModifiedProperties();
                items = property.FindPropertyRelative("Items");
            }

            var root = new VisualElement();
            // Graph Toolkit lays the option label and value out in a row; stack them so headers span the inspector like the Volume editor.
            // Done on layout: the field's classes and parent are not final yet when the drawer attaches.
            root.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                for (var e = root.parent; e != null; e = e.parent)
                    if (e.ClassListContains("ge-model-property-field")) { e.style.flexDirection = FlexDirection.Column; e.style.alignItems = Align.Stretch; break; }
            });
            var list = new VisualElement { style = { marginTop = 2, borderBottomWidth = 1, borderBottomColor = Separator } };
            root.Add(list);
            var addButton = new Button { text = "Add Feature", style = { height = 22, marginLeft = 0, marginRight = 0, marginTop = 6 } };
            root.Add(addButton);

            void Commit()
            {
                property.serializedObject.ApplyModifiedProperties();
                Rebuild();
            }

            void ShowMenu(int index, Rect at)
            {
                var el = items.GetArrayElementAtIndex(index);
                var type = el.managedReferenceValue?.GetType();
                var menu = new GenericMenu();
                if (type != null)
                    menu.AddItem(new GUIContent("Reset"), false, () => { el.managedReferenceValue = Activator.CreateInstance(type); Commit(); });
                else
                    menu.AddDisabledItem(new GUIContent("Reset"));
                menu.AddItem(new GUIContent("Remove"), false, () => { items.DeleteArrayElementAtIndex(index); Commit(); });
                menu.AddSeparator("");
                if (index > 0) menu.AddItem(new GUIContent("Move Up"), false, () => { items.MoveArrayElement(index, index - 1); Commit(); });
                else menu.AddDisabledItem(new GUIContent("Move Up"));
                if (index < items.arraySize - 1) menu.AddItem(new GUIContent("Move Down"), false, () => { items.MoveArrayElement(index, index + 1); Commit(); });
                else menu.AddDisabledItem(new GUIContent("Move Down"));
                menu.DropDown(at);
            }

            void Rebuild()
            {
                list.Clear();
                for (var i = 0; i < items.arraySize; i++)
                {
                    var index = i;
                    var el = items.GetArrayElementAtIndex(i);
                    var menuButton = MenuButton(at => ShowMenu(index, at));
                    if (el.managedReferenceValue == null)
                    {
                        var row = Section(new VisualElement { style = { flexDirection = FlexDirection.Row } });
                        row.Add(new Label("Missing feature (class renamed or deleted)")
                        {
                            style = { paddingLeft = 16, unityFontStyleAndWeight = FontStyle.Italic, color = new Color(1f, 0.75f, 0.2f) },
                        });
                        row.Add(menuButton);
                        list.Add(row);
                        continue;
                    }

                    var foldout = new Foldout { text = DisplayName(el.managedReferenceValue.GetType()), value = true };
                    foldout.contentContainer.style.paddingTop = 2;
                    foldout.contentContainer.style.paddingBottom = 4;
                    foldout.contentContainer.style.paddingRight = 4;
                    var header = (Toggle)Section(foldout.Q<Toggle>());
                    var title = header.Q<Label>();
                    title.style.unityFontStyleAndWeight = FontStyle.Bold;
                    // Volume-style header: arrow, enable checkbox, title, options menu.
                    var enabled = new Toggle { bindingPath = el.FindPropertyRelative(nameof(AppStateFeature.Enabled)).propertyPath, tooltip = "Enabled", style = { marginLeft = 0, marginRight = 4 } };
                    enabled.RegisterValueChangedCallback(e =>
                    {
                        foldout.contentContainer.SetEnabled(e.newValue);
                        title.SetEnabled(e.newValue);
                    });
                    title.parent.Insert(title.parent.IndexOf(title), enabled);
                    header.Add(menuButton);

                    var end = el.GetEndProperty();
                    var child = el.Copy();
                    var depth = el.depth + 1;
                    for (var enter = true; child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end); enter = false)
                    {
                        if (child.depth != depth || child.name == nameof(AppStateFeature.Enabled)) continue;
                        foldout.Add(new PropertyField(child.Copy()));
                    }
                    list.Add(foldout);
                }
                list.style.display = items.arraySize > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                list.Bind(property.serializedObject);
            }

            addButton.clicked += () =>
            {
                var used = Enumerable.Range(0, items.arraySize)
                    .Select(i => items.GetArrayElementAtIndex(i).managedReferenceValue?.GetType())
                    .Where(t => t != null).ToHashSet();
                var types = TypeCache.GetTypesDerivedFrom<AppStateFeature>()
                    .Where(t => !t.IsAbstract && !t.IsGenericType).OrderBy(t => t.Name).ToList();
                var menu = new GenericMenu();
                if (types.Count == 0) menu.AddDisabledItem(new GUIContent("No AppStateFeature types found"));
                foreach (var t in types)
                {
                    var label = new GUIContent(DisplayName(t));
                    if (used.Contains(t)) { menu.AddDisabledItem(label); continue; }
                    menu.AddItem(label, false, () =>
                    {
                        items.arraySize++;
                        items.GetArrayElementAtIndex(items.arraySize - 1).managedReferenceValue = Activator.CreateInstance(t);
                        Commit();
                    });
                }
                menu.DropDown(addButton.worldBound);
            };

            Rebuild();
            return root;
        }
    }
}
