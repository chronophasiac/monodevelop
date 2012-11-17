//
// ViKeypressHandlers.cs
//
// Author:
//       Michael Raimondi <zeno@pobox.com>
//
// Copyright (c) 2012 Michael Raimondi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Mono.TextEditor.Vi
{
	public partial class ViEditMode : EditMode
	{
		protected void NormalStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier, Gdk.Key key)
		{
			Action<ViMotionContext> context = null;
			if (((modifier & (Gdk.ModifierType.ControlMask)) == 0)) {
				if (key == Gdk.Key.Delete)
					unicodeKey = 'x';
				switch ((char)unicodeKey) {
				case '?':
				case '/':
				case ':':
					state = State.Command;
					commandBuffer.Append ((char)unicodeKey);
					Status = commandBuffer.ToString ();
					return;
					
				case 'A':
					InsertState (CaretMoveActions.LineEnd);
					return;
						
				case 'I':
					InsertState (CaretMoveActions.LineFirstNonWhitespace);
					return;

				case 'a':
						//use CaretMoveActions so that we can move past last character on line end
					InsertState (CaretMoveActions.Right);
					return;

				case 'i':
					InsertState ();
					return;
						
				case 'R':
					ClearRecordedAction();
					DoAction (() => {
						Caret.Mode = CaretMode.Underscore;
					}, true);
					Status = "-- REPLACE --";
					state = State.Replace;
					return;

				case 'V':
					Status = "-- VISUAL LINE --";
					Data.SetSelectLines (Caret.Line, Caret.Line);
					state = State.VisualLine;
					return;

				case 'v':
					Status = "-- VISUAL --";
					state = State.Visual;
					RunMotions (ViEditMode.VisualSelectionFromMotion (ViMotionsAndCommands.Right));
					return;
						
				case 'd':
					Status = "d";
					state = State.Delete;
					return;
						
				case 'y':
					Status = "y";
					state = State.Yank;
					return;

				case 'Y':
					state = State.Yank;
					HandleKeypress (Gdk.Key.y, (int)'y', Gdk.ModifierType.None);
					return;
						
				case 'O':
					InsertState (ViMotionsAndCommands.NewLineAbove);
					return;
						
				case 'o':
					InsertState (ViMotionsAndCommands.NewLineBelow);
					return;

				case 'r':
					ClearRecordedAction();
					DoAction (() => {
						Caret.Mode = CaretMode.Underscore;
					}, true);
					Status = "-- REPLACE --";
					state = State.WriteChar;
					return;
						
				case 'c':
					Caret.Mode = CaretMode.Insert;
					Status = "c";
					state = State.Change;
					return;
						
				case 'x':
					if (Data.Caret.Column == Data.Document.GetLine (Data.Caret.Line).Length + 1)
						return;
					Status = string.Empty;
					if (!Data.IsSomethingSelected)
						DoAction (() => {
							RunActions (SelectionActions.FromMoveAction (CaretMoveActions.Right), ClipboardActions.Cut);
						}
						);
					else
						DoAction (() => {
							RunActions (ClipboardActions.Cut);
						}
						);
					ViEditMode.RetreatFromLineEnd (Data);
					return;
						
				case 'X':
					if (Data.Caret.Column == DocumentLocation.MinColumn)
						return;
					Status = string.Empty;
					if (!Data.IsSomethingSelected && 0 < Caret.Offset)
						RunActions (SelectionActions.FromMoveAction (CaretMoveActions.Left), ClipboardActions.Cut);
					else
						RunActions (ClipboardActions.Cut);
					return;
						
				case 'D':
					RunActions (SelectionActions.FromMoveAction (CaretMoveActions.LineEnd), ClipboardActions.Cut);
					return;
						
				case 'C':
					InsertState (SelectionActions.FromMoveAction (CaretMoveActions.LineEnd), ClipboardActions.Cut);
					return;

				case '>':
					Status = ">";
					state = State.Indent;
					return;
						
				case '<':
					Status = "<";
					state = State.Unindent;
					return;
				case 'n':
					Search ();
					return;
				case 'N':
					searchBackward = !searchBackward;
					Search ();
					searchBackward = !searchBackward;
					return;
				case 'p':
					PasteAfter (false);
					return;
				case 'P':
					PasteBefore (false);
					return;
				case 's':
					if (!Data.IsSomethingSelected)
						InsertState (SelectionActions.FromMoveAction (CaretMoveActions.Right), ClipboardActions.Cut);
					else
						InsertState (ClipboardActions.Cut);
					return;

				case 'S':
					if (!Data.IsSomethingSelected)
						InsertState (SelectionActions.LineActionFromMoveAction (CaretMoveActions.LineEnd), ClipboardActions.Cut);
					else {
						Data.SetSelectLines (Data.MainSelection.Anchor.Line, Data.Caret.Line);
						InsertState (ClipboardActions.Cut);
					}
					return;
											
				case 'g':
					Status = "g";
					state = State.G;
					return;
						
				case 'H':
					Caret.Line = System.Math.Max (DocumentLocation.MinLine, Editor.PointToLocation (0, Editor.LineHeight - 1).Line);
					return;
				case 'J':
					RunActions (ViMotionsAndCommands.Join);
					return;
				case 'L':
					int line = Editor.PointToLocation (0, Editor.Allocation.Height - Editor.LineHeight * 2 - 2).Line;
					if (line < DocumentLocation.MinLine)
						line = Document.LineCount;
					Caret.Line = line;
					return;
				case 'M':
					line = Editor.PointToLocation (0, Editor.Allocation.Height / 2).Line;
					if (line < DocumentLocation.MinLine)
						line = Document.LineCount;
					Caret.Line = line;
					return;
						
				case '~':
					RunActions (ViMotionsAndCommands.ToggleCase);
					return;
						
				case 'z':
					Status = "z";
					state = State.Fold;
					return;
						
				case 'm':
					Status = "m";
					state = State.Mark;
					return;
						
				case '`':
					Status = "`";
					state = State.GoToMark;
					return;
						
				case '@':
					Status = "@";
					state = State.PlayMacro;
					return;
	
				case 'q':
					if (currentMacro == null) {
						Status = "q";
						state = State.NameMacro;
						return;
					} 
					currentMacro = null;
					Reset ("Macro Recorded");
					return;

				case '*':
					SearchWordAtCaret ();
					return;

				case '.':
					RepeatAction ();
					return;
				}
					
			}
				
			context = ViActionMaps.GetNavCharAction ((char)unicodeKey);
			if (context == null)
				context = ViActionMaps.GetDirectionKeyAction (key, modifier);
			if (context == null)
				context = ViMotionContext.ViDataToContext(ViActionMaps.GetCommandCharAction ((char)unicodeKey));
				
			if (context != null)
				RunMotions (context);
				
			//undo/redo may leave MD with a selection mode without activating visual mode
			CheckVisualMode ();
			return;
		}

		protected void DeleteStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier, Gdk.Key key)
		{
			Action<ViMotionContext> context = null;
			bool lineAction = false;
			if (((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0 
				&& unicodeKey == 'd')) {
				context = SelectionActions.LineActionFromMotion (CaretMoveActions.LineEndMotion);
				lineAction = true;
			} else {
				context = ViActionMaps.GetNavCharAction ((char)unicodeKey);
				if (context == null)
					context = ViActionMaps.GetDirectionKeyAction (key, modifier);
				if (context != null)
					context = SelectionActions.FromMotion (context);
			}
				
			if (context != null) {
				DoAction (() => {
					if (lineAction)
						RunMotions (context, ViMotionContext.ViDataToContext(ClipboardActions.Cut), ViMotionContext.ViDataToContext(CaretMoveActions.LineFirstNonWhitespace));
					else
						RunMotions (context, ViMotionContext.ViDataToContext(ClipboardActions.Cut));
				}
				);
				Reset ("");
			} else {
				Reset ("Unrecognised motion");
			}
				
			return;
		}

		protected void InsertState (params Action<TextEditorData> [] actions)
		{
			ClearRecordedAction();
			DoAction (() => {
				RunActions (actions);
			}, true);
			Caret.Mode = CaretMode.Insert;
			Status = "-- INSERT --";
			state = State.Insert;
			return;
		}

		protected void YankStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier, Gdk.Key key)
		{
			Action<ViMotionContext> context = null;
			bool lineAction = false;
			int offset = Caret.Offset;
			
			if (((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0 
			     && unicodeKey == 'y'))
			{
				context = SelectionActions.LineActionFromMotion (CaretMoveActions.LineEndMotion);
				lineAction	= true;
			} else {
				context = ViActionMaps.GetNavCharAction ((char)unicodeKey);
				if (context == null)
					context = ViActionMaps.GetDirectionKeyAction (key, modifier);
				if (context != null)
					context = SelectionActions.FromMotion (context);
			}
			
			if (context != null) {
				RunMotions (context);
				if (Data.IsSomethingSelected && !lineAction)
					offset = Data.SelectionRange.Offset;
				RunActions (ClipboardActions.Copy);
				Reset (string.Empty);
			} else {
				Reset ("Unrecognised motion");
			}
			Caret.Offset = offset;
			
			return;
		}

		protected void ChangeStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier, Gdk.Key key)
		{
			Action<ViMotionContext> context = null;
			bool lineAction = false;
			//copied from delete action
			if (((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0 
				&& unicodeKey == 'c')) {
				context = ViMotionContext.ViDataToContext(SelectionActions.LineActionFromMoveAction (CaretMoveActions.LineEnd));
				lineAction = true;
			} else {
				context = ViActionMaps.GetEditObjectCharAction ((char)unicodeKey);
				if (context == null)
					context = ViActionMaps.GetDirectionKeyAction (key, modifier);
				if (context != null)
					context = SelectionActions.FromMotion(context);
			}
				
			if (context != null) {
				if (lineAction)
					RunMotions (context, ViMotionContext.ViDataToContext(ClipboardActions.Cut), ViMotionContext.ViDataToContext(ViMotionsAndCommands.NewLineAbove));
				else
					RunMotions (context, ViMotionContext.ViDataToContext(ClipboardActions.Cut));
				Status = "-- INSERT --";
				state = State.Insert;
				Caret.Mode = CaretMode.Insert;
			} else {
				Reset ("Unrecognised motion");
			}
				
			return;
		}

		protected void InsertReplaceStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier, Gdk.Key key)
		{
			Action<ViMotionContext> context = GetInsertAction (key, modifier);
			if (context != null) {
				DoAction (() => {
					RunMotions (context);
				}
				, true);
			}
			else if (unicodeKey != 0) {
				DoAction(() => {
					InsertCharacter (unicodeKey);
				}	
				, true);
			}
			return;
		}

		protected void VisualLineStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier, Gdk.Key key)
		{
			if (key == Gdk.Key.Delete)
				unicodeKey = 'x';
			switch ((char)unicodeKey) {
			case 'p':
				PasteAfter (true);
				return;
			case 'P':
				PasteBefore (true);
				return;
			}
			Action<ViMotionContext> context = ViActionMaps.GetNavCharAction ((char)unicodeKey);
			if (context == null) {
				context = ViActionMaps.GetDirectionKeyAction (key, modifier);
			}
			if (context == null) {
				context = ViMotionContext.ViDataToContext(ViActionMaps.GetCommandCharAction ((char)unicodeKey));
			}
			if (context != null) {
				RunMotions (SelectionActions.LineActionFromMotion (context));
				return;
			}

			ApplyActionToSelection (modifier, unicodeKey);
			return;
		}

		protected void VisualStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier, Gdk.Key key)
		{
			if (key == Gdk.Key.Delete)
				unicodeKey = 'x';
			switch ((char)unicodeKey) {
			case 'p':
				PasteAfter (false);
				return;
			case 'P':
				PasteBefore (false);
				return;
			}
			Action<ViMotionContext> context = ViActionMaps.GetNavCharAction ((char)unicodeKey);
			if (context == null) {
				context = ViActionMaps.GetDirectionKeyAction (key, modifier);
			}
			if (context == null) {
				context = ViMotionContext.ViDataToContext(ViActionMaps.GetCommandCharAction ((char)unicodeKey));
			}
			if (context != null) {
				RunMotions (ViEditMode.VisualSelectionFromMotion (context));
				return;
			}

			ApplyActionToSelection (modifier, unicodeKey);
			return;
		}

		protected void CommandStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier, Gdk.Key key)
		{
			switch (key) {
			case Gdk.Key.Return:
			case Gdk.Key.KP_Enter:
				Status = RunExCommand (commandBuffer.ToString ());
				commandBuffer.Length = 0;
				state = State.Normal;
				break;
			case Gdk.Key.BackSpace:
			case Gdk.Key.Delete:
			case Gdk.Key.KP_Delete:
				if (0 < commandBuffer.Length) {
					commandBuffer.Remove (commandBuffer.Length - 1, 1);
					Status = commandBuffer.ToString ();
					if (0 == commandBuffer.Length)
						Reset (Status);
				}
				break;
			default:
				if (unicodeKey != 0) {
					commandBuffer.Append ((char)unicodeKey);
					Status = commandBuffer.ToString ();
				}
				break;
			}
			return;
		}

		protected void WriteCharStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (unicodeKey != 0) {
				DoAction (() => {
					RunActions (SelectionActions.StartSelection);
					int roffset = Data.SelectionRange.Offset;
					InsertCharacter ((char)unicodeKey);
					Reset (string.Empty);
					Caret.Offset = roffset;
				});
			} else {
				Reset ("Keystroke was not a character");
			}
			return;
		}

		protected void IndentStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier,  Gdk.Key key)
		{
			if (((modifier & (Gdk.ModifierType.ControlMask)) == 0 && unicodeKey == '>')) {
				RunActions (MiscActions.IndentSelection);
				Reset ("");
				return;
			}
			
			Action<ViMotionContext> context = ViActionMaps.GetNavCharAction ((char)unicodeKey);
			if (context == null)
				context = ViActionMaps.GetDirectionKeyAction (key, modifier);
			
			if (context != null) {
				RunMotions (SelectionActions.FromMotion (context), MiscActions.IndentSelectionMotion);
				Reset ("");
			} else {
				Reset ("Unrecognised motion");
			}
			return;
		}

		protected void UnindentStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier,  Gdk.Key key)
		{
			if (((modifier & (Gdk.ModifierType.ControlMask)) == 0 && ((char)unicodeKey) == '<')) {
				RunActions (MiscActions.RemoveIndentSelection);
				Reset ("");
				return;
			}
			
			Action<ViMotionContext> context = ViActionMaps.GetNavCharAction ((char)unicodeKey);
			if (context == null)
				context = ViActionMaps.GetDirectionKeyAction (key, modifier);
			
			if (context != null) {
				RunMotions (SelectionActions.FromMotion (context), MiscActions.RemoveIndentSelectionMotion);
				Reset ("");
			} else {
				Reset ("Unrecognised motion");
			}
			return;
		}

		protected void GStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (((modifier & (Gdk.ModifierType.ControlMask)) == 0)) {
				switch ((char)unicodeKey) {
				case 'g':
					Caret.Offset = 0;
					Reset ("");
					return;
				}
			}
			Reset ("Unknown command");
			return;
		}

		protected void MarkStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier)
		{
			char k = (char)unicodeKey;
			ViMark mark = null;
			if (!char.IsLetterOrDigit(k)) {
				Reset ("Invalid Mark");
				return;
			}
			if (marks.ContainsKey(k)) {
				mark = marks [k];
			} else {
				mark = new ViMark(k);
				marks [k] = mark;
			}
			RunActions(mark.SaveMark);
			Reset("");
			return;
		}

		protected void NameMacroStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier)
		{
			char k = (char) unicodeKey;
			if(!char.IsLetterOrDigit(k)) {
				Reset("Invalid Macro Name");
				return;
			}
			currentMacro = new ViMacro (k);
			currentMacro.KeysPressed = new Queue<ViMacro.KeySet> ();
			macros [k] = currentMacro;
			Reset("");
			return;
		}	

		protected void PlayMacroStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier)
		{
			char k = (char) unicodeKey;
			if (k == '@') 
				k = macros_lastplayed;
			if (macros.ContainsKey(k)) {
				Reset ("");
				macros_lastplayed = k; // FIXME play nice when playing macros from inside macros?
				ViMacro macroToPlay = macros [k];
				foreach (ViMacro.KeySet keySet in macroToPlay.KeysPressed) {
					HandleKeypress(keySet.Key, keySet.UnicodeKey, keySet.Modifiers); // FIXME stop on errors? essential with multipliers and nowrapscan
				}
				// Once all the keys have been played back, quickly exit.
				return;
			} else {
				Reset ("Invalid Macro Name '" + k + "'");
				return;
			}
		}	

		protected void GoToMarkStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier)
		{
			char k = (char)unicodeKey;
			if (marks.ContainsKey(k)) {
				RunActions(marks [k].LoadMark);
				Reset ("");
			} else {
				Reset ("Unknown Mark");
			}
			return;
		}

		protected void FoldStateHandleKeypress (uint unicodeKey, Gdk.ModifierType modifier)
		{
			Action<TextEditorData> action = null;
			if (((modifier & (Gdk.ModifierType.ControlMask)) == 0)) {
				switch ((char)unicodeKey) {
					case 'A':
					// Recursive fold toggle
						action = FoldActions.ToggleFoldRecursive;
						break;
					case 'C':
					// Recursive fold close
						action = FoldActions.CloseFoldRecursive;
						break;
					case 'M':
					// Close all folds
						action = FoldActions.CloseAllFolds;
						break;
					case 'O':
					// Recursive fold open
						action = FoldActions.OpenFoldRecursive;
						break;
					case 'R':
					// Expand all folds
						action = FoldActions.OpenAllFolds;
						break;
					case 'a':
					// Fold toggle
						action = FoldActions.ToggleFold;
						break;
					case 'c':
					// Fold close
						action = FoldActions.CloseFold;
						break;
					case 'o':
					// Fold open
						action = FoldActions.OpenFold;
						break;
					default:
						Reset ("Unknown command");
						break;
				}
				
				if (null != action) {
					RunActions (action);
					Reset (string.Empty);
				}
			}
		}
	}
}