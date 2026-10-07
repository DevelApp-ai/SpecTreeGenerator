using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.Text;

namespace GeneticSpecOptimizer.Common.Models
{
    /// <summary>
    /// Represents a text span in a source file
    /// </summary>
    public class TextSpan
    {
        /// <summary>
        /// Start position of the span
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// End position of the span
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// Length of the span
        /// </summary>
        public int Length => End - Start;

        /// <summary>
        /// Creates a new text span
        /// </summary>
        public TextSpan(int start, int end)
        {
            // Ensure start and end are valid
            if (end < start)
            {
                // Swap if end is before start
                int temp = start;
                start = end;
                end = temp;
            }

            Start = start;
            End = end;
        }

        /// <summary>
        /// Returns a string representation of the span
        /// </summary>
        public override string ToString()
        {
            return $"[{Start}..{End})";
        }

        /// <summary>
        /// Converts to Microsoft.CodeAnalysis.Text.TextSpan
        /// </summary>
        public static implicit operator Microsoft.CodeAnalysis.Text.TextSpan(TextSpan span)
        {
            // Ensure length is non-negative
            int length = Math.Max(0, span.Length);
            return new Microsoft.CodeAnalysis.Text.TextSpan(span.Start, length);
        }

        /// <summary>
        /// Converts from Microsoft.CodeAnalysis.Text.TextSpan
        /// </summary>
        public static implicit operator TextSpan(Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            return new TextSpan(span.Start, span.End);
        }
    }
}