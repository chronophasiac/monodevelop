// 
// ViTests.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using NUnit.Framework;
using System.Reflection;
using Mono.TextEditor.Vi;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	public class ViTests : TextEditorTestBase
	{
		[Test]
		public void ColumnMotion ()
		{
			var mode = new TestViEditMode () { Text = "Test\nText" };
			Assert.AreEqual (1, mode.Col);
			mode.Input ('l');
			Assert.AreEqual (2, mode.Col);
			mode.Input ("ll");
			Assert.AreEqual (4, mode.Col);
			mode.Input ('l');
			Assert.AreEqual (4, mode.Col);
			mode.Input ("hh");
			Assert.AreEqual (2, mode.Col);
			mode.Input ('h');
			Assert.AreEqual (1, mode.Col);
			mode.Input ('h');
			Assert.AreEqual (1, mode.Col);
		}
		
		[Test]
		public void LineMotion ()
		{
			var mode = new TestViEditMode () { Text =
@"abc def
ghy jklmn op
qrstu
vwxyz",
			};
			mode.Caret.Offset = 0;
			Assert.AreEqual (1, mode.Line);
			mode.Input ('j');
			Assert.AreEqual (2, mode.Line);
			mode.Input ('k');
			Assert.AreEqual (1, mode.Line);
			mode.Input ("jjjj");
			Assert.AreEqual (4, mode.Line);
			mode.Caret.Line = 2;
			mode.Caret.Column = 12;
			mode.Input ('j');
			Assert.AreEqual (5, mode.Col);
			mode.Input ('k');
			Assert.AreEqual (12, mode.Col);
		}
		
		[Test]
		public void ChangeWord ()
		{
			var mode = new TestViEditMode () { Text =
@"abc def
ghy jklmn op
qrstu",
			};
			mode.Caret.Offset = 0;
			mode.Input ("jwcwhi");
			Assert.AreEqual ("ghy hi op", mode.GetLine ());
		}
		
		[Test]
		public void DeleteWord ()
		{
			var mode = new TestViEditMode () { Text =
@"abc def
ghy jklmn op
qrstu",
			};
			mode.Caret.Offset = 0;
			mode.Input ("jwlldw");
			Assert.AreEqual ("ghy jkop", mode.GetLine ());
		}
		
		[Test]
		public void DeleteLine ()
		{
			var mode = new TestViEditMode () { Text =
@"   aaa aaa
   bbb bbb
   ccc ccc",
			};
			mode.Input ("jwlldd");
			Assert.AreEqual (2, mode.Line);
			Assert.AreEqual (4, mode.Col);
			Assert.AreEqual (
@"   aaa aaa
   ccc ccc", mode.Text);
		}
		
		[Test]
		public void ChangeLine ()
		{
			var mode = new TestViEditMode () { Text =
@"   aaa aaa
   bbb bbb
   ccc ccc",
			};
			mode.Input ("jwllcceeee");
			Assert.AreEqual (
@"   aaa aaa
   eeee
   ccc ccc", mode.Text);
		}
		
		[Ignore("FixMe")]
		[Test]
		public void LineDeletePaste ()
		{
			var mode = new TestViEditMode () { Text =
@"   aaa aaa
   bbb bbb
   ccc ccc
   eee eee
   fff fff
   ggg ggg
   hhh hhh",
			};
			//move down, enter visual line mode, move down twice -> lines 2/3/4 selected -> delete
			mode.Input ("jjVjjd");
			Assert.AreEqual (
@"   aaa aaa
   bbb bbb
   ggg ggg
   hhh hhh", mode.Text);
			//enter visual line mode, move down once -> lines 1/2 selected -> delete
   			mode.Input ("Vkd");
			Assert.AreEqual (
@"   aaa aaa
   hhh hhh", mode.Text);
			//paste last delete below current line (1)
   			mode.Input ("p");
			Assert.AreEqual (
@"   aaa aaa
   hhh hhh
   bbb bbb
   ggg ggg", mode.Text);
			//paste last delete above current line (3)
   			mode.Input ("P");
			Assert.AreEqual (
@"   aaa aaa
   hhh hhh
   bbb bbb
   bbb bbb
   ggg ggg
   ggg ggg", mode.Text);
			Assert.AreEqual (5, mode.Line);
			//movement to/across boundaries, check selection still okay by deleting lines 1/2/3
			mode.Input ("kVlllllhhhhhhhhhhjjjhhhhhjjjjkkkkkkkkkkkkjd");
			Assert.AreEqual (
@"   aaa aaa
   ggg ggg
   ggg ggg", mode.Text);
		}
		
		[Ignore("Got broken because 'RemoveTrailingWhitespaces' option was removed.")]
		[Test]
		public void DeleteToLineBoundary ()
		{
			var mode = new TestViEditMode () { Text =
@"   aaa bbb ccc ddd
   eee fff ggg hhh
   iii jjj kkk lll",
			};
			//move 3 words, delete to line end
			mode.Input ("wwwd$");
			Assert.AreEqual (
@"   aaa bbb 
   eee fff ggg hhh
   iii jjj kkk lll", mode.Text);
			//move down to 0 position, move 3 words, delete to home
			mode.Input ("j0wwwd^");
			Assert.AreEqual (
@"   aaa bbb
   ggg hhh
   iii jjj kkk lll", mode.Text);
			//move down to 0 position, move 3 words, delete to 0
			mode.Input ("j0wwwd0");
			Assert.AreEqual (
@"   aaa bbb
   ggg hhh
kkk lll", mode.Text);
		}
		
		[Test]
		public void VisualMotion ()
		{
			var mode = new TestViEditMode () { Text =
@"    aaa bbb ccc ddd
   eee fff ggg hhh
   iii jjj kkk lll
   mmm nnn ooo ppp
   qqq rrr sss ttt",
			};
			//move 2 lines down, 2 words in, enter visual mode
			mode.Input ("jjwwv");
			mode.AssertSelection (3, 8, 3, 9);
			//2 letters to right
			mode.Input ("ll");
			mode.AssertSelection (3, 8, 3, 11);
			//4 letters to left
			mode.Input ("hhhh");
			mode.AssertSelection (3, 9, 3, 6);
			//1 line up
			mode.Input ("k");
			mode.AssertSelection (3, 9, 2, 6);
			//1 line up
			mode.Input ("k");
			mode.AssertSelection (3, 9, 1, 6);
			//5 letters to right
			mode.Input ("lllll");
			mode.AssertSelection (3, 9, 1, 11);
			//3 lines down
			mode.Input ("jjj");
			mode.AssertSelection (3, 8, 4, 12);
		}
		
		[Test]
		public void KeyNotationRoundTrip ()
		{
			string command = "<C-m>av2f<Space>34<Esc><M-Space><S-C-M-Down>";
			var keys = ViKeyNotation.Parse (command);
			Assert.IsNotNull (keys);
			Assert.AreEqual (11, keys.Count);
			var s = ViKeyNotation.ToString (keys);
			Assert.AreEqual (command, s);

		}

		public class ExternalViTest
		{
			public List<string> Text = new List<string>();
			public List<string> Input = new List<string>();
			public string Name;
			public string Level;
		}

		[Test, TestCaseSource (typeof(ExternalViTestFactory),"ExternalViTestCases")]
		public void RunExternalViTest (ExternalViTest test)
		{
			for (int i = 0; i < test.Input.Count; i++)
			{
				string[] sep = {"\n"};
				string[] stringArray = test.Text[i].Split(sep, StringSplitOptions.None);
				int col = 1;
				int line = 1;
				foreach (string s in stringArray)
				{
					col = s.IndexOf('|') + 1;
					if (col > 0)
					{
						break;
					}
					line++;
				}
				var mode = new TestViEditMode () { Text = test.Text[i].Replace("|", null) };
				mode.Caret.Line = line;
				mode.Caret.Column = col;
				for (int j = 0; j < test.Input[i].Length; j++)
				{
					if (test.Input[i][j] == '\\')
					{
						j++;
						if (test.Input[i][j] == '<')
						{
							StringBuilder sb = new StringBuilder();
							j++;
							do
							{
								sb.Append(test.Input[i][j]);
								j++;
							}
							while (test.Input[i][j] != '>');
							mode = TestViEditMode.ParseViTestKeypress(mode, sb.ToString());
						}
						else if (test.Input[i][j] == 'n')
						{
							mode.Input(Gdk.Key.Return, 0, Gdk.ModifierType.None);
						}
						else throw new Exception("Unrecognized escape sequence:" + test.Input[i]);
					}
					else 
					{
						mode.Input(test.Input[i][j]);
						if (mode.Caret.Offset < 0) mode.Caret.Offset = 0;
					}
				}
				mode.Text = mode.Text.Insert(mode.Caret.Offset, "|");
				Assert.AreEqual(test.Text[i + 1], mode.Text);
			}
		}

		public class ExternalViTestFactory
		{
			public static IEnumerable ExternalViTestCases
			{
				get
				{
					List<ExternalViTest> tests = TestViEditMode.ParseExternalViTests ();
					foreach (ExternalViTest test in tests) 
					{
						yield return new TestCaseData(test)
							.Returns (null)
							.SetName(test.Level + " " + test.Name);
					}
				}
			}
		}

	}
	
	class TestViEditMode : ViEditMode
	{
		public TestViEditMode () : this (new TextEditorData ())
		{
			Data.Options.WordFindStrategy = new Mono.TextEditor.Vi.ViWordFindStrategy ();
		}
		
		//used to prevent edit actions from the HandleKeypress causing Caret/SelectionPositionChanged
		bool inputting;
		
		public TestViEditMode (TextEditorData data)
		{
			data.CurrentMode = this;
			textEditorData = data;
			// Currently doesn't work on my mac (test doesn't terminate with mono 2.6.7).
		//	var f = typeof (EditMode).GetField ("textEditorData", BindingFlags.NonPublic | BindingFlags.Instance);
		//	f.SetValue (this, data);
			
			data.Caret.PositionChanged += delegate(object sender, DocumentLocationEventArgs e) {
				if (!inputting)
					this.CaretPositionChanged ();
			};
			data.SelectionChanged += delegate(object sender, EventArgs e) {
				if (!inputting)
					this.SelectionChanged ();
			};
		}
		
		public new TextEditorData Data {
			get { return base.Data; }
		}
		
		public new TextDocument Document {
			get { return base.Document; }
		}
		
		public new Caret Caret {
			get { return base.Caret; }
		}

		public new TextEditor Editor {
			get { return base.Editor; }
		}

		public int Col {
			get { return Caret.Column; }
		}
		
		public int Line {
			get { return Caret.Line; }
		}
		
		public string Text {
			get { return Data.Document.Text; }
			set { Data.Document.Text = value; }
		}
		
		public string GetLine ()
		{
			return GetLine (Line);
		}
		
		public string GetLine (int line)
		{
			var seg = Document.GetLine (line);
			return Document.GetTextAt (seg.Offset, seg.Length);
		}
		
		public char GetChar (int offset)
		{
			return Document.GetCharAt (offset);
		}
		
		public char GetChar ()
		{
			return Document.GetCharAt (Caret.Offset);
		}
		
		public void Input (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			inputting = true;
			this.editor =  new TextEditor(Data.Document); 
			HandleKeypress (key, unicodeKey, modifier);
			this.editor = null;
			inputting = false;
		}
		
		public void Input (char unicodeKey)
		{
			Input ((Gdk.Key)0, unicodeKey, Gdk.ModifierType.None);
		}
		
		public void Input (string sequence)
		{
			foreach (char c in sequence)
				Input ((Gdk.Key)0, c, Gdk.ModifierType.None);
		}
		
		public void AssertSelection (int anchorLine, int anchorCol, int leadLine, int leadCol)
		{
			var sel = Data.MainSelection;
			Assert.IsNotNull (sel);
			Assert.AreEqual (anchorLine, sel.Anchor.Line);
			Assert.AreEqual (anchorCol, sel.Anchor.Column);
			Assert.AreEqual (leadLine, sel.Lead.Line);
			Assert.AreEqual (leadCol, sel.Lead.Column);
		}

		public static List<ViTests.ExternalViTest> ParseExternalViTests ()
		{
			List<ViTests.ExternalViTest> parsedTests = new List<ViTests.ExternalViTest>();
			List<string> testStrings = ReadExternalViTests();
			string[] sep = {"\n"};
			foreach (string testString in testStrings)
			{
				string[] testArray = testString.Split (sep, StringSplitOptions.None);
				bool newline = false;
				StringBuilder textSB = new StringBuilder();
				ViTests.ExternalViTest test = new ViTests.ExternalViTest();
				string level = "";
				foreach (string s in testArray) 
				{
					if (!String.IsNullOrEmpty(s) && (s[0] != '#')) 
					{
						if (s.StartsWith(":level"))
						{
							level = s.Remove(0,7);
						}

						else if (s.StartsWith(":start"))
					    {
							test = new ViTests.ExternalViTest();
							test.Name = s.Remove(0,7);
						}

						else if (s.StartsWith(":type"))
						{
							test.Text.Add(textSB.ToString());
							textSB.Clear();
							test.Input.Add(s.Remove(0,6));
							newline = false;
						}

						else if (s.StartsWith(":end"))
						{
							test.Text.Add(textSB.ToString());
							textSB.Clear();
							test.Level = level;
							parsedTests.Add(test);
							newline = false;
						}

						else 
						{
							if (newline) textSB.Append("\n");
							//Append text, stripping out indent
							textSB.Append(s.Remove(0,1));
							newline = true;
						}
					}
				}
			}
			return parsedTests;
		}

		public static TestViEditMode ParseViTestKeypress(TestViEditMode mode, string key)
		{
			switch (key)
			{
			case "esc":
				mode.Input(Gdk.Key.Escape, 0, Gdk.ModifierType.None);
				break;
			case "NL":
				mode.Input(Gdk.Key.Return, 0, Gdk.ModifierType.None);
				break;
			default:
				throw new Exception("Unrecognized keypress:" + key);
			}
			return mode;
		}

		public static List<string> ReadExternalViTests()
		{
			List<string> tests = new List<string>();
			string[] subdirs = Directory.GetDirectories(@"Mono.TextEditor.Tests/ExternalViModeTests");
			foreach (string subdir in subdirs)
			{
				string[] files = Directory.GetFiles(subdir);
				foreach (string file in files)
				{
					using (StreamReader sr = new StreamReader(file))
					{
						StringBuilder sb = new StringBuilder();
						string[] fileName = subdir.Split('/');
						string level = fileName[fileName.Length - 1];
						sb.Append(":level " + level + "\n");
						sb.Append(sr.ReadToEnd());
						tests.Add(sb.ToString());
					}
				}
			}
			return tests;
		}

	}
}

