﻿using System;
using System.Linq;
using System.Text;
using BencodeNET.Exceptions;
using BencodeNET.Objects;

namespace BencodeNET.Parsing
{
    /// <summary>
    /// A parser for bencoded numbers.
    /// </summary>
    public class BNumberParser : BObjectParser<BNumber>
    {
        /// <summary>
        /// The minimum stream length in bytes for a valid number ('i0e').
        /// </summary>
        protected const int MinimumLength = 3;

        /// <summary>
        /// The encoding used for parsing.
        /// </summary>
        protected override Encoding Encoding
        {
            get {
                return Encoding.UTF8;
            }
        }

        /// <summary>
        /// Parses the next <see cref="BNumber"/> from the stream.
        /// </summary>
        /// <param name="stream">The stream to parse from.</param>
        /// <returns>The parsed <see cref="BNumber"/>.</returns>
        /// <exception cref="InvalidBencodeException{BNumber}">Invalid bencode</exception>
        /// <exception cref="UnsupportedBencodeException{BNumber}">The bencode is unsupported by this library</exception>
        public override BNumber Parse(BencodeStream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            if (stream.Length < MinimumLength)
                throw InvalidBencodeException<BNumber>.BelowMinimumLength(MinimumLength, stream.Length, stream.Position);

            var startPosition = stream.Position;

            // Numbers must start with 'i'
            if (stream.ReadChar() != 'i')
                throw InvalidBencodeException<BNumber>.UnexpectedChar('i', stream.ReadPreviousChar(), stream.Position);

            var digits = new StringBuilder();
            char c;
            for (c = stream.ReadChar(); c != 'e' && c != default(char); c = stream.ReadChar())
            {
                digits.Append(c);
            }

            // Last read character should be 'e'
            if (c != 'e')
            {
                if (stream.EndOfStream) throw InvalidBencodeException<BNumber>.MissingEndChar();
                throw InvalidBencodeException<BNumber>.InvalidEndChar(c, stream.Position);
            }

            var isNegative = digits[0] == '-';
            var numberOfDigits = isNegative ? digits.Length - 1 : digits.Length;

            // We do not support numbers that cannot be stored as a long (Int64)
            if (numberOfDigits > BNumber.MaxDigits)
            {
                throw UnsupportedException(
                    string.Format("The number '{0}' has more than 19 digits and cannot be stored as a long (Int64) and therefore is not supported.", digits),
                    startPosition);
            }

            // We need at least one digit
            if (numberOfDigits < 1)
                throw InvalidException("It contains no digits.", startPosition);

            var firstDigit = isNegative ? digits[1] : digits[0];

            // Leading zeros are not valid
            if (firstDigit == '0' && numberOfDigits > 1)
                throw InvalidException(string.Format("Leading '0's are not valid. Found value '{0}'.", digits), startPosition);

            // '-0' is not valid either
            if (firstDigit == '0' && numberOfDigits == 1 && isNegative)
                throw InvalidException("'-0' is not a valid number.", startPosition);

            long number;
            if (!ParseUtil.TryParseLongFast(digits.ToString(), out number))
            {
                var nonSignChars = isNegative ? digits.ToString(1, digits.Length - 1) : digits.ToString();
                if (nonSignChars.Any(x => !x.IsDigit()))
                    throw InvalidException(string.Format("The value '{0}' is not a valid number.", digits), startPosition);

                throw UnsupportedException(
                    string.Format("The value '{0}' is not a valid long (Int64). Supported values range from '{1:N0}' to '{2:N0}'.", digits, long.MinValue, long.MaxValue),
                    startPosition);
            }

            return new BNumber(number);
        }

        private static InvalidBencodeException<BNumber> InvalidException(string message, long startPosition)
        {
            return new InvalidBencodeException<BNumber>(
                string.Format("{0} The number starts at position {1}.", message, startPosition),
                startPosition);
        }

        private static UnsupportedBencodeException<BNumber> UnsupportedException(string message, long startPosition)
        {
            return new UnsupportedBencodeException<BNumber>(
                string.Format("{0} The number starts at position {1}.", message, startPosition),
                startPosition);
        }
    }
}
