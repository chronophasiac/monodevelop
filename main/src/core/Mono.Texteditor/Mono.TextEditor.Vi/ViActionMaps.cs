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
	
		public static Func<ViMotionContext, ViMotionResult> GetEditObjectCharAction (char c)
		{
			switch (c) {
			case 'W':
			case 'w':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.WordEnd);
			case 'B':
			case 'b':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.WordStart);
			}
			return GetNavCharAction (c);
		}
		
		public static Func<ViMotionContext, ViMotionResult> GetNavCharAction (char c)
		{
			switch (c) {
			case 'h':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.Left);
			case 'b':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.PreviousSubword);
			case 'B':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.PreviousWord);
			case 'l':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.Right);
			case 'w':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.NextSubword);
			case 'W':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.NextWord);
			case 'k':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.Up);
			case 'j':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.Down);
			case '%':
				return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(MiscActions.GotoMatchingBracket));
			case '0':
				return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.LineStart));
			case '^':
			case '_':
				return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.LineFirstNonWhitespace));
			case '$':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.LineEnd);
			case 'G':
				return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.ToDocumentEnd));
			case '{':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.MoveToPreviousEmptyLine);
			case '}':
				return ViMotionResult.DoMotion(ViMotionsAndCommands.MoveToNextEmptyLine);
			}
			return null;
		}
		
		public static Func<ViMotionContext, ViMotionResult> GetDirectionKeyAction (Gdk.Key key, Gdk.ModifierType modifier)
		{
			//
			// NO MODIFIERS
			//
			if ((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0) {
				switch (key) {
				case Gdk.Key.Left:
				case Gdk.Key.KP_Left:
					return ViMotionResult.DoMotion(ViMotionsAndCommands.Left);
					
				case Gdk.Key.Right:
				case Gdk.Key.KP_Right:
					return ViMotionResult.DoMotion(ViMotionsAndCommands.Right);
					
				case Gdk.Key.Up:
				case Gdk.Key.KP_Up:
					return ViMotionResult.DoMotion(ViMotionsAndCommands.Up);
					
				case Gdk.Key.Down:
				case Gdk.Key.KP_Down:
					return ViMotionResult.DoMotion(ViMotionsAndCommands.Down);
				
				//not strictly vi, but more useful IMO
				case Gdk.Key.KP_Home:
				case Gdk.Key.Home:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.LineHome));
					
				case Gdk.Key.KP_End:
				case Gdk.Key.End:
					return ViMotionResult.DoMotion(ViMotionsAndCommands.LineEnd);

				case Gdk.Key.Page_Up:
				case Gdk.Key.KP_Page_Up:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.PageUp));

				case Gdk.Key.Page_Down:
				case Gdk.Key.KP_Page_Down:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.PageDown));
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
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.PreviousWord));
					
				case Gdk.Key.Right:
				case Gdk.Key.KP_Right:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.NextWord));
					
				case Gdk.Key.Up:
				case Gdk.Key.KP_Up:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(ScrollActions.Up));
					
				// usually bound at IDE level
				case Gdk.Key.u:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.PageUp));
					
				case Gdk.Key.Down:
				case Gdk.Key.KP_Down:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(ScrollActions.Down));
					
				case Gdk.Key.d:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.PageDown));
				
				case Gdk.Key.KP_Home:
				case Gdk.Key.Home:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.ToDocumentStart));
					
				case Gdk.Key.KP_End:
				case Gdk.Key.End:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(CaretMoveActions.ToDocumentEnd));
				}
			}
			return null;
		}
		
		public static Func<ViMotionContext, ViMotionResult> GetInsertKeyAction (Gdk.Key key, Gdk.ModifierType modifier)
		{
			//
			// NO MODIFIERS
			//
			if ((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0) {
				switch (key) {
				case Gdk.Key.Tab:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(MiscActions.InsertTab));
					
				case Gdk.Key.Return:
				case Gdk.Key.KP_Enter:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(MiscActions.InsertNewLine));
					
				case Gdk.Key.BackSpace:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(DeleteActions.Backspace));
					
				case Gdk.Key.Delete:
				case Gdk.Key.KP_Delete:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(DeleteActions.Delete));
					
				case Gdk.Key.Insert:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(MiscActions.SwitchCaretMode));
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
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(DeleteActions.PreviousWord));
					
				case Gdk.Key.Delete:
				case Gdk.Key.KP_Delete:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(DeleteActions.NextWord));
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
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(MiscActions.RemoveTab));
					
				case Gdk.Key.BackSpace:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(DeleteActions.Backspace));

				case Gdk.Key.Return:
				case Gdk.Key.KP_Enter:
					return ViMotionResult.DoMotion(ViMotionContext.ViDataToContext(MiscActions.InsertNewLine));
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
