//
// ViMode.cs
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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Mono.TextEditor.Vi
{
	public class NewViEditMode : EditMode
	{
		protected ViEditor ViEditor { get ; private set ;}
		
		public NewViEditMode ()
		{
			ViEditor = new ViEditor (this);
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			ViEditor.ProcessKey (modifier, key, (char)unicodeKey);
		}
		
		public new TextEditor Editor { get { return base.Editor; } }
		public new TextEditorData Data { get { return base.Data; } }
		
		public override bool WantsToPreemptIM {
			get {
				switch (ViEditor.Mode) {
				case ViEditorMode.Insert:
				case ViEditorMode.Replace:
					return false;
				case ViEditorMode.Normal:
				case ViEditorMode.Visual:
				case ViEditorMode.VisualLine:
				default:
					return true;
				}
			}
		}
		
		protected override void OnAddedToEditor (TextEditorData data)
		{
			ViEditor.SetMode (ViEditorMode.Normal);
			SetCaretMode (CaretMode.Block, data);
			ViEditMode.RetreatFromLineEnd (data);
		}
		
		protected override void OnRemovedFromEditor (TextEditorData data)
		{
			SetCaretMode (CaretMode.Insert, data);
		}
		
		protected override void CaretPositionChanged ()
		{
			ViEditor.OnCaretPositionChanged ();
		}
		
		public void SetCaretMode (CaretMode mode)
		{
			SetCaretMode (mode, Data);
		}
		
		static void SetCaretMode (CaretMode mode, TextEditorData data)
		{
			if (data.Caret.Mode == mode)
				return;
			data.Caret.Mode = mode;
			data.Document.RequestUpdate (new SinglePositionUpdate (data.Caret.Line, data.Caret.Column));
			data.Document.CommitDocumentUpdate ();
		}
	}
	
	public partial class ViEditMode : EditMode
	{
		bool searchBackward;
		static string lastPattern;
		static string lastReplacement;
		State state;
		const string substMatch = @"^:s(?<sep>.)(?<pattern>.+?)\k<sep>(?<replacement>.*?)(\k<sep>(?<trailer>i?))?$";
		StringBuilder commandBuffer = new StringBuilder ();
		Dictionary<char,ViMark> marks = new Dictionary<char, ViMark>();
		Dictionary<char,ViMacro> macros = new Dictionary<char, ViMacro>();
		char macros_lastplayed = '@'; // start with the illegal macro character
		string statusText = "";
		public static int? count;

		/// <summary>
		/// The macro currently being implemented. Will be set to null and checked as a flag when required.
		/// </summary>
		ViMacro currentMacro;
		
		public virtual string Status {
		
			get {
				return statusText;
			}
			
			protected set {
				if (currentMacro == null) {
					statusText = value;
				} else {
					statusText = value + " recording";
				}
			}
	
		}
		
		protected virtual string RunExCommand (string command)
		{
			switch (command[0]) {
			case ':':
				if (2 > command.Length)
					break;
					
				int line;
				if (int.TryParse (command.Substring (1), out line)) {
					if (line < DocumentLocation.MinLine || line > Data.Document.LineCount) {
						return "Invalid line number.";
					} else if (line == 0) {
						RunActions (CaretMoveActions.ToDocumentStart);
						return "Jumped to beginning of document.";
					}
					
					Data.Caret.Line = line;
					Editor.ScrollToCaret ();
					return string.Format ("Jumped to line {0}.", line);
				}
	
				switch (command[1]) {
				case 's':
					if (2 == command.Length) {
						if (null == lastPattern || null == lastReplacement)
							return "No stored pattern.";
							
						// Perform replacement with stored stuff
						command = string.Format (":s/{0}/{1}/", lastPattern, lastReplacement);
					}
		
					var match = Regex.Match (command, substMatch, RegexOptions.Compiled);
					if (!(match.Success && match.Groups["pattern"].Success && match.Groups["replacement"].Success))
						break;
		
					return RegexReplace (match);
					
				case '$':
					if (command.Length == 2) {
						RunActions (CaretMoveActions.ToDocumentEnd);
						return "Jumped to end of document.";
					}
					break;	
				}
				break;
				
			case '?':
			case '/':
				searchBackward = ('?' == command[0]);
				if (1 < command.Length) {
					Editor.HighlightSearchPattern = true;
					Editor.SearchEngine = new RegexSearchEngine ();
					var pattern = command.Substring (1);
					Editor.SearchPattern = pattern;
					var caseSensitive = pattern.ToCharArray ().Any (c => char.IsUpper (c));
					Editor.SearchEngine.SearchRequest.CaseSensitive = caseSensitive;
				}
				return Search ();
			}
			
			return "Command not recognised";
		}
		
		void SearchWordAtCaret ()
		{
			Editor.SearchEngine = new RegexSearchEngine ();
			var s = Data.FindCurrentWordStart (Data.Caret.Offset);
			var e = Data.FindCurrentWordEnd (Data.Caret.Offset);
			if (s < 0 || e <= s)
				return;
			
			var word = Document.GetTextBetween (s, e);
			//use negative lookahead and lookbehind for word characters to make sure we only get fully matching words
			word = "(?<!\\w)" + System.Text.RegularExpressions.Regex.Escape (word) + "(?!\\w)";
			Editor.SearchPattern = word;
			Editor.SearchEngine.SearchRequest.CaseSensitive = true;
			searchBackward = false;
			Search ();
		}
		
		public override bool WantsToPreemptIM {
			get {
				return state != State.Insert && state != State.Replace;
			}
		}
		
		protected override void SelectionChanged ()
		{
			if (Data.IsSomethingSelected) {
				state = ViEditMode.State.Visual;
				Status = "-- VISUAL --";
			} else if (state == State.Visual && !Data.IsSomethingSelected) {
				Reset ("");
			}
		}
		
		protected override void CaretPositionChanged ()
		{
			if (state == State.Replace || state == State.Insert || state == State.Visual)
				return;
			else if (state == ViEditMode.State.Normal || state == ViEditMode.State.Unknown)
				RetreatFromLineEnd (Data);
			else
				Reset ("");
		}
		
		void CheckVisualMode ()
		{
			if (state == ViEditMode.State.Visual || state == ViEditMode.State.Visual) {
				if (!Data.IsSomethingSelected)
					state = ViEditMode.State.Normal;
			} else {
				if (Data.IsSomethingSelected) {
					state = ViEditMode.State.Visual;
					Status = "-- VISUAL --";
				}
			}
		}
		
		void ResetEditorState (TextEditorData data)
		{
			if (data == null)
				return;
			data.ClearSelection ();
			
			//Editor can be null during GUI-less tests
			// Commenting this fixes bug: Bug 622618 - Inline search fails in vi mode
//			if (Editor != null)
//				Editor.HighlightSearchPattern = false;
			
			if (CaretMode.Block != data.Caret.Mode) {
				data.Caret.Mode = CaretMode.Block;
				if (data.Caret.Column > DocumentLocation.MinColumn)
					data.Caret.Column--;
			}
			RetreatFromLineEnd (data);
		}
		
		protected override void OnAddedToEditor (TextEditorData data)
		{
			data.Caret.Mode = CaretMode.Block;
			RetreatFromLineEnd (data);
		}
		
		protected override void OnRemovedFromEditor (TextEditorData data)
		{
			data.Caret.Mode = CaretMode.Insert;
		}
		
		void Reset (string status)
		{
			state = State.Normal;
			ResetEditorState (Data);
			
			commandBuffer.Length = 0;
			Status = status;
		}
		
		protected virtual Func<ViMotionContext, ViMotionResult> GetInsertAction (Gdk.Key key, Gdk.ModifierType modifier)
		{
			return ViActionMaps.GetInsertKeyAction (key, modifier) ??
				ViActionMaps.GetDirectionKeyAction (key, modifier);
		}

		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
		
			// Reset on Esc, Ctrl-C, Ctrl-[
			if (key == Gdk.Key.Escape) {
				if (currentMacro != null) {
					// Record Escapes into the macro since it actually does something
					ViMacro.KeySet toAdd = new ViMacro.KeySet();
					toAdd.Key = key;
					toAdd.Modifiers = modifier;
					toAdd.UnicodeKey = unicodeKey;
					currentMacro.KeysPressed.Enqueue(toAdd);
				}
				Reset(string.Empty);
				return;
			} else if (((key == Gdk.Key.c || key == Gdk.Key.bracketleft) && (modifier & Gdk.ModifierType.ControlMask) != 0)) {
				Reset (string.Empty);
				if (currentMacro != null) {
					// Otherwise remove the macro from the pool
					macros.Remove(currentMacro.MacroCharacter);
					currentMacro = null;
				}
				return;
			} else if (currentMacro != null && !((char)unicodeKey == 'q' && modifier == Gdk.ModifierType.None)) {
				ViMacro.KeySet toAdd = new ViMacro.KeySet();
				toAdd.Key = key;
				toAdd.Modifiers = modifier;
				toAdd.UnicodeKey = unicodeKey;
				currentMacro.KeysPressed.Enqueue(toAdd);
			}
			
			switch (state) {
			case State.Unknown:
				Reset (string.Empty);
				goto case State.Normal;
			case State.Normal:     NormalStateHandleKeypress        (unicodeKey, modifier, key); return;
			case State.Delete:     DeleteStateHandleKeypress        (unicodeKey, modifier, key); return;
			case State.Yank:       YankStateHandleKeypress          (unicodeKey, modifier, key); return;
			case State.Change:     ChangeStateHandleKeypress        (unicodeKey, modifier, key); return;
			case State.Insert:
			case State.Replace:    InsertReplaceStateHandleKeypress (unicodeKey, modifier, key); return;
			case State.VisualLine: VisualLineStateHandleKeypress    (unicodeKey, modifier, key); return;
			case State.Visual:     VisualStateHandleKeypress        (unicodeKey, modifier, key); return;
			case State.Command:    CommandStateHandleKeypress       (unicodeKey, modifier, key); return;
			case State.WriteChar:  WriteCharStateHandleKeypress     (unicodeKey, modifier);      return;
			case State.Indent:     IndentStateHandleKeypress        (unicodeKey, modifier, key); return;
			case State.Unindent:   UnindentStateHandleKeypress      (unicodeKey, modifier, key); return;
			case State.G:          GStateHandleKeypress             (unicodeKey, modifier);      return;
			case State.Mark:       MarkStateHandleKeypress          (unicodeKey, modifier);      return;
			case State.NameMacro:  NameMacroStateHandleKeypress     (unicodeKey, modifier);      return;
			case State.PlayMacro:  PlayMacroStateHandleKeypress     (unicodeKey, modifier);      return;
			case State.GoToMark:   GoToMarkStateHandleKeypress      (unicodeKey, modifier);      return;
			case State.Fold:       FoldStateHandleKeypress          (unicodeKey, modifier);      return;
			}
		}


		/// <summary>
		/// Runs an in-place replacement on the selection or the current line
		/// using the "pattern", "replacement", and "trailer" groups of match.
		/// </summary>
		public string RegexReplace (System.Text.RegularExpressions.Match match)
		{
			string line = null;
			var segment = TextSegment.Invalid;

			if (Data.IsSomethingSelected) {
				// Operate on selection
				line = Data.SelectedText;
				segment = Data.SelectionRange;
			} else {
				// Operate on current line
				var lineSegment = Data.Document.GetLine (Caret.Line);
				if (lineSegment != null)
					segment = lineSegment;
				line = Data.Document.GetTextBetween (segment.Offset, segment.EndOffset);
			}

			// Set regex options
			RegexOptions options = RegexOptions.Multiline;
			if (match.Groups["trailer"].Success && "i" == match.Groups["trailer"].Value)
				options |= RegexOptions.IgnoreCase;

			// Mogrify group backreferences to .net-style references
			string replacement = Regex.Replace (match.Groups["replacement"].Value, @"\\([0-9]+)", "$$$1", RegexOptions.Compiled);
			replacement = Regex.Replace (replacement, "&", "$$0", RegexOptions.Compiled);

			try {
				string newline = Regex.Replace (line, match.Groups["pattern"].Value, replacement, options);
				Data.Replace (segment.Offset, line.Length, newline);
				if (Data.IsSomethingSelected)
					Data.ClearSelection ();
				lastPattern = match.Groups["pattern"].Value;
				lastReplacement = replacement; 
			} catch (ArgumentException ae) {
				return string.Format("Replacement error: {0}", ae.Message);
			}

			return "Performed replacement.";
		}

		public void ApplyActionToSelection (Gdk.ModifierType modifier, uint unicodeKey)
		{
			if (Data.IsSomethingSelected && (modifier & (Gdk.ModifierType.ControlMask)) == 0) {
				switch ((char)unicodeKey) {
				case 'x':
				case 'd':
					RunActions (ClipboardActions.Cut);
					Reset ("Deleted selection");
					return;
				case 'y':
					int offset = Data.SelectionRange.Offset;
					RunActions (ClipboardActions.Copy);
					Reset ("Yanked selection");
					Caret.Offset = offset;
					return;
				case 's':
				case 'c':
					RunActions (ClipboardActions.Cut);
					Caret.Mode = CaretMode.Insert;
					state = State.Insert;
					Status = "-- INSERT --";
					return;
				case 'S':
					Data.SetSelectLines (Data.MainSelection.Anchor.Line, Data.Caret.Line);
					goto case 'c';
					
				case '>':
					RunActions (MiscActions.IndentSelection);
					Reset ("");
					return;
					
				case '<':
					RunActions (MiscActions.RemoveIndentSelection);
					Reset ("");
					return;

				case ':':
					commandBuffer.Append (":");
					Status = commandBuffer.ToString ();
					state = State.Command;
					break;
				case 'J':
					RunActions (ViMotionsAndCommands.Join);
					Reset ("");
					return;
					
				case '~':
					RunActions (ViMotionsAndCommands.ToggleCase);
					Reset ("");
					return;
				}
			}
		}

		public static Func<ViMotionContext, ViMotionResult> VisualSelectionFromMotion (Func<ViMotionContext, ViMotionResult> motion)
		{
			return (ViMotionContext context) => {
				//get info about the old selection state
				DocumentLocation oldCaret = context.Data.Caret.Location, oldAnchor = oldCaret, oldLead = oldCaret;
				if (context.Data.MainSelection != null) {
					oldLead = context.Data.MainSelection.Lead;
					oldAnchor = context.Data.MainSelection.Anchor;
				}
				
				//do the action, preserving selection
				SelectionActions.StartSelection (context.Data);
				ViMotionResult res = motion (context);
				SelectionActions.EndSelection (context.Data);
				
				DocumentLocation newCaret = context.Data.Caret.Location, newAnchor = newCaret, newLead = newCaret;
				if (context.Data.MainSelection != null) {
					newLead = context.Data.MainSelection.Lead;
					newAnchor = context.Data.MainSelection.Anchor;
				}
				
				//Console.WriteLine ("oc{0}:{1} oa{2}:{3} ol{4}:{5}", oldCaret.Line, oldCaret.Column, oldAnchor.Line, oldAnchor.Column, oldLead.Line, oldLead.Column);
				//Console.WriteLine ("nc{0}:{1} na{2}:{3} nl{4}:{5}", newCaret.Line, newCaret.Line, newAnchor.Line, newAnchor.Column, newLead.Line, newLead.Column);
				
				//pivot the anchor around the anchor character
				if (oldAnchor < oldLead && newAnchor >= newLead) {
					context.Data.SetSelection (new DocumentLocation (newAnchor.Line, newAnchor.Column + 1), newLead);
				} else if (oldAnchor > oldLead && newAnchor <= newLead) {
					context.Data.SetSelection (new DocumentLocation (newAnchor.Line, newAnchor.Column - 1), newLead);
				}
				
				//pivot the lead about the anchor character
				if (newAnchor == newLead) {
					if (oldAnchor < oldLead)
						SelectionActions.FromMotion (ViMotionResult.DoMotion (ViMotionsAndCommands.Left)) (context);
					else
						SelectionActions.FromMotion (ViMotionResult.DoMotion (ViMotionsAndCommands.Right)) (context);
				}
				//pivot around the anchor line
				else {
					if (oldAnchor < oldLead && newAnchor > newLead && (
							(newLead.Line == newAnchor.Line && oldLead.Line == oldAnchor.Line + 1) ||
						    (newLead.Line == newAnchor.Line - 1 && oldLead.Line == oldAnchor.Line)))
						SelectionActions.FromMotion (ViMotionResult.DoMotion (ViMotionsAndCommands.Left)) (context);
					else if (oldAnchor > oldLead && newAnchor < newLead && (
							(newLead.Line == newAnchor.Line && oldLead.Line == oldAnchor.Line - 1) ||
							(newLead.Line == newAnchor.Line + 1 && oldLead.Line == oldAnchor.Line)))
						SelectionActions.FromMotion (ViMotionResult.DoMotion (ViMotionsAndCommands.Right)) (context);
				}
				return res;
			};
		}

		private string Search()
		{
			SearchResult result = searchBackward?
				Editor.SearchBackward (Caret.Offset):
				Editor.SearchForward (Caret.Offset+1);
			Editor.HighlightSearchPattern = (null != result);
			if (null == result) 
				return string.Format ("Pattern not found: '{0}'", Editor.SearchPattern);
			else Caret.Offset = result.Offset;
		
			return string.Empty;
		}
		
		internal static bool IsEol (char c)
		{
			return (c == '\r' || c == '\n');
		}
		
		internal static void RetreatFromLineEnd (TextEditorData data)
		{
			if (data.Caret.Mode == CaretMode.Block && !data.IsSomethingSelected && !data.Caret.PreserveSelection) {
				while (DocumentLocation.MinColumn < data.Caret.Column && (data.Caret.Offset >= data.Document.TextLength
				                                 || IsEol (data.Document.GetCharAt (data.Caret.Offset)))) {
					ViMotionsAndCommands.Left (new ViMotionContext(data));
				}
			}
		}

		/// <summary>
		/// Pastes the selection after the caret,
		/// or replacing an existing selection.
		/// </summary>
		private void PasteAfter (bool linemode)
		{
			TextEditorData data = Data;
			using (var undo = Document.OpenUndoGroup ()) {
				
				Gtk.Clipboard.Get (ClipboardActions.CopyOperation.CLIPBOARD_ATOM).RequestText 
					(delegate (Gtk.Clipboard cb, string contents) {
					if (contents == null)
						return;
					if (contents.EndsWith ("\r") || contents.EndsWith ("\n")) {
						// Line mode paste
						if (data.IsSomethingSelected) {
							// Replace selection
							RunActions (ClipboardActions.Cut);
							data.InsertAtCaret (data.EolMarker);
							int offset = data.Caret.Offset;
							data.InsertAtCaret (contents);
							if (linemode) {
								// Existing selection was also in line mode
								data.Caret.Offset = offset;
								RunActions (DeleteActions.FromMoveAction (CaretMoveActions.Left));
							}
							RunActions (CaretMoveActions.LineStart);
						} else {
							// Paste on new line
							RunActions (ViMotionsAndCommands.NewLineBelow);
							RunActions (DeleteActions.FromMoveAction (CaretMoveActions.LineStart));
							data.InsertAtCaret (contents);
							RunActions (DeleteActions.FromMoveAction (CaretMoveActions.Left));
							RunActions (CaretMoveActions.LineStart);
						}
					} else {
						// Inline paste
						if (data.IsSomethingSelected) 
							RunActions (ClipboardActions.Cut);
						else RunActions (CaretMoveActions.Right);
						data.InsertAtCaret (contents);
						RunMotions (ViMotionResult.DoMotion(ViMotionsAndCommands.Left));
					}
					Reset (string.Empty);
				});
			}
		}

		/// <summary>
		/// Pastes the selection before the caret,
		/// or replacing an existing selection.
		/// </summary>
		private void PasteBefore (bool linemode)
		{
			TextEditorData data = Data;
			
			using (var undo = Document.OpenUndoGroup ()) {
				Gtk.Clipboard.Get (ClipboardActions.CopyOperation.CLIPBOARD_ATOM).RequestText 
					(delegate (Gtk.Clipboard cb, string contents) {
					if (contents == null)
						return;
					if (contents.EndsWith ("\r") || contents.EndsWith ("\n")) {
						// Line mode paste
						if (data.IsSomethingSelected) {
							// Replace selection
							RunActions (ClipboardActions.Cut);
							data.InsertAtCaret (data.EolMarker);
							int offset = data.Caret.Offset;
							data.InsertAtCaret (contents);
							if (linemode) {
								// Existing selection was also in line mode
								data.Caret.Offset = offset;
								RunActions (DeleteActions.FromMoveAction (CaretMoveActions.Left));
							}
							RunActions (CaretMoveActions.LineStart);
						} else {
							// Paste on new line
							RunActions (ViMotionsAndCommands.NewLineAbove);
							RunActions (DeleteActions.FromMoveAction (CaretMoveActions.LineStart));
							data.InsertAtCaret (contents);
							RunActions (DeleteActions.FromMoveAction (CaretMoveActions.Left));
							RunActions (CaretMoveActions.LineStart);
						}
					} else {
						// Inline paste
						if (data.IsSomethingSelected) 
							RunActions (ClipboardActions.Cut);
						data.InsertAtCaret (contents);
						RunMotions (ViMotionResult.DoMotion(ViMotionsAndCommands.Left));
					}
					Reset (string.Empty);
				});
			}
		}

		enum State {
			Unknown = 0,
			Normal,
			Command,
			Delete,
			Yank,
			Visual,
			VisualLine,
			Insert,
			Replace,
			WriteChar,
			Change,
			Indent,
			Unindent,
			G,
			Fold,
			Mark,
			GoToMark,
			NameMacro,
			PlayMacro
		}

	}

}
