using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FLua.Runtime
{
    /// <summary>
    /// Implements Lua 5.4 pattern matching (not regular expressions)
    /// </summary>
    public static class LuaPatterns
    {
        /// <summary>
        /// Character class definitions for Lua patterns
        /// </summary>
        internal static readonly Dictionary<char, Func<char, bool>> CharacterClasses = new()
        {
            ['a'] = char.IsLetter,
            ['c'] = char.IsControl,
            ['d'] = char.IsDigit,
            ['g'] = c => char.IsLetterOrDigit(c) || char.IsPunctuation(c), // graphic characters
            ['l'] = char.IsLower,
            ['p'] = char.IsPunctuation,
            ['s'] = char.IsWhiteSpace,
            ['u'] = char.IsUpper,
            ['w'] = char.IsLetterOrDigit,
            ['x'] = c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')
        };

        /// <summary>
        /// Finds a pattern in a string
        /// </summary>
        public static LuaPatternMatch? Find(string text, string pattern, int start = 1, bool plain = false)
        {
            // Handle negative start position (count from end)
            int actualStart = start;
            if (start < 0)
            {
                actualStart = text.Length + start + 1;
            }
            
            // Clamp to valid range
            actualStart = Math.Max(1, Math.Min(actualStart, text.Length + 1));
            
            if (plain)
            {
                // Plain text search
                if (string.IsNullOrEmpty(pattern))
                {
                    // Empty pattern special case - should find at start position
                    return new LuaPatternMatch
                    {
                        Start = actualStart,
                        End = actualStart - 1, // Empty match: end before start
                        Captures = []
                    };
                }
                
                var index = text.IndexOf(pattern, actualStart - 1, StringComparison.Ordinal);
                if (index >= 0)
                {
                    return new LuaPatternMatch
                    {
                        Start = index + 1, // Convert to 1-based
                        End = index + pattern.Length, // End position is inclusive (last character position)
                        Captures = []
                    };
                }
                return null;
            }

            // Lua pattern matching
            var matcher = new LuaPatternMatcher(pattern);
            return matcher.Match(text, actualStart - 1); // Convert to 0-based for internal use
        }

        /// <summary>
        /// Finds all matches of a pattern in a string
        /// </summary>
        public static IEnumerable<LuaPatternMatch> FindAll(string text, string pattern)
        {
            var matcher = new LuaPatternMatcher(pattern);
            int start = 0;
            
            while (start < text.Length)
            {
                var match = matcher.Match(text, start);
                if (match == null)
                    break;
                    
                yield return match;
                start = match.End; // Move past this match
                
                // Avoid infinite loop on zero-length matches
                if (match.Start == match.End)
                    start++;
            }
        }

        /// <summary>
        /// Substitutes matches of a pattern in a string
        /// </summary>
        public static (string result, int count) GSub(string text, string pattern, string replacement, int limit = int.MaxValue)
        {
            var matches = new List<LuaPatternMatch>();
            var matcher = new LuaPatternMatcher(pattern);
            int start = 0;
            int count = 0;
            
            while (start < text.Length && count < limit)
            {
                var match = matcher.Match(text, start);
                if (match == null)
                    break;
                    
                matches.Add(match);
                count++;
                start = match.End;
                
                // Avoid infinite loop on zero-length matches
                if (match.Start == match.End)
                    start++;
            }

            if (matches.Count == 0)
                return (text, 0);

            var result = new StringBuilder();
            int lastEnd = 0;

            foreach (var match in matches)
            {
                // Add text before match
                result.Append(text.Substring(lastEnd, match.Start - 1 - lastEnd));
                
                // Add replacement (process capture references)
                result.Append(ProcessReplacement(replacement, match.Captures, text));
                
                lastEnd = match.End;
            }

            // Add remaining text
            result.Append(text.Substring(lastEnd));

            return (result.ToString(), matches.Count);
        }

        /// <summary>
        /// Processes replacement string with capture references
        /// </summary>
        private static string ProcessReplacement(string replacement, List<string> captures, string originalText)
        {
            var result = new StringBuilder();
            
            for (int i = 0; i < replacement.Length; i++)
            {
                if (replacement[i] == '%' && i + 1 < replacement.Length)
                {
                    var next = replacement[i + 1];
                    if (char.IsDigit(next))
                    {
                        var captureIndex = next - '0';
                        if (captureIndex == 0)
                        {
                            // %0 refers to the entire match
                            result.Append(originalText);
                        }
                        else if (captureIndex <= captures.Count)
                        {
                            result.Append(captures[captureIndex - 1]);
                        }
                        i++; // Skip the digit
                    }
                    else if (next == '%')
                    {
                        result.Append('%');
                        i++; // Skip the second %
                    }
                    else
                    {
                        result.Append('%');
                    }
                }
                else
                {
                    result.Append(replacement[i]);
                }
            }
            
            return result.ToString();
        }
    }

    /// <summary>
    /// Represents a Lua pattern match result
    /// </summary>
    public class LuaPatternMatch
    {
        public int Start { get; set; } // 1-based
        public int End { get; set; }   // 1-based, exclusive
        public List<string> Captures { get; set; } = [];
    }

    /// <summary>
    /// Lua pattern matcher implementation
    /// </summary>
    internal class LuaPatternMatcher
    {
        private readonly string _pattern;
        private readonly List<PatternElement> _elements;

        public LuaPatternMatcher(string pattern)
        {
            _pattern = pattern;
            _elements = ParsePattern(pattern);
        }

        // Constructor for sub-patterns (used in quantified captures)
        internal LuaPatternMatcher(List<PatternElement> elements)
        {
            _pattern = string.Empty; // Not used for sub-patterns
            _elements = elements;
        }

        /// <summary>
        /// Attempts to match the pattern at the specified position
        /// </summary>
        public LuaPatternMatch? Match(string text, int start)
        {
            var captureInfos = new List<(int CaptureNumber, string Text)>();
            
            // Try matching at each position from start
            for (int pos = start; pos < text.Length; pos++)
            {
                var match = TryMatch(text, pos, 0, captureInfos);
                if (match.HasValue)
                {
                    // Sort captures by their capture number to maintain left-to-right order
                    var sortedCaptures = captureInfos
                        .OrderBy(c => c.CaptureNumber)
                        .Select(c => c.Text)
                        .ToList();
                    
                    return new LuaPatternMatch
                    {
                        Start = pos + 1, // Convert to 1-based
                        End = match.Value, // TryMatch returns position after match (0-based), so this is already 1-based position after match
                        Captures = sortedCaptures
                    };
                }
                
                captureInfos.Clear();
                
                // If pattern starts with ^, only try at the specified start position
                if (_elements.Count > 0 && _elements[0] is AnchorElement anchor && anchor.IsStart)
                    break;
            }

            return null;
        }

        /// <summary>
        /// Tries to match the pattern starting at a specific position
        /// </summary>
        private int? TryMatch(string text, int textPos, int patternPos, List<(int CaptureNumber, string Text)> captureInfos)
        {
            return TryMatchInternal(text, textPos, patternPos, captureInfos, new Stack<(int captureNumber, int startIndex)>());
        }

        /// <summary>
        /// Internal recursive match method with capture stack tracking
        /// </summary>
        private int? TryMatchInternal(string text, int textPos, int patternPos, List<(int CaptureNumber, string Text)> captureInfos, Stack<(int captureNumber, int startIndex)> captureStack)
        {
            while (patternPos < _elements.Count)
            {
                var element = _elements[patternPos];
                
                if (textPos >= text.Length && !(element is AnchorElement || element is CaptureElement))
                {
                    return null;
                }
                
                if (element is AnchorElement anchor)
                {
                    if (anchor.IsStart && textPos != 0)
                        return null;
                    if (anchor.IsEnd && textPos != text.Length)
                        return null;
                    patternPos++;
                    continue;
                }

                if (element is CaptureElement capture)
                {
                    if (capture.IsStart)
                    {
                        // Push the current text position and capture number as capture start
                        captureStack.Push((capture.CaptureNumber, textPos));
                        patternPos++;
                        continue;
                    }
                    else
                    {
                        // This is a capture end - check if it has quantifiers
                        if (capture.MinRepeats != 1 || capture.MaxRepeats != 1)
                        {
                            // Handle quantified capture groups
                            return HandleQuantifiedCapture(text, textPos, patternPos, captureInfos, captureStack, capture);
                        }
                        else
                        {
                            // Normal capture end without quantifiers
                            if (captureStack.Count > 0)
                            {
                                var captureStart = captureStack.Pop();
                                var captureText = text.Substring(captureStart.startIndex, textPos - captureStart.startIndex);
                                captureInfos.Add((captureStart.captureNumber, captureText));
                            }
                            patternPos++;
                            continue;
                        }
                    }
                }

                if (element is CharacterElement charElem)
                {
                    var minRepeats = charElem.MinRepeats;
                    var maxRepeats = charElem.MaxRepeats;
                    var isUnlimited = maxRepeats == 0; // 0 is special value for unlimited
                    
                    // For greedy quantifiers (*, +), try longest match first
                    // For non-greedy quantifiers (-), try shortest match first
                    var tryGreedy = !charElem.IsNonGreedy;
                    
                    if (isUnlimited)
                    {
                        // Unlimited quantifiers: *, +, -
                        var maxPossible = text.Length - textPos;
                        
                        // Find the maximum number of characters that match
                        int actualMax = 0;
                        for (int i = 0; i < maxPossible; i++)
                        {
                            if (textPos + i >= text.Length || !charElem.Matches(text[textPos + i]))
                                break;
                            actualMax++;
                        }
                        
                        // Ensure we satisfy minimum requirements
                        if (actualMax < minRepeats)
                            return null;
                        
                        // Try different counts based on greediness
                        if (tryGreedy)
                        {
                            // Greedy: try from maximum down to minimum
                            for (int count = actualMax; count >= minRepeats; count--)
                            {
                                var nextMatch = TryMatchInternal(text, textPos + count, patternPos + 1, captureInfos, captureStack);
                                if (nextMatch.HasValue)
                                    return nextMatch;
                            }
                        }
                        else
                        {
                            // Non-greedy: try from minimum up to maximum
                            for (int count = minRepeats; count <= actualMax; count++)
                            {
                                var nextMatch = TryMatchInternal(text, textPos + count, patternPos + 1, captureInfos, captureStack);
                                if (nextMatch.HasValue)
                                    return nextMatch;
                            }
                        }
                        return null;
                    }
                    else
                    {
                        // Fixed or range repeats (like ? which is {0,1})
                        var effectiveMax = Math.Min(maxRepeats, text.Length - textPos);
                        
                        // Find how many characters actually match
                        int actualMatches = 0;
                        for (int i = 0; i < effectiveMax; i++)
                        {
                            if (textPos + i >= text.Length || !charElem.Matches(text[textPos + i]))
                                break;
                            actualMatches++;
                        }
                        
                        // Ensure we can satisfy minimum requirements
                        if (actualMatches < minRepeats)
                            return null;
                        
                        // For fixed ranges, try from maximum possible down to minimum
                        for (int count = Math.Min(actualMatches, effectiveMax); count >= minRepeats; count--)
                        {
                            var nextMatch = TryMatchInternal(text, textPos + count, patternPos + 1, captureInfos, captureStack);
                            if (nextMatch.HasValue)
                                return nextMatch;
                        }
                        return null;
                    }
                }

                patternPos++;
            }

            return textPos; // Successful match, return current position
        }

        private int? HandleQuantifiedCapture(string text, int textPos, int patternPos, List<(int CaptureNumber, string Text)> captureInfos, Stack<(int captureNumber, int startIndex)> captureStack, CaptureElement capture)
        {
            // For quantified captures like (st)?, we need to try different numbers of matches
            var minRepeats = capture.MinRepeats;
            var maxRepeats = capture.MaxRepeats;
            var isUnlimited = maxRepeats == 0; // 0 is special value for unlimited
            var tryGreedy = !capture.IsNonGreedy;
            
            // Find the pattern elements inside the capture group
            var captureStartIndex = -1;
            var captureEndIndex = patternPos;
            
            // Walk backwards to find the matching capture start
            for (int i = patternPos - 1; i >= 0; i--)
            {
                if (_elements[i] is CaptureElement startCapture && 
                    startCapture.IsStart && 
                    startCapture.CaptureNumber == capture.CaptureNumber)
                {
                    captureStartIndex = i;
                    break;
                }
            }
            
            if (captureStartIndex == -1)
                return null; // Invalid pattern structure
            
            // Get the sub-pattern elements between capture start and end (exclusive)
            var subPatternElements = new List<PatternElement>();
            for (int i = captureStartIndex + 1; i < captureEndIndex; i++)
            {
                subPatternElements.Add(_elements[i]);
            }
            
            if (isUnlimited)
            {
                // Try to match the capture group as many times as possible
                var matches = new List<string>();
                var currentPos = textPos;
                
                // Find maximum possible matches
                while (currentPos < text.Length)
                {
                    var subMatcher = new LuaPatternMatcher(subPatternElements);
                    var subCaptureInfos = new List<(int CaptureNumber, string Text)>();
                    var match = subMatcher.TryMatch(text, currentPos, 0, subCaptureInfos);
                    
                    if (match.HasValue && match.Value > currentPos)
                    {
                        matches.Add(text.Substring(currentPos, match.Value - currentPos));
                        currentPos = match.Value;
                    }
                    else
                    {
                        break;
                    }
                }
                
                // Ensure we have at least minRepeats matches
                if (matches.Count < minRepeats)
                    return null;
                
                // Try different counts based on greediness
                if (tryGreedy)
                {
                    // Greedy: try from maximum down to minimum
                    for (int count = matches.Count; count >= minRepeats; count--)
                    {
                        // Add the capture(s) for this count
                        if (count > 0)
                        {
                            // For quantified captures, we typically add the last match
                            captureInfos.Add((capture.CaptureNumber, matches[count - 1]));
                        }
                        
                        var totalConsumed = matches.Take(count).Sum(m => m.Length);
                        var nextMatch = TryMatchInternal(text, textPos + totalConsumed, patternPos + 1, captureInfos, captureStack);
                        if (nextMatch.HasValue)
                            return nextMatch;
                            
                        // Remove the capture we added for backtracking
                        if (count > 0 && captureInfos.Count > 0 && captureInfos.Last().CaptureNumber == capture.CaptureNumber)
                        {
                            captureInfos.RemoveAt(captureInfos.Count - 1);
                        }
                    }
                }
                else
                {
                    // Non-greedy: try from minimum up to maximum
                    for (int count = minRepeats; count <= matches.Count; count++)
                    {
                        // Add the capture(s) for this count
                        if (count > 0)
                        {
                            // For quantified captures, we typically add the last match
                            captureInfos.Add((capture.CaptureNumber, matches[count - 1]));
                        }
                        
                        var totalConsumed = matches.Take(count).Sum(m => m.Length);
                        var nextMatch = TryMatchInternal(text, textPos + totalConsumed, patternPos + 1, captureInfos, captureStack);
                        if (nextMatch.HasValue)
                            return nextMatch;
                            
                        // Remove the capture we added for backtracking
                        if (count > 0 && captureInfos.Count > 0 && captureInfos.Last().CaptureNumber == capture.CaptureNumber)
                        {
                            captureInfos.RemoveAt(captureInfos.Count - 1);
                        }
                    }
                }
                return null;
            }
            else
            {
                // Fixed range quantifiers (like ? which is {0,1})
                var effectiveMax = Math.Min(maxRepeats, text.Length - textPos);
                var matches = new List<string>();
                var currentPos = textPos;
                
                // Find matches up to the maximum allowed
                for (int attempt = 0; attempt < effectiveMax && currentPos < text.Length; attempt++)
                {
                    var subMatcher = new LuaPatternMatcher(subPatternElements);
                    var subCaptureInfos = new List<(int CaptureNumber, string Text)>();
                    var match = subMatcher.TryMatch(text, currentPos, 0, subCaptureInfos);
                    
                    if (match.HasValue && match.Value > currentPos)
                    {
                        matches.Add(text.Substring(currentPos, match.Value - currentPos));
                        currentPos = match.Value;
                    }
                    else
                    {
                        break;
                    }
                }
                
                // Ensure we can satisfy minimum requirements
                if (matches.Count < minRepeats)
                    return null;
                
                // Try from maximum possible down to minimum
                for (int count = Math.Min(matches.Count, effectiveMax); count >= minRepeats; count--)
                {
                    // Add the capture(s) for this count
                    if (count > 0)
                    {
                        // For quantified captures, we typically add the last match
                        captureInfos.Add((capture.CaptureNumber, matches[count - 1]));
                    }
                    
                    var totalConsumed = matches.Take(count).Sum(m => m.Length);
                    var nextMatch = TryMatchInternal(text, textPos + totalConsumed, patternPos + 1, captureInfos, captureStack);
                    if (nextMatch.HasValue)
                        return nextMatch;
                        
                    // Remove the capture we added for backtracking
                    if (count > 0 && captureInfos.Count > 0 && captureInfos.Last().CaptureNumber == capture.CaptureNumber)
                    {
                        captureInfos.RemoveAt(captureInfos.Count - 1);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Finds the matching end capture for a start capture
        /// </summary>
        private int FindMatchingCaptureEnd(int startPos)
        {
            int depth = 0;
            for (int i = startPos; i < _elements.Count; i++)
            {
                if (_elements[i] is CaptureElement capture)
                {
                    if (capture.IsStart)
                        depth++;
                    else
                    {
                        depth--;
                        if (depth == 0)
                            return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Parses a Lua pattern into elements
        /// </summary>
        private List<PatternElement> ParsePattern(string pattern)
        {
            var elements = new List<PatternElement>();
            var captureNumber = 1; // Capture numbers start from 1
            var openCaptureStack = new Stack<int>(); // Track which capture numbers are open
            
            for (int i = 0; i < pattern.Length; i++)
            {
                var c = pattern[i];
                
                switch (c)
                {
                    case '^':
                        if (i == 0)
                            elements.Add(new AnchorElement { IsStart = true });
                        else
                            elements.Add(new CharacterElement('^'));
                        break;
                        
                    case '$':
                        if (i == pattern.Length - 1)
                            elements.Add(new AnchorElement { IsEnd = true });
                        else
                            elements.Add(new CharacterElement('$'));
                        break;
                        
                    case '(':
                        // Only treat as capture if there's a properly nested matching closing paren
                        var closingParen = FindMatchingParen(pattern, i);
                        if (closingParen != -1)
                        {
                            elements.Add(new CaptureElement { IsStart = true, CaptureNumber = captureNumber });
                            openCaptureStack.Push(captureNumber);
                            captureNumber++;
                        }
                        else
                            elements.Add(new CharacterElement('(')); // Treat as literal
                        break;
                        
                    case ')':
                        // Only treat as capture end if there's an unmatched opening paren before
                        if (openCaptureStack.Count > 0)
                        {
                            var matchingCaptureNumber = openCaptureStack.Pop();
                            var captureEnd = new CaptureElement { IsStart = false, CaptureNumber = matchingCaptureNumber };
                            
                            // Check for quantifiers after capture group
                            if (i + 1 < pattern.Length)
                            {
                                var next = pattern[i + 1];
                                switch (next)
                                {
                                    case '*':
                                        captureEnd.MinRepeats = 0;
                                        captureEnd.MaxRepeats = 0; // Special value for unlimited
                                        i++;
                                        break;
                                    case '+':
                                        captureEnd.MinRepeats = 1;
                                        captureEnd.MaxRepeats = 0; // Special value for unlimited
                                        i++;
                                        break;
                                    case '-':
                                        captureEnd.MinRepeats = 0;
                                        captureEnd.MaxRepeats = 0; // Non-greedy unlimited
                                        captureEnd.IsNonGreedy = true;
                                        i++;
                                        break;
                                    case '?':
                                        captureEnd.MinRepeats = 0;
                                        captureEnd.MaxRepeats = 1;
                                        i++;
                                        break;
                                }
                            }
                            
                            elements.Add(captureEnd);
                        }
                        else
                            elements.Add(new CharacterElement(')')); // Treat as literal
                        break;
                        
                    case '%':
                        if (i + 1 < pattern.Length)
                        {
                            var next = pattern[i + 1];
                            var escapeElem = new CharacterElement(next, isEscape: true);
                            i++; // Skip next character
                            
                            // Check for quantifiers after escape sequence
                            if (i + 1 < pattern.Length)
                            {
                                var quantifier = pattern[i + 1];
                                switch (quantifier)
                                {
                                    case '*':
                                        escapeElem.MinRepeats = 0;
                                        escapeElem.MaxRepeats = 0; // Special value for unlimited
                                        i++;
                                        break;
                                    case '+':
                                        escapeElem.MinRepeats = 1;
                                        escapeElem.MaxRepeats = 0; // Special value for unlimited
                                        i++;
                                        break;
                                    case '-':
                                        escapeElem.MinRepeats = 0;
                                        escapeElem.MaxRepeats = 0; // Non-greedy unlimited
                                        escapeElem.IsNonGreedy = true;
                                        i++;
                                        break;
                                    case '?':
                                        escapeElem.MinRepeats = 0;
                                        escapeElem.MaxRepeats = 1;
                                        i++;
                                        break;
                                }
                            }
                            elements.Add(escapeElem);
                        }
                        else
                        {
                            // Trailing % - treat as literal
                            elements.Add(new CharacterElement('%'));
                        }
                        break;
                        
                    case '.':
                        var dotElem = new CharacterElement('.', isDot: true);
                        
                        // Check for quantifiers after dot
                        if (i + 1 < pattern.Length)
                        {
                            var next = pattern[i + 1];
                            switch (next)
                            {
                                case '*':
                                    dotElem.MinRepeats = 0;
                                    dotElem.MaxRepeats = 0; // Special value for unlimited
                                    i++;
                                    break;
                                case '+':
                                    dotElem.MinRepeats = 1;
                                    dotElem.MaxRepeats = 0; // Special value for unlimited
                                    i++;
                                    break;
                                case '-':
                                    dotElem.MinRepeats = 0;
                                    dotElem.MaxRepeats = 0; // Non-greedy unlimited
                                    dotElem.IsNonGreedy = true;
                                    i++;
                                    break;
                                case '?':
                                    dotElem.MinRepeats = 0;
                                    dotElem.MaxRepeats = 1;
                                    i++;
                                    break;
                            }
                        }
                        elements.Add(dotElem);
                        break;
                        
                    case '[':
                        // Parse character class
                        int endBracket = pattern.IndexOf(']', i + 1);
                        if (endBracket != -1)
                        {
                            var charClass = pattern.Substring(i + 1, endBracket - i - 1);
                            var classElem = new CharacterElement(charClass, isCharClass: true);
                            i = endBracket;
                            
                            // Check for quantifiers after character class
                            if (i + 1 < pattern.Length)
                            {
                                var next = pattern[i + 1];
                                switch (next)
                                {
                                    case '*':
                                        classElem.MinRepeats = 0;
                                        classElem.MaxRepeats = 0; // Special value for unlimited
                                        i++;
                                        break;
                                    case '+':
                                        classElem.MinRepeats = 1;
                                        classElem.MaxRepeats = 0; // Special value for unlimited
                                        i++;
                                        break;
                                    case '-':
                                        classElem.MinRepeats = 0;
                                        classElem.MaxRepeats = 0; // Non-greedy unlimited
                                        classElem.IsNonGreedy = true;
                                        i++;
                                        break;
                                    case '?':
                                        classElem.MinRepeats = 0;
                                        classElem.MaxRepeats = 1;
                                        i++;
                                        break;
                                }
                            }
                            elements.Add(classElem);
                        }
                        else
                        {
                            elements.Add(new CharacterElement('['));
                        }
                        break;
                        
                    default:
                        var charElem = new CharacterElement(c);
                        
                        // Check for quantifiers
                        if (i + 1 < pattern.Length)
                        {
                            var next = pattern[i + 1];
                            switch (next)
                            {
                                case '*':
                                    charElem.MinRepeats = 0;
                                    charElem.MaxRepeats = 0; // Special value for unlimited
                                    i++;
                                    break;
                                case '+':
                                    charElem.MinRepeats = 1;
                                    charElem.MaxRepeats = 0; // Special value for unlimited
                                    i++;
                                    break;
                                case '-':
                                    charElem.MinRepeats = 0;
                                    charElem.MaxRepeats = 0; // Non-greedy unlimited
                                    charElem.IsNonGreedy = true;
                                    i++;
                                    break;
                                case '?':
                                    charElem.MinRepeats = 0;
                                    charElem.MaxRepeats = 1;
                                    i++;
                                    break;
                            }
                        }
                        
                        elements.Add(charElem);
                        break;
                }
            }
            
            return elements;
        }

        /// <summary>
        /// Finds the matching closing parenthesis for the opening paren at the given index
        /// </summary>
        private int FindMatchingParen(string pattern, int openIndex)
        {
            int depth = 1;
            for (int i = openIndex + 1; i < pattern.Length; i++)
            {
                switch (pattern[i])
                {
                    case '(':
                        depth++;
                        break;
                    case ')':
                        depth--;
                        if (depth == 0)
                            return i;
                        break;
                    case '%':
                        // Skip escaped characters
                        if (i + 1 < pattern.Length)
                            i++;
                        break;
                }
            }
            return -1; // No matching closing paren found
        }
    }

    /// <summary>
    /// Base class for pattern elements
    /// </summary>
    internal abstract class PatternElement
    {
    }

    /// <summary>
    /// Represents an anchor (^ or $)
    /// </summary>
    internal class AnchorElement : PatternElement
    {
        public bool IsStart { get; set; }
        public bool IsEnd { get; set; }
    }

    /// <summary>
    /// Represents a capture group
    /// </summary>
    internal class CaptureElement : PatternElement
    {
        public bool IsStart { get; set; }
        public int CaptureNumber { get; set; } // For ordering captures
        public int MinRepeats { get; set; } = 1;
        public int MaxRepeats { get; set; } = 1;
        public bool IsNonGreedy { get; set; }
    }

    /// <summary>
    /// Represents a character or character class
    /// </summary>
    internal class CharacterElement : PatternElement
    {
        private readonly char _char;
        private readonly string? _charClass;
        private readonly bool _isEscape;
        private readonly bool _isDot;
        private readonly bool _isCharClass;
        
        public int MinRepeats { get; set; } = 1;
        public int MaxRepeats { get; set; } = 1;
        public bool IsNonGreedy { get; set; }

        public CharacterElement(char c, bool isEscape = false, bool isDot = false)
        {
            _char = c;
            _isEscape = isEscape;
            _isDot = isDot;
        }

        public CharacterElement(string charClass, bool isCharClass = false)
        {
            _charClass = charClass;
            _isCharClass = isCharClass;
        }

        /// <summary>
        /// Checks if a character matches this element
        /// </summary>
        public bool Matches(char c)
        {
            if (_isDot)
                return true; // . matches any character
                
            if (_isEscape)
            {
                // Handle escape sequences
                if (LuaPatterns.CharacterClasses.TryGetValue(char.ToLower(_char), out var predicate))
                {
                    bool matches = predicate(c);
                    return char.IsUpper(_char) ? !matches : matches; // Uppercase negates
                }
                return c == _char;
            }
            
            if (_isCharClass && _charClass != null)
            {
                return MatchesCharClass(c, _charClass);
            }
            
            return c == _char;
        }

        /// <summary>
        /// Checks if a character matches a character class
        /// </summary>
        private bool MatchesCharClass(char c, string charClass)
        {
            bool negate = charClass.StartsWith("^");
            if (negate)
                charClass = charClass.Substring(1);

            bool matches = false;
            
            for (int i = 0; i < charClass.Length; i++)
            {
                if (charClass[i] == '%' && i + 1 < charClass.Length)
                {
                    // Escaped character class or literal escape
                    var classChar = charClass[i + 1];
                    if (LuaPatterns.CharacterClasses.TryGetValue(char.ToLower(classChar), out var predicate))
                    {
                        // It's a character class like %d, %w, etc.
                        bool classMatches = predicate(c);
                        if (char.IsUpper(classChar))
                            classMatches = !classMatches;
                        if (classMatches)
                        {
                            matches = true;
                            break;
                        }
                    }
                    else
                    {
                        // It's a literal escaped character like %., %%, %(, etc.
                        if (c == classChar)
                        {
                            matches = true;
                            break;
                        }
                    }
                    i++; // Skip next char
                }
                else if (i + 2 < charClass.Length && charClass[i + 1] == '-')
                {
                    // Range
                    if (c >= charClass[i] && c <= charClass[i + 2])
                    {
                        matches = true;
                        break;
                    }
                    i += 2; // Skip range
                }
                else
                {
                    // Single character
                    if (c == charClass[i])
                    {
                        matches = true;
                        break;
                    }
                }
            }
            
            return negate ? !matches : matches;
        }
    }
}
