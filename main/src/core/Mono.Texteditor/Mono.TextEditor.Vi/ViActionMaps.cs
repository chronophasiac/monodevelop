// 
// ViActionMaps.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace Mono.TextEditor.Vi
{
	
	
	public static class ViActionMaps
	{
	
		public static Action<ViMotionContext> GetEditObjectCharAction (char c)
		{
			switch (c) {
			case 'W':
			case 'w':
				return ViMotionsAndCommands.WordEnd;
			case 'B':
			case 'b':
				return ViMotionsAndCommands.WordStart;
			}
			return GetNavCharAction (c);
		}
		
		public static Action<ViMotionContext> GetNavCharAction (char c)
		{
			switch (c) {
			case 'h':
				return ViMotionsAndCommands.Left;
			case 'b':
				return ViMotionContext.ViDataToContext(CaretMoveActions.PreviousSubword);
			case 'B':
				return ViMotionContext.ViDataToContext(CaretMoveActions.PreviousWord);
			case 'l':
				return ViMotionsAndCommands.Right;
			case 'w':
				return ViMotionContext.ViDataToContext(CaretMoveActions.NextSubword);
			case 'W':
				return ViMotionContext.ViDataToContext(CaretMoveActions.NextWord);
			case 'k':
				return ViMotionsAndCommands.Up;
			case 'j':
				return ViMotionsAndCommands.Down;
			case '%':
				return ViMotionContext.ViDataToContext(MiscActions.GotoMatchingBracket);
			case '0':
				return ViMotionContext.ViDataToContext(CaretMoveActions.LineStart);
			case '^':
			case '_':
				return ViMotionContext.ViDataToContext(CaretMoveActions.LineFirstNonWhitespace);
			case '$':
				return ViMotionsAndCommands.LineEnd;
			case 'G':
				return ViMotionContext.ViDataToContext(CaretMoveActions.ToDocumentEnd);
			case '{':
				return ViMotionsAndCommands.MoveToPreviousEmptyLine;
			case '}':
				return ViMotionsAndCommands.MoveToNextEmptyLine;
			}
			return null;
		}
		
		public static Action<ViMotionContext> GetDirectionKeyAction (Gdk.Key key, Gdk.ModifierType modifier)
		{
			//
			// NO MODIFIERS
			//
			if ((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0) {
				switch (key) {
				case Gdk.Key.Left:
				case Gdk.Key.KP_Left:
					return ViMotionsAndCommands.Left;
					
				case Gdk.Key.Right:
				case Gdk.Key.KP_Right:
					return ViMotionsAndCommands.Right;
					
				case Gdk.Key.Up:
				case Gdk.Key.KP_Up:
					return ViMotionsAndCommands.Up;
					
				case Gdk.Key.Down:
				case Gdk.Key.KP_Down:
					return ViMotionsAndCommands.Down;
				
				//not strictly vi, but more useful IMO
				case Gdk.Key.KP_Home:
				case Gdk.Key.Home:
					return ViMotionContext.ViDataToContext(CaretMoveActions.LineHome);
					
				case Gdk.Key.KP_End:
				case Gdk.Key.End:
					return ViMotionsAndCommands.LineEnd;

				case Gdk.Key.Page_Up:
				case Gdk.Key.KP_Page_Up:
					return ViMotionContext.ViDataToContext(CaretMoveActions.PageUp);

				case Gdk.Key.Page_Down:
				case Gdk.Key.KP_Page_Down:
					return ViMotionContext.ViDataToContext(CaretMoveActions.PageDown);
				}
			}
			//
			// === CONTROL ===
			//
			else if ((modifier & Gdk.ModifierType.ShiftMask) == 0
			         && (modifier & Gdk.ModifierType.ControlMask) != 0)
			{
				switch (key) {
				case Gdk.Key.Left:
				case Gdk.Key.KP_Left:
					return ViMotionContext.ViDataToContext(CaretMoveActions.PreviousWord);
					
				case Gdk.Key.Right:
				case Gdk.Key.KP_Right:
					return ViMotionContext.ViDataToContext(CaretMoveActions.NextWord);
					
				case Gdk.Key.Up:
				case Gdk.Key.KP_Up:
					return ViMotionContext.ViDataToContext(ScrollActions.Up);
					
				// usually bound at IDE level
				case Gdk.Key.u:
					return ViMotionContext.ViDataToContext(CaretMoveActions.PageUp);
					
				case Gdk.Key.Down:
				case Gdk.Key.KP_Down:
					return ViMotionContext.ViDataToContext(ScrollActions.Down);
					
				case Gdk.Key.d:
					return ViMotionContext.ViDataToContext(CaretMoveActions.PageDown);
				
				case Gdk.Key.KP_Home:
				case Gdk.Key.Home:
					return ViMotionContext.ViDataToContext(CaretMoveActions.ToDocumentStart);
					
				case Gdk.Key.KP_End:
				case Gdk.Key.End:
					return ViMotionContext.ViDataToContext(CaretMoveActions.ToDocumentEnd);
				}
			}
			return null;
		}
		
		public static Action<ViMotionContext> GetInsertKeyAction (Gdk.Key key, Gdk.ModifierType modifier)
		{
			//
			// NO MODIFIERS
			//
			if ((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0) {
				switch (key) {
				case Gdk.Key.Tab:
					return ViMotionContext.ViDataToContext(MiscActions.InsertTab);
					
				case Gdk.Key.Return:
				case Gdk.Key.KP_Enter:
					return ViMotionContext.ViDataToContext(MiscActions.InsertNewLine);
					
				case Gdk.Key.BackSpace:
					return ViMotionContext.ViDataToContext(DeleteActions.Backspace);
					
				case Gdk.Key.Delete:
				case Gdk.Key.KP_Delete:
					return ViMotionContext.ViDataToContext(DeleteActions.Delete);
					
				case Gdk.Key.Insert:
					return ViMotionContext.ViDataToContext(MiscActions.SwitchCaretMode);
				}
			}
			//
			// CONTROL
			//
			else if ((modifier & Gdk.ModifierType.ControlMask) != 0
			         && (modifier & Gdk.ModifierType.ShiftMask) == 0)
			{
				switch (key) {
				case Gdk.Key.BackSpace:
					return ViMotionContext.ViDataToContext(DeleteActions.PreviousWord);
					
				case Gdk.Key.Delete:
				case Gdk.Key.KP_Delete:
					return ViMotionContext.ViDataToContext(DeleteActions.NextWord);
				}
			}
			//
			// SHIFT
			//
			else if ((modifier & Gdk.ModifierType.ControlMask) == 0
			         && (modifier & Gdk.ModifierType.ShiftMask) != 0)
			{
				switch (key) {
				case Gdk.Key.Tab:
					return ViMotionContext.ViDataToContext(MiscActions.RemoveTab);
					
				case Gdk.Key.BackSpace:
					return ViMotionContext.ViDataToContext(DeleteActions.Backspace);

				case Gdk.Key.Return:
				case Gdk.Key.KP_Enter:
					return ViMotionContext.ViDataToContext(MiscActions.InsertNewLine);
				}
			}
			return null;
		}
		
		public static Action<TextEditorData> GetCommandCharAction (char c)
		{
			switch (c) {
			case 'u':
				return MiscActions.Undo;
			}
			return null;
		}
	}
}
