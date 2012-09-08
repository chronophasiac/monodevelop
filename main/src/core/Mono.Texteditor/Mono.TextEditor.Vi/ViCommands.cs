//
// ViCommands.cs
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

namespace Mono.TextEditor.Vi
{
	
	
	public partial class ViMotionsAndCommands
	{
		public static void NewLineBelow (TextEditorData data)
		{
			DocumentLine currentLine = data.Document.GetLine (data.Caret.Line);
			data.Caret.Offset = currentLine.Offset + currentLine.Length;
			MiscActions.InsertNewLine (data);
		}
		
		public static void NewLineAbove (TextEditorData data)
		{
			if (data.Caret.Line == DocumentLocation.MinLine ) {
				data.Caret.Offset = 0;
				MiscActions.InsertNewLine (data);
				data.Caret.Offset = 0;
				return;
			}
			
			DocumentLine currentLine = data.Document.GetLine (data.Caret.Line - 1);
			data.Caret.Offset = currentLine.Offset + currentLine.Length;
			MiscActions.InsertNewLine (data);
		}

		public static void Join (TextEditorData data)
		{
			int startLine, endLine, startOffset, length;
			
			if (data.IsSomethingSelected) {
				startLine = data.Document.OffsetToLineNumber (data.SelectionRange.Offset);
				endLine = data.Document.OffsetToLineNumber (data.SelectionRange.EndOffset - 1);
			} else {
				startLine = endLine = data.Caret.Line;
			}
			
			//single-line joins
			if (endLine == startLine)
				endLine++;
			
			if (endLine > data.Document.LineCount)
				return;
			
			DocumentLine seg = data.Document.GetLine (startLine);
			startOffset = seg.Offset;
			StringBuilder sb = new StringBuilder (data.Document.GetTextAt (seg).TrimEnd ());
			//lastSpaceOffset = startOffset + sb.Length;
			
			for (int i = startLine + 1; i <= endLine; i++) {
				seg = data.Document.GetLine (i);
				//lastSpaceOffset = startOffset + sb.Length;
				sb.Append (" ");
				sb.Append (data.Document.GetTextAt (seg).Trim ());
			}
			length = (seg.Offset - startOffset) + seg.Length;
			// TODO: handle conversion issues ? 
			data.Replace (startOffset, length, sb.ToString ());
		}

		public static void ToggleCase (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				if (!data.CanEditSelection)
					return;
				
				StringBuilder sb = new StringBuilder (data.SelectedText);
				for (int i = 0; i < sb.Length; i++) {
					char ch = sb [i];
					if (Char.IsLower (ch))
						sb [i] = Char.ToUpper (ch);
					else if (Char.IsUpper (ch))
						sb [i] = Char.ToLower (ch);
				}
				data.Replace (data.SelectionRange.Offset, data.SelectionRange.Length, sb.ToString ());
			} else if (data.CanEdit (data.Caret.Line)) {
				char ch = data.Document.GetCharAt (data.Caret.Offset);
				if (Char.IsLower (ch))
					ch = Char.ToUpper (ch);
				else if (Char.IsUpper (ch))
					ch = Char.ToLower (ch);
				var caretOffset = data.Caret.Offset;
				int length = data.Replace (caretOffset, 1, new string (ch, 1));
				DocumentLine seg = data.Document.GetLine (data.Caret.Line);
				if (data.Caret.Column < seg.Length)
					data.Caret.Offset = caretOffset + length;
			}
		}
	}
}
