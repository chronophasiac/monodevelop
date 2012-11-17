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

namespace Mono.TextEditor.Vi
{
	
	
	public partial class ViMotionsAndCommands
	{
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
		
		public static void Right (ViMotionContext context)
		{
			DocumentLine segment = context.Data.Document.GetLine (context.Data.Caret.Line);
			if (segment.EndOffsetIncludingDelimiter-1 > context.Data.Caret.Offset) {
				CaretMoveActions.Right (context.Data);
				ViEditMode.RetreatFromLineEnd (context.Data);
			}
		}
		
		public static void Left (ViMotionContext context)
		{
			if (DocumentLocation.MinColumn < context.Data.Caret.Column) {
				CaretMoveActions.Left (context.Data);
			}
		}
		
		public static void Down (ViMotionContext context)
		{
			int desiredColumn = System.Math.Max (context.Data.Caret.Column, context.Data.Caret.DesiredColumn);
			
			CaretMoveActions.Down (context.Data);
			ViEditMode.RetreatFromLineEnd (context.Data);
			
			context.Data.Caret.DesiredColumn = desiredColumn;
		}
		
		public static void Up (ViMotionContext context)
		{
			int desiredColumn = System.Math.Max (context.Data.Caret.Column, context.Data.Caret.DesiredColumn);
			
			CaretMoveActions.Up (context.Data);
			ViEditMode.RetreatFromLineEnd (context.Data);
			
			context.Data.Caret.DesiredColumn = desiredColumn;
		}
		
		public static void WordEnd (ViMotionContext context)
		{
			context.Data.Caret.Offset = context.Data.FindCurrentWordEnd (context.Data.Caret.Offset);
		}
		
		public static void WordStart (ViMotionContext context)
		{
			context.Data.Caret.Offset = context.Data.FindCurrentWordStart (context.Data.Caret.Offset);
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
