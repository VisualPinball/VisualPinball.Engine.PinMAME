// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace VisualPinball.Engine.PinMAME.Editor
{
	internal sealed class PinMameSearchablePicker<T> : PopupWindowContent
	{
		private const float RowHeight = 40f;
		private const float HorizontalPadding = 8f;
		private const string NoneSearchText = "None clear selection";
		private readonly string _title;
		private readonly string _itemName;
		private readonly IReadOnlyList<T> _items;
		private readonly int _selectedIndex;
		private readonly Func<T, string> _primaryText;
		private readonly Func<T, string> _secondaryText;
		private readonly Func<T, string> _statusText;
		private readonly Func<T, string> _searchText;
		private readonly Action<int> _onSelected;
		private readonly bool _allowNone;
		private readonly SearchField _searchField = new();
		private readonly GUIStyle _statusStyle;
		private readonly GUIStyle _primaryStyle;
		private readonly GUIStyle _secondaryStyle;
		private List<int> _filteredIndices;
		private Vector2 _scrollPosition;
		private string _query = string.Empty;
		private int _highlightedRow;

		internal PinMameSearchablePicker(string title, string itemName, IReadOnlyList<T> items, int selectedIndex,
			Func<T, string> primaryText, Func<T, string> secondaryText, Func<T, string> statusText,
			Func<T, string> searchText, Action<int> onSelected, bool allowNone = false)
		{
			_title = title;
			_itemName = itemName;
			_items = items ?? Array.Empty<T>();
			_selectedIndex = selectedIndex;
			_primaryText = primaryText ?? throw new ArgumentNullException(nameof(primaryText));
			_secondaryText = secondaryText ?? (_ => string.Empty);
			_statusText = statusText ?? (_ => string.Empty);
			_searchText = searchText ?? primaryText;
			_onSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
			_allowNone = allowNone;
			_filteredIndices = Enumerable.Range(0, _items.Count).ToList();
			_statusStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperRight };
			_primaryStyle = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Clip, fontStyle = FontStyle.Bold };
			_secondaryStyle = new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip };
		}

		public override Vector2 GetWindowSize() => new(460f, 440f);

		public override void OnOpen()
		{
			editorWindow.wantsMouseMove = true;
			_searchField.SetFocus();
			if (_selectedIndex >= 0) {
				_highlightedRow = _filteredIndices.IndexOf(_selectedIndex) + (_allowNone ? 1 : 0);
				_scrollPosition.y = System.Math.Max(0f, _highlightedRow * RowHeight - 120f);
			}
		}

		public override void OnGUI(Rect rect)
		{
			var noneMatches = NoneMatches();
			HandleKeyboard(noneMatches);

			GUILayout.Space(6f);
			EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
			var nextQuery = _searchField.OnGUI(_query);
			if (!string.Equals(nextQuery, _query, StringComparison.Ordinal)) {
				_query = nextQuery;
				FilterItems();
				_scrollPosition = Vector2.zero;
				noneMatches = NoneMatches();
				// a typed query highlights the first match, never "None"
				_highlightedRow = noneMatches && _query.Trim().Length > 0 && _filteredIndices.Count > 0 ? 1 : 0;
			}

			var resultCount = _filteredIndices.Count + (noneMatches ? 1 : 0);
			EditorGUILayout.LabelField($"{resultCount} {(resultCount == 1 ? _itemName : _itemName + "s")}", EditorStyles.miniLabel);

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			if (resultCount > 0) {
				var listRect = GUILayoutUtility.GetRect(0f, resultCount * RowHeight, GUILayout.ExpandWidth(true));
				var firstVisibleRow = System.Math.Max(0, Mathf.FloorToInt(_scrollPosition.y / RowHeight));
				var lastVisibleRow = System.Math.Min(resultCount - 1,
					firstVisibleRow + Mathf.CeilToInt(GetWindowSize().y / RowHeight) + 1);
				for (var row = firstVisibleRow; row <= lastVisibleRow; row++) {
					var rowRect = new Rect(listRect.x, listRect.y + row * RowHeight, listRect.width, RowHeight);
					if (noneMatches && row == 0) {
						DrawRow(rowRect, -1, "None", "Clear the selected machine and ROM", string.Empty,
							_selectedIndex < 0, row == _highlightedRow);
						continue;
					}
					var index = _filteredIndices[row - (noneMatches ? 1 : 0)];
					var item = _items[index];
					DrawRow(rowRect, index, _primaryText(item), _secondaryText(item), _statusText(item),
						index == _selectedIndex, row == _highlightedRow);
				}
			} else {
				GUILayout.Space(12f);
				EditorGUILayout.LabelField("No matches", EditorStyles.centeredGreyMiniLabel);
			}
			EditorGUILayout.EndScrollView();

			if (Event.current.type == EventType.MouseMove) editorWindow.Repaint();
		}

		private void DrawRow(Rect rowRect, int index, string primary, string secondary, string status, bool selected,
			bool highlighted)
		{
			var hovered = rowRect.Contains(Event.current.mousePosition);
			if (Event.current.type == EventType.Repaint) {
				if (selected) {
					EditorGUI.DrawRect(rowRect, EditorGUIUtility.isProSkin
						? new Color(0.18f, 0.36f, 0.58f, 0.75f)
						: new Color(0.24f, 0.48f, 0.75f, 0.4f));
				} else if (hovered || highlighted) {
					EditorGUI.DrawRect(rowRect, EditorGUIUtility.isProSkin
						? new Color(1f, 1f, 1f, 0.08f)
						: new Color(0f, 0f, 0f, 0.06f));
				}
			}

			var contentRect = new Rect(rowRect.x + HorizontalPadding, rowRect.y + 3f,
				rowRect.width - HorizontalPadding * 2f, rowRect.height - 6f);
			var statusWidth = string.IsNullOrWhiteSpace(status) ? 0f : System.Math.Min(105f, _statusStyle.CalcSize(new GUIContent(status)).x + 8f);
			var primaryRect = new Rect(contentRect.x, contentRect.y, contentRect.width - statusWidth, 18f);
			var statusRect = new Rect(contentRect.xMax - statusWidth, contentRect.y, statusWidth, 18f);
			var secondaryRect = new Rect(contentRect.x, contentRect.y + 18f, contentRect.width, 16f);
			GUI.Label(primaryRect, primary ?? string.Empty, _primaryStyle);
			if (statusWidth > 0f) GUI.Label(statusRect, status, _statusStyle);
			GUI.Label(secondaryRect, secondary ?? string.Empty, _secondaryStyle);

			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hovered) {
				Event.current.Use();
				Select(index);
			}
		}

		private void FilterItems()
		{
			_filteredIndices = Enumerable.Range(0, _items.Count)
				.Where(index => MatchesQuery(_searchText(_items[index])))
				.ToList();
		}

		// Only offer "None" for an empty query or one that starts spelling it out, so a search such as
		// "elect" (Electra) never puts it on top, where Enter would clear the selection.
		private bool NoneMatches()
		{
			var query = _query.Trim();
			return _allowNone && (query.Length == 0 || NoneSearchText.Split(' ').Any(word => word.StartsWith(query, StringComparison.OrdinalIgnoreCase)));
		}

		private bool MatchesQuery(string value)
		{
			var terms = _query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			return terms.All(term => (value ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		private void HandleKeyboard(bool noneMatches)
		{
			if (Event.current.type != EventType.KeyDown) return;
			if (Event.current.keyCode == KeyCode.Escape) {
				Event.current.Use();
				editorWindow.Close();
				GUIUtility.ExitGUI();
			}
			var resultCount = _filteredIndices.Count + (noneMatches ? 1 : 0);
			if (Event.current.keyCode == KeyCode.DownArrow && resultCount > 0) {
				_highlightedRow = System.Math.Min(resultCount - 1, _highlightedRow + 1);
				ScrollToHighlightedRow();
				editorWindow.Repaint();
				Event.current.Use();
				return;
			}
			if (Event.current.keyCode == KeyCode.UpArrow && resultCount > 0) {
				_highlightedRow = System.Math.Max(0, _highlightedRow - 1);
				ScrollToHighlightedRow();
				editorWindow.Repaint();
				Event.current.Use();
				return;
			}
			if (Event.current.keyCode != KeyCode.Return && Event.current.keyCode != KeyCode.KeypadEnter) return;
			if (resultCount == 0) return;
			Event.current.Use();
			if (noneMatches && _highlightedRow == 0) {
				Select(-1);
			} else {
				Select(_filteredIndices[_highlightedRow - (noneMatches ? 1 : 0)]);
			}
		}

		private void ScrollToHighlightedRow()
		{
			var rowTop = _highlightedRow * RowHeight;
			var viewportHeight = GetWindowSize().y - 80f;
			if (rowTop < _scrollPosition.y) _scrollPosition.y = rowTop;
			if (rowTop + RowHeight > _scrollPosition.y + viewportHeight) {
				_scrollPosition.y = rowTop + RowHeight - viewportHeight;
			}
		}

		private void Select(int index)
		{
			_onSelected(index);
			editorWindow.Close();
			GUIUtility.ExitGUI();
		}
	}
}
