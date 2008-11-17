//  DefaultFind.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Collections;
using System.Diagnostics;

namespace MonoDevelop.Ide.Gui.Search
{
	internal class DefaultFind : IFind
	{
		ISearchStrategy searchStrategy;
		IDocumentIterator documentIterator;
		ITextIterator textIterator;
		IDocumentInformation info;
		bool cancelled;
		int searchedFiles;
		int matches;
		int lastResultPos;
		SearchMap reverseSearchMap;
		bool lastWasReverse;
		
		public IDocumentInformation CurrentDocumentInformation {
			get {
				return info;
			}
		}
		
		public ITextIterator TextIterator {
			get {
				return textIterator;
			}
		}
		
		public ISearchStrategy SearchStrategy {
			get {
				return searchStrategy;
			}
			set {
				searchStrategy = value;
			}
		}
		
		public IDocumentIterator DocumentIterator {
			get {
				return documentIterator;
			}
			set {
				documentIterator = value;
			}
		}
		
		public int SearchedFileCount {
			get { return searchedFiles; }
		}
		
		public int MatchCount {
			get { return matches; }
		}
		
		public void Reset()
		{
			documentIterator.Reset();
			textIterator = null;
			reverseSearchMap = null;
			cancelled = false;
			searchedFiles = 0;
			matches = 0;
			lastResultPos = -1;
		}
		
		public void Replace (SearchResult result, string pattern)
		{
			if (CurrentDocumentInformation != null && TextIterator != null) {
				TextIterator.Position = result.Position;
				TextIterator.Replace (result.Length, pattern);
				TextIterator.Position = result.Position + pattern.Length - 1;
			}
		}
		
		public SearchResult FindNext(SearchOptions options) 
		{
			return Find (options, false);
		}
		
		public SearchResult FindPrevious (SearchOptions options) 
		{
			return Find (options, true);
		}
		
		public SearchResult Find (SearchOptions options, bool reverse)
		{
			// insanity check
			Debug.Assert(searchStrategy      != null);
			Debug.Assert(documentIterator    != null);
			Debug.Assert(options             != null);
			
			while (!cancelled)
			{
				if (info != null && textIterator != null && documentIterator.CurrentFileName != null) {
					if (info.FileName != documentIterator.CurrentFileName || lastWasReverse != reverse) {
						// create new iterator, if document changed or search direction has changed.
						info = documentIterator.Current;
						textIterator = info.GetTextIterator ();
						reverseSearchMap = null;
						lastResultPos = -1;
						if (reverse)
							textIterator.MoveToEnd ();
					} 

					SearchResult result;
					if (!reverse)
						result = searchStrategy.FindNext (textIterator, options, false);
					else {
						if (searchStrategy.SupportsReverseSearch (textIterator, options)) {
							result = searchStrategy.FindNext (textIterator, options, true);
						}
						else {
							if (reverseSearchMap == null) {
								reverseSearchMap = new SearchMap ();
								reverseSearchMap.Build (searchStrategy, textIterator, options);
							}
							if (lastResultPos == -1)
								lastResultPos = textIterator.Position;
							result = reverseSearchMap.GetPreviousMatch (lastResultPos);
							if (result != null)
								textIterator.Position = result.Position;
						}
					}
						
					if (result != null) {
						matches++;
						lastResultPos = result.Position;
						lastWasReverse = reverse;
						return result;
					}
				}
				
				if (textIterator != null) textIterator.Close ();
					
				// not found or first start -> move forward to the next document
				bool more = !reverse ? documentIterator.MoveForward () : documentIterator.MoveBackward ();
				if (more && ((info = documentIterator.Current) != null)) {
					searchedFiles++;
					info = documentIterator.Current;
					textIterator = info.GetTextIterator ();
					reverseSearchMap = null;
					lastResultPos = -1;
					if (reverse)
						textIterator.MoveToEnd ();
				}
				else
					cancelled = true;

				lastWasReverse = reverse;
			}
			
			cancelled = false;
			return null;
		}
		
		public void Cancel ()
		{
			cancelled = true;
		}
	}
	
	class SearchMap
	{
		ArrayList matches = new ArrayList ();

		public void Build (ISearchStrategy strategy, ITextIterator it, SearchOptions options)
		{
			int startPos = it.Position;
			it.Reset ();

			SearchResult res = strategy.FindNext (it, options, false);
			while (res != null) {
				matches.Add (res);
				res = strategy.FindNext (it, options, false);
			}
			it.Position = startPos;
		}
		
		public SearchResult GetPreviousMatch (int pos)
		{
			if (matches.Count == 0) return null;
			
			for (int n = matches.Count - 1; n >= 0; n--) {
				SearchResult m = (SearchResult) matches [n];
				if (m.Position < pos)
					return m;
			}
			
			return null;
		}
	}
}
