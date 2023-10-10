namespace Unity.Media.Osc
{
    /// <summary>
    /// A class used to match OSC Address Patterns.
    /// </summary>
    static unsafe class PatternMatching
    {
        /// <summary>
        /// Match an OSC Address Pattern with an OSC Address.
        /// </summary>
        /// <param name="pattern">The pattern to match. Must point to a valid address pattern, consisting of a
        /// null-terminated ASCII string. It is up to the caller to verify correctness.</param>
        /// <param name="address">The address to match. Must point to a valid address, consisting of a
        /// null-terminated ASCII string. It is up to the caller to verify correctness.</param>
        /// <returns><see langword="true"/> if the addresses match; otherwise, <see langword="false"/>.</returns>
        internal static bool Match(byte* pattern, byte* address)
        {
            // we iterate along the pattern string checking that address matches along the way
            while (*pattern != '\0')
            {
                // If we have reached the end of the address but there are characters left in the pattern they don't match,
                // unless the last pattern character is a wildcard that matches any number of characters
                if (*address == '\0' && *pattern != '*')
                {
                    return false;
                }

                var c = (char)*pattern++;

                switch (c)
                {
                    case '?':
                    {
                        // this wildcard matches any character in an address part
                        if (*address == '/')
                        {
                            return false;
                        }
                        break;
                    }
                    case '*':
                    {
                        // this wildcard matches any sequence of zero or more characters within an address part
                        return MatchWildcard(ref pattern, ref address);
                    }
                    case '[':
                    {
                        // A string of characters in square brackets (e.g., “[asdf]”) matches on any character in the string. Inside square
                        // brackets, the minus sign and exclamation point have special meanings. Two characters separated by a minus sign
                        // indicate the range of characters between the given two in ASCII collating sequence. (A minus sign at the end of the
                        // string has no special meaning.) An exclamation point at the beginning of a bracketed string negates the sense
                        // of the list, meaning that the list matches any character not in the list. (An exclamation point anywhere besides
                        // the first character after the open bracket has no special meaning.)
                        if (!MatchBrackets(ref pattern, ref address))
                        {
                            return false;
                        }
                        break;
                    }
                    case '{':
                    {
                        // A comma-separated list of strings enclosed in curly braces (e.g., “{foo,bar}”) matches any of the strings in the list.
                        if (!MatchBraces(ref pattern, ref address))
                        {
                            return false;
                        }
                        break;
                    }
                    case '/':
                    {
                        // the "//" wildcard matches any subsequent address parts
                        if (*pattern == '/')
                        {
                            return MatchMultiLevelWildcard(ref pattern, ref address);
                        }

                        // match as usual if there aren't two forward slashes in sequence
                        if (*address != c)
                        {
                            return false;
                        }
                        break;
                    }
                    default:
                    {
                        // normally each character must match exactly
                        if (*address != c)
                        {
                            return false;
                        }
                        break;
                    }
                }

                address++;
            }

            // if we have reached the end of the pattern but there are characters left in the address they don't match
            return *address == '\0';
        }

        static bool MatchWildcard(ref byte* pattern, ref byte* address)
        {
            // skip past redundant wildcards
            while (*pattern == '*')
            {
                pattern++;
            }

            // If the wildcard is at the end of the pattern, it matches just one address part, so we must check that
            // there are no additional address parts
            if (*pattern == '\0')
            {
                while (*address != '\0')
                {
                    if (*address++ == '/')
                    {
                        return false;
                    }
                }

                return true;
            }

            // Check each substring of the address for matches with the remaining pattern.
            // Although costly on performance, we need to do this as we do not know exactly how much of
            // the remaining address matches the remaining patterns instead of the wildcard. Fortunately,
            // this will only need to recuse once in the most common cases.
            while (*address != '\0')
            {
                if (Match(pattern, address))
                {
                    return true;
                }

                // stop matching at the end of the current address part
                if (*address++ == '/')
                {
                    return false;
                }
            }

            return false;
        }

        static bool MatchMultiLevelWildcard(ref byte* pattern, ref byte* address)
        {
            // skip past redundant slashes
            while (*pattern == '/')
            {
                pattern++;
            }

            // the wildcard must be at the start of a new address part
            if (*address != '/')
            {
                return false;
            }

            // check each substring of address parts for matches with the remaining pattern
            while (*address != '\0')
            {
                address++;

                if (Match(pattern, address))
                {
                    return true;
                }

                // skip to the next container
                while (*++address != '\0' && *address != '/')
                {
                }
            }

            return false;
        }

        static bool MatchBrackets(ref byte* pattern, ref byte* address)
        {
            // check if we are excluding characters in the brackets
            var negate = false;

            if (*pattern == '!')
            {
                negate = true;
                pattern++;
            }

            // empty brackets match everything
            if (*pattern == ']')
            {
                pattern++;
                return true;
            }

            // test each character in the brackets until a match is found or the closing bracket is reached
            while (true)
            {
                // check if a range of characters (e.g., "a-z") is next
                if (pattern[1] == '-')
                {
                    // get the character range
                    var lowerBound = pattern[0];
                    var upperBound = pattern[2];

                    pattern += 3;

                    // when the minus is adjacent to the closing bracket (e.g., "a-]") we match it and the previous character as usual
                    if (upperBound == ']')
                    {
                        if (*address == lowerBound)
                        {
                            return !negate;
                        }
                        if (*address == '-')
                        {
                            return !negate;
                        }
                        return negate;
                    }

                    // the spec doesn't specify that the range is in ascending order, so make sure the range in ascending order first
                    if (upperBound < lowerBound)
                    {
                        var temp = upperBound;
                        upperBound = lowerBound;
                        lowerBound = temp;
                    }

                    // match all characters in the range inclusively
                    if (lowerBound <= *address && *address <= upperBound)
                    {
                        // move to the start of the next pattern
                        while (*pattern++ != ']')
                        {
                        }

                        return !negate;
                    }
                }
                else
                {
                    // check if the current character matches
                    if (*address == *pattern++)
                    {
                        // move to the start of the next pattern
                        while (*pattern++ != ']')
                        {
                        }

                        return !negate;
                    }
                }

                // check if we are at the end of the braces
                if (*pattern == ']')
                {
                    pattern++;
                    return negate;
                }
            }
        }

        static bool MatchBraces(ref byte* pattern, ref byte* address)
        {
            var addressStart = address;
            var canMatch = true;
            var match = false;

            // empty braces match everything
            if (*pattern == '}')
            {
                pattern++;
                return true;
            }

            // test each string in the braces until a match is found or the closing brace is found
            while (true)
            {
                // check if we have reached the end the last string
                if (*pattern == '}')
                {
                    pattern++;
                    return match;
                }

                // check if we have reached the end of a string other than the last string
                if (*pattern == ',')
                {
                    pattern++;

                    if (match)
                    {
                        // move to the start of the next pattern
                        while (*pattern++ != '}')
                        {
                        }

                        return true;
                    }

                    // prepare to match the next string
                    address = addressStart;
                    canMatch = true;
                    continue;
                }

                // Check if the next character in the string and address match. If so, keep matching, otherwise
                // stop matching until the next string to test.
                if (canMatch)
                {
                    if (match)
                    {
                        address++;
                    }

                    if (*address == *pattern)
                    {
                        match = true;
                    }
                    else
                    {
                        match = false;
                        canMatch = false;
                    }
                }

                pattern++;
            }
        }
    }
}
