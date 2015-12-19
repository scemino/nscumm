using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NScumm.Core.Common
{
    /// <summary>
    /// Huffman bitstream decoding.
    /// </summary>
    public class Huffman
    {
        struct Symbol
        {
            public uint code;
            public uint symbol;

            public Symbol(uint c, uint s)
            {
                code = c;
                symbol = s;
            }
        }

        /// <summary>
        /// Lists of codes and their symbols, sorted by code length.
        /// </summary>
        private List<Symbol>[] _codes;

        /// <summary>
        /// Sorted list of symbols.
        /// </summary>
        private Symbol[] _symbols;

        /// <summary>
        /// Creates a Huffman decoder.
        /// </summary>
        /// <param name="maxLength">Maximal code length. If 0, it's searched for</param>
        /// <param name="codeCount">Number of codes</param>
        /// <param name="codes">The actual codes</param>
        /// <param name="lengths">Lengths of the individual codes</param>
        /// <param name="symbols">The symbols. If null, assume they are identical to the code indices</param>
        public Huffman(byte maxLength, uint codeCount, uint[] codes, byte[] lengths, uint[] symbols = null)
        {
            Debug.Assert(codeCount > 0);

            Debug.Assert(codes != null);
            Debug.Assert(lengths != null);

            if (maxLength == 0)
                for (uint i = 0; i < codeCount; i++)
                    maxLength = Math.Max(maxLength, lengths[i]);

            Debug.Assert(maxLength <= 32);

            _codes = new List<Symbol>[maxLength];
            for (uint i = 0; i < maxLength; i++)
            {
                _codes[i] = new List<Symbol>();
            }
            _symbols = new Symbol[codeCount];
            for (uint i = 0; i < codeCount; i++)
            {
                // The symbol. If none were specified, just assume it's identical to the code index
                uint symbol = symbols != null ? symbols[i] : i;

                // Put the code and symbol into the correct list
                _codes[lengths[i] - 1].Add(new Symbol(codes[i], symbol));

                // And put the pointer to the symbol/code struct into the symbol list.
                _symbols[i] = _codes[lengths[i] - 1].Last();
            }
        }

        /// <summary>
        /// Modify the codes' symbols.
        /// </summary>
        /// <param name="symbols"></param>
        void SetSymbols(uint[] symbols = null)
        {
            var s = 0;
            for (uint i = 0; i < _symbols.Length; i++)
                _symbols[i].symbol = symbols != null ? symbols[s++] : i;
        }

        /// <summary>
        /// Return the next symbol in the bitstream.
        /// </summary>
        /// <param name="bits">The bitstream.</param>
        /// <returns></returns>
        public uint GetSymbol(BitStream bits)
        {
            uint code = 0;

            for (var i = 0; i < _codes.Length; i++)
            {
                bits.AddBit(ref code, i);

                foreach (var cCode in _codes[i])
                {
                    if (code == cCode.code)
                        return cCode.symbol;
                }
            }

            throw new InvalidOperationException("Unknown Huffman code");
        }
    }
}
