// 
// ViActions.cs
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
using System.Linq;
using Mono.TextEditor;

namespace Mono.TextEditor.Vi
{
	
	
	public partial class ViMotionsAndCommands
	{
		private static int PersistColumn = 0;

		public static void MoveToNextEmptyLine (ViMotionContext context)
		{
			if (context.Data.Caret.Line == context.Data.Document.LineCount) {
				context.Data.Caret.Offset = context.Data.Document.TextLength;
				return;
			}
			
			int line = context.Data.Caret.Line + 1;
			DocumentLine currentLine = context.Data.Document.GetLine (line);
			while (line <= context.Data.Document.LineCount) {
				line++;
				DocumentLine nextLine = context.Data.Document.GetLine (line);
				if (currentLine.Length != 0 && nextLine.Length == 0) {
					context.Data.Caret.Offset = nextLine.Offset;
					return;
				}
				currentLine = nextLine;
			}
			
			context.Data.Caret.Offset = currentLine.Offset;
		}
		
		public static void MoveToPreviousEmptyLine (ViMotionContext context)
		{
			if (context.Data.Caret.Line == DocumentLocation.MinLine) {
				context.Data.Caret.Offset = 0;
				return;
			}
			
			int line = context.Data.Caret.Line - 1;
			DocumentLine currentLine = context.Data.Document.GetLine (line);
			while (line > DocumentLocation.MinLine) {
				line--;
				DocumentLine previousLine = context.Data.Document.GetLine (line);
				if (currentLine.Length != 0 && previousLine.Length == 0) {
					context.Data.Caret.Offset = previousLine.Offset;
					return;
				}
				currentLine = previousLine;
			}
			
			context.Data.Caret.Offset = currentLine.Offset;
		}
		
		public static ViMotionResult Right (ViMotionContext context)
		{
			ViMotionResult result = new ViMotionResult(context);
			DocumentLine currentLine = context.Data.Document.GetLine (context.Data.Caret.Line);
			if (currentLine.EndOffsetIncludingDelimiter-1 > context.Data.Caret.Offset) {
				result.Column += (context.Count ?? 1);
				if (result.Column > currentLine.Length)
					result.Column = currentLine.Length;
				result = ViEditMode.RetreatFromLineEnd (context, result);
			}
			return result;
		}

		public static ViMotionResult Left (ViMotionContext context)
		{
			ViMotionResult result = new ViMotionResult(context);
			result.Column -= (context.Count ?? 1);
			if (result.Column < 1) 
				result.Column = 1;
			return result;
		}

		public static ViMotionResult Down (ViMotionContext context)
		{
			var result = new ViMotionResult(context);
			var data = context.Data;
			PersistColumn = System.Math.Max(PersistColumn, result.Column);
			result.Line += (context.Count ?? 1);
			if (result.Line >= data.LineCount)
				result.Line = data.LineCount - 1;
			ViEditMode.RetreatFromLineEnd (context, result);
			DocumentLine line = data.Document.GetLine (result.Line);
			if ((PersistColumn != result.Column) && (PersistColumn <= line.Length))
			{
				result.Column = PersistColumn;
				PersistColumn = 0;
			}
			return result;	
		}
		
		public static ViMotionResult Up (ViMotionContext context)
		{
			var result = new ViMotionResult(context);
			var data = context.Data;
			PersistColumn = System.Math.Max(PersistColumn, result.Column);
			result.Line -= (context.Count ?? 1);
			if (result.Line < 1)
				result.Line = 1;
			ViEditMode.RetreatFromLineEnd (context, result);
			DocumentLine line = data.Document.GetLine (result.Line);
			if ((PersistColumn != result.Column) && (PersistColumn <= line.Length))
			{
				result.Column = PersistColumn;
				PersistColumn = 0;
			}
			return result;	
		}

		public static ViMotionResult NextSubword (ViMotionContext context)
		{
			var result = new ViMotionResult(context);
			TextDocument doc = context.Data.Document;
			for (int i=0; i < context.Count; i++)
			{
				int offset = doc.LocationToOffset(result.Line, result.Column);
				offset = context.Data.FindNextSubwordOffset (offset);
				DocumentLocation loc = doc.OffsetToLocation(offset);
				result.Line = loc.Line;
				result.Column = loc.Column;
			}
			return result;
		}

		public static ViMotionResult NextWord (ViMotionContext context)
		{
			var result = new ViMotionResult(context);
			TextDocument doc = context.Data.Document;
			for (int i=0; i < context.Count; i++)
			{
				int offset = doc.LocationToOffset(result.Line, result.Column);
				offset = context.Data.FindNextWordOffset (offset);
				DocumentLocation loc = doc.OffsetToLocation(offset);
				result.Line = loc.Line;
				result.Column = loc.Column;
			}
			return result;
		}

		public static ViMotionResult PreviousWord (ViMotionContext context)
		{
			var result = new ViMotionResult(context);
			TextDocument doc = context.Data.Document;
			for (int i=0; i < context.Count; i++)
			{
				int offset = doc.LocationToOffset(result.Line, result.Column);
				offset = context.Data.FindPrevWordOffset (offset);
				DocumentLocation loc = doc.OffsetToLocation(offset);
				result.Line = loc.Line;
				result.Column = loc.Column;
			}
			return result;
		}
		
		public static ViMotionResult PreviousSubword (ViMotionContext context)
		{
			var result = new ViMotionResult(context);
			TextDocument doc = context.Data.Document;
			for (int i=0; i < context.Count; i++)
			{
				int offset = doc.LocationToOffset(result.Line, result.Column);
				offset = context.Data.FindPrevSubwordOffset (offset);
				DocumentLocation loc = doc.OffsetToLocation(offset);
				result.Line = loc.Line;
				result.Column = loc.Column;
			}
			return result;
		}
		
		public static ViMotionResult WordEnd (ViMotionContext context)
		{
			var result = new ViMotionResult(context);
			TextDocument doc = context.Data.Document;
			for (int i=0; i < context.Count; i++)
			{
				int offset = doc.LocationToOffset(result.Line, result.Column);
				offset = context.Data.FindCurrentWordEnd (offset);
				DocumentLocation loc = doc.OffsetToLocation(offset);
				result.Line = loc.Line;
				result.Column = loc.Column;
			}
			return result;
		}
		
		public static ViMotionResult WordStart (ViMotionContext context)
		{
			var result = new ViMotionResult(context);
			TextDocument doc = context.Data.Document;
			for (int i=0; i < context.Count; i++)
			{
				int offset = doc.LocationToOffset(result.Line, result.Column);
				offset = context.Data.FindCurrentWordStart (offset);
				DocumentLocation loc = doc.OffsetToLocation(offset);
				result.Line = loc.Line;
				result.Column = loc.Column;
			}
			return result;
		}
		
		public static void LineEnd (ViMotionContext context)
		{
			int desiredColumn = System.Math.Max (context.Data.Caret.Column, context.Data.Caret.DesiredColumn);
			
			CaretMoveActions.LineEnd (context.Data);
			ViEditMode.RetreatFromLineEnd (context.Data);
			
			context.Data.Caret.DesiredColumn = desiredColumn;
		}

	}
}
