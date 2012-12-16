//
// ViMotionResult.cs
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

namespace Mono.TextEditor.Vi
{
	public class ViMotionResult
	{
		public int Line;
		public int Column;

		public ViMotionResult (int line, int column)
		{
			this.Line = line;
			this.Column = column;
		}

		public ViMotionResult(ViMotionContext context)
		{
			this.Line = context.Data.Caret.Line;
			this.Column = context.Data.Caret.Column;
		}

		public TextEditorData ApplyTo (TextEditorData data)
		{
			data.Caret.Line = this.Line;
			data.Caret.Column = this.Column;
			return data;
		}

		public static Func<ViMotionContext, ViMotionResult> DoMotion (Action<ViMotionContext> motion)
		{
			return (ViMotionContext context) => { 
				int count = context.Count ?? 1;
				ViMotionResult result = null;
				for (int i = 0; i < count; i++)
				{
					if (context.StartingLine.HasValue) context.Data.Caret.Line = context.StartingLine.Value;
					if (context.StartingColumn.HasValue) context.Data.Caret.Column = context.StartingColumn.Value;
					int line = context.Data.Caret.Line;
					int column = context.Data.Caret.Column; 
					motion(context);
					result = new ViMotionResult(context.Data.Caret.Line, context.Data.Caret.Column);
					context.StartingLine = result.Line;
					context.StartingColumn = result.Column;
					context.Data.Caret.Line = line;
					context.Data.Caret.Column = column;
				}
				context.StartingLine = null;
				context.StartingColumn = null;
				return result;
			};
		}
	}
}

