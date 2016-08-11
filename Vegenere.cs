using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace A1
{
    /// <summary>
    /// The solution breaks the Vigenere cipher (input hardoded, but could be generalized)
    /// https://en.wikipedia.org/wiki/Vigen%C3%A8re_cipher
    /// </summary>
    [TestFixture]
    public class Vegenere
    {
        const string Input = @"F96DE8C227A259C87EE1DA2AED57C93FE5DA36ED4EC87EF2C63AAE5B9A7EFFD673BE4ACF7BE8923CAB1ECE7AF2DA3DA44FCF7AE29235A24C963FF0DF3CA3599A70E5DA36BF1ECE77F8DC34BE129A6CF4D126BF5B9A7CFEDF3EB850D37CF0C63AA2509A76FF9227A55B9A6FE3D720A850D97AB1DD35ED5FCE6BF0D138A84CC931B1F121B44ECE70F6C032BD56C33FF9D320ED5CDF7AFF9226BE5BDE3FF7DD21ED56CF71F5C036A94D963FF8D473A351CE3FE5DA3CB84DDB71F5C17FED51DC3FE8D732BF4D963FF3C727ED4AC87EF5DB27A451D47EFD9230BF47CA6BFEC12ABE4ADF72E29224A84CDF3FF5D720A459D47AF59232A35A9A7AE7D33FB85FCE7AF5923AA31EDB3FF7D33ABF52C33FF0D673A551D93FFCD33DA35BC831B1F43CBF1EDF67F0DF23A15B963FE5DA36ED68D378F4DC36BF5B9A7AFFD121B44ECE76FEDC73BE5DD27AFCD773BA5FC93FE5DA3CB859D26BB1C63CED5CDF3FE2D730B84CDF3FF7DD21ED5ADF7CF0D636BE1EDB79E5D721ED57CE3FE6D320ED57D469F4DC27A85A963FF3C727ED49DF3FFFDD24ED55D470E69E73AC50DE3FE5DA3ABE1EDF67F4C030A44DDF3FF5D73EA250C96BE3D327A84D963FE5DA32B91ED36BB1D132A31ED87AB1D021A255DF71B1C436BF479A7AF0C13AA14794";
        const int AlphabetSize = 256; // Just plain ASCII
        const int MaxKeyLength = 13;

        private readonly byte[] _input = new byte[Input.Length / 2];

        public Vegenere()
        {
            // Parse byte input from the hex string:
            for (int i = 0; i < Input.Length / 2; i++)
            {
                var strByte = Input.Substring(i * 2, 2);
                _input[i] = byte.Parse(strByte, NumberStyles.HexNumber);
            }
        }

        public int ComputeKeyLength()
        {
            double max = 0;
            int maxLen = 0;

            for (var i = 1; i <= MaxKeyLength; ++i)
            {
                var counted = 0;
                int[] bytes = new int[AlphabetSize];
                for (var pos = i; pos < _input.Length; pos += i)
                {
                    byte b = _input[pos];
                    bytes[b] = bytes[b] + 1;
                    counted++;
                }

                // The highest sum of squares will give the most likely key size:
                double sum = bytes.Sum(b => Math.Pow( b/(double) counted, 2));
                if (sum > max)
                {
                    max = sum;
                    maxLen = i;
                }
            }
            Debug("Key Length: {0}, max score: {1}", maxLen, max);
            return maxLen;
        }

        [Test]
        public void ComputeKey()
        {
            var watch = Stopwatch.StartNew();
            var keyLen = ComputeKeyLength();
            var freqs = EnglishLetterFrequencies();
            var divisor = _input.Length/(decimal)keyLen;
            byte[] key = new byte[keyLen];

            Debug("\nKey:");
            for (int i = 0; i < keyLen; ++i)
            {
                decimal maxSum = 0;
                byte found = 0;

                for (int j = 0; j < AlphabetSize; ++j)
                {
                    bool valid = true;
                    decimal[] observedFreqs = new decimal[AlphabetSize];

                    for (int pos = i; pos < _input.Length; pos += keyLen)
                    {
                        var b = _input[pos] ^ j;
                        if (b < 32 || b > 127)
                        {
                            // non-printable character, discard:
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                    {
                        int count = 0;
                        for (int pos = i; pos < _input.Length; pos += keyLen, count++)
                        {
                            var b = _input[pos] ^ j;
                            // consider lowercase only:
                            char ch = char.ToLower((char)b);
                            var loByte = (byte) ch;
                            // tabulate observed frequencies:
                            observedFreqs[loByte] += (1m / divisor);
                        }

                        var result = new decimal[AlphabetSize];
                        for (int k = 0; k < AlphabetSize; ++k)
                        {
                            result[k] = observedFreqs[k] * (decimal)freqs[k];
                        }

                        // Again, the highest match is the most likely key's byte[i]:
                        var sum = result.Sum();
                        if (maxSum < sum)
                        {
                            maxSum = sum; 
                            found = (byte)j;
                        }
                    }

                } // byte j

                Debug("0x{0:X}", found);
                key[i] = found;
            }

            Console.WriteLine("\nDecoded\n");
            byte[] xored = new byte[_input.Length];
            for (int i = 0; i < _input.Length; ++i)
            {
                byte d = (byte) (_input[i] ^ key[i % keyLen]);
                xored[i] = d;
            }

            var ret = Encoding.ASCII.GetString(xored);
            Debug(ret);

            watch.Stop();
            Debug("\nelapsed: {0}", watch.Elapsed);
        }

        private void Debug(string format, params object[] args)
        {
            if (args == null)
                Console.WriteLine(format);
            else
                Console.WriteLine(format, args);
        }

        private double[] EnglishLetterFrequencies()
        {
            var ret = new double[AlphabetSize];

            // https://en.wikipedia.org/wiki/Letter_frequency
            ret[(byte)'a'] = 8.167;
            ret[(byte)'b'] = 1.492;
            ret[(byte)'c'] = 2.782;
            ret[(byte)'d'] = 4.253;
            ret[(byte)'e'] = 12.702;
            ret[(byte)'f'] = 2.228;
            ret[(byte)'g'] = 2.015;
            ret[(byte)'h'] = 6.094;
            ret[(byte)'i'] = 6.966;
            ret[(byte)'j'] = 0.153;
            ret[(byte)'k'] = 0.772;
            ret[(byte)'l'] = 4.025;
            ret[(byte)'m'] = 2.406;
            ret[(byte)'n'] = 6.749;
            ret[(byte)'o'] = 7.507;
            ret[(byte)'p'] = 1.929;
            ret[(byte)'q'] = 0.095;
            ret[(byte)'r'] = 5.987;
            ret[(byte)'s'] = 6.327;
            ret[(byte)'t'] = 9.056;
            ret[(byte)'u'] = 2.758;
            ret[(byte)'v'] = 0.978;
            ret[(byte)'w'] = 2.361;
            ret[(byte)'x'] = 0.15;
            ret[(byte)'y'] = 1.974;
            ret[(byte)'z'] = 0.074;

            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = ret[i]/100.0d;
            }

            return ret;
        }
    }
}
