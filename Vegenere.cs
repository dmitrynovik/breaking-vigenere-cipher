﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace A1
{
    [TestFixture]
    public class Vegenere
    {
        private readonly byte[] _input;
        const int VocabularyLength = 256;

        [Test]
        public void ParseByte()
        {
            byte.Parse("F9", NumberStyles.HexNumber);
        }

        public Vegenere()
        {
            var input = @"F96DE8C227A259C87EE1DA2AED57C93FE5DA36ED4EC87EF2C63AAE5B9A7EFFD673BE4ACF7BE8923CAB1ECE7AF2DA3DA44FCF7AE29235A24C963FF0DF3CA3599A70E5DA36BF1ECE77F8DC34BE129A6CF4D126BF5B9A7CFEDF3EB850D37CF0C63AA2509A76FF9227A55B9A6FE3D720A850D97AB1DD35ED5FCE6BF0D138A84CC931B1F121B44ECE70F6C032BD56C33FF9D320ED5CDF7AFF9226BE5BDE3FF7DD21ED56CF71F5C036A94D963FF8D473A351CE3FE5DA3CB84DDB71F5C17FED51DC3FE8D732BF4D963FF3C727ED4AC87EF5DB27A451D47EFD9230BF47CA6BFEC12ABE4ADF72E29224A84CDF3FF5D720A459D47AF59232A35A9A7AE7D33FB85FCE7AF5923AA31EDB3FF7D33ABF52C33FF0D673A551D93FFCD33DA35BC831B1F43CBF1EDF67F0DF23A15B963FE5DA36ED68D378F4DC36BF5B9A7AFFD121B44ECE76FEDC73BE5DD27AFCD773BA5FC93FE5DA3CB859D26BB1C63CED5CDF3FE2D730B84CDF3FF7DD21ED5ADF7CF0D636BE1EDB79E5D721ED57CE3FE6D320ED57D469F4DC27A85A963FF3C727ED49DF3FFFDD24ED55D470E69E73AC50DE3FE5DA3ABE1EDF67F4C030A44DDF3FF5D73EA250C96BE3D327A84D963FE5DA32B91ED36BB1D132A31ED87AB1D021A255DF71B1C436BF479A7AF0C13AA14794";

            var bytes = new List<byte>();

            for (int i = 0; i < input.Length / 2; i++)
            {
                var start = i * 2;
                var strByte = input.Substring(start, 2);
                bytes.Add(byte.Parse(strByte, NumberStyles.HexNumber));
            }
            _input = bytes.ToArray();
        }

        //[Test]
        public int ComputeKeyLength(out double[] freqs)
        {
            double max = 0;
            int maxLen = 0;
            const int maxKeyLength = 13;
            freqs = new double[VocabularyLength];

            for (var i = 1; i <= maxKeyLength; ++i)
            {
                var count = 0;
                int[] bytes = new int[VocabularyLength];
                for (var j = 0; j < _input.Length; j++)
                {
                    var pos = i*j;
                    if (pos >= _input.Length)
                        break;

                    byte b = _input[pos];
                    bytes[b] = bytes[b] + 1;
                    count++;
                }

                double sum = bytes.Sum(b => Math.Pow( (b/(double) count), 2));
                Console.WriteLine("Length: {0}, result: {1}", i, sum);
                if (sum > max)
                {
                    max = sum;
                    maxLen = i;
                    for (int k = 0; k < VocabularyLength; ++k)
                    {
                        freqs[k] = bytes[k]/(double) count;
                    }
                }
            }
            Console.WriteLine("LENGTH: {0}, MAX: {1}", maxLen, max);
            return maxLen;
        }

        [Test]
        public void ComputeKey()
        {
            double[] freqs;
            var keyLen = ComputeKeyLength(out freqs);
            byte[] key = new byte[keyLen];

            for (int i = 0; i < keyLen; ++i)
            {
                double maxSum = 0;
                byte found = 0;

                for (int j = 0; j < VocabularyLength; ++j)
                {
                    bool valid = true;
                    int[] observed = new int[VocabularyLength];

                    for (int pos = i; pos < _input.Length; pos += keyLen)
                    {
                        var b = _input[i] ^ j;
                        if (b < 32 || b > 127)
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                    {
                        //Console.WriteLine("{0}Valid: {1}", i, (char)j);
                        int count = 1;
                        for (int pos = i; pos < _input.Length; pos += keyLen)
                        {
                            var b = _input[pos] ^ j;
                            char ch = char.ToLower((char)b);
                            var loByte = (byte) ch;
                            observed[loByte] = observed[loByte] + 1;
                            count++;
                        }

                        double[] oFreqs = observed.Select(o => o/(double) count).ToArray();
                        double[] result = new double[VocabularyLength];

                        int maxX = 0, maxY = 0;
                        for (int x = 0; x < VocabularyLength; x++)
                        {
                            if (maxX < freqs[x])
                                maxX = x;
                        }

                        for (int y = 0; y < VocabularyLength; y++)
                        {
                            if (maxY < oFreqs[y])
                                maxY = y;
                        }
                        var diff = maxX - maxY;

                        for (int k = 0; k < VocabularyLength; ++k)
                        {
                            result[k] = oFreqs[k] * freqs[(k + diff) % VocabularyLength];
                        }

                        var sum = result.Sum();
                        //var variance = Math.Abs(0.065 - sum);
                        if (/*variance < maxSum*/ maxSum < sum)
                        {
                            maxSum = sum; // variance
                            found = (byte)j;
                        }
                    }

                } // byte j

                Console.WriteLine("{0} {1:X}", i, found);
                key[i] = found;
            }

            // TO BE REMOVED:
            //key = new byte[] {0xba, 0x1f, 0x91, 0xb2, 0x53, 0xcd, 0x3e};

            Console.WriteLine("Result\n");
            byte[] xored = new byte[_input.Length];
            for (int i = 0; i < _input.Length; ++i)
            {
                var b = _input[i];
                var k = key[i%keyLen];
                byte d = (byte) (b ^ k);
                xored[i] = d;
            }

            var ret = Encoding.ASCII.GetString(xored);
            Console.WriteLine(ret);
        }
    }
}